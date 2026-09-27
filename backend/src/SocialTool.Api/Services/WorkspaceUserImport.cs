using System.Globalization;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SocialTool.Api.DTOs;
using SocialTool.Domain.Entities;
using SocialTool.Domain.Enums;
using SocialTool.Infrastructure.Persistence;

namespace SocialTool.Api.Services;

public record DirectoryEntry(string Email, string Name, string? JobTitle, string? Department, bool Suspended, bool NeverSignedIn);

// Lê o arquivo exportado pelo Admin Console ("Fazer o download dos usuários", formato JSON).
// Só os campos que o SocialTool usa são aproveitados; senha, telefones, endereços etc. são ignorados.
public static class WorkspaceExportParser
{
    private static readonly string[] EmailKeys = ["Email Address [Required]", "Email Address"];
    private static readonly string[] FirstNameKeys = ["First Name [Required]", "First Name"];
    private static readonly string[] LastNameKeys = ["Last Name [Required]", "Last Name"];
    private static readonly string[] StatusKeys = ["Status [READ ONLY]", "Status"];
    private static readonly string[] LastSignInKeys = ["Last Sign In [READ ONLY]", "Last Sign In"];
    private static readonly string[] TitleKeys = ["Employee Title"];
    private static readonly string[] DepartmentKeys = ["Department"];

    public static List<DirectoryEntry> Parse(Stream stream)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(stream);
        }
        catch (JsonException)
        {
            throw new FormatException("O arquivo não é um JSON válido.");
        }

        using (document)
        {
            var users = document.RootElement.ValueKind switch
            {
                JsonValueKind.Array => document.RootElement,
                JsonValueKind.Object when document.RootElement.TryGetProperty("users", out var list) && list.ValueKind == JsonValueKind.Array => list,
                _ => throw new FormatException("Formato não reconhecido: esperava a exportação de usuários do Admin Console (JSON com a lista \"users\").")
            };

            var entries = new List<DirectoryEntry>();
            foreach (var user in users.EnumerateArray())
            {
                if (user.ValueKind != JsonValueKind.Object)
                    continue;

                var email = Read(user, EmailKeys);
                if (email == null)
                    throw new FormatException("Formato não reconhecido: os registros não têm o campo \"Email Address\".");

                var name = $"{Read(user, FirstNameKeys)} {Read(user, LastNameKeys)}".Trim();
                var status = Read(user, StatusKeys) ?? string.Empty;
                var lastSignIn = Read(user, LastSignInKeys) ?? string.Empty;

                entries.Add(new DirectoryEntry(
                    email.Trim().ToLowerInvariant(),
                    name,
                    Read(user, TitleKeys),
                    Read(user, DepartmentKeys),
                    status.Contains("suspend", StringComparison.OrdinalIgnoreCase),
                    lastSignIn.Length == 0 || lastSignIn.Contains("never", StringComparison.OrdinalIgnoreCase)));
            }
            return entries;
        }
    }

    private static string? Read(JsonElement user, string[] keys)
    {
        foreach (var key in keys)
        {
            if (user.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null)
            {
                var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
                return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            }
        }
        return null;
    }
}

public enum ImportAction { Create, Update, Deactivate, Unchanged, Skip }

public class ImportPlanItem
{
    public required DirectoryEntry Entry { get; init; }
    public ImportAction Action { get; set; }
    public string? Reason { get; set; }
    public List<string> Changes { get; } = [];
    public User? Existing { get; init; }
    public bool SetJobTitle { get; set; }
    public string? DepartmentKey { get; set; }
}

public class ImportPlan
{
    public List<ImportPlanItem> Items { get; } = [];
    public int LocalNotInFile { get; set; }
    public Dictionary<string, Department> ExistingDepartments { get; init; } = [];
    // Chave normalizada -> nome como veio no arquivo (primeira ocorrência).
    public Dictionary<string, string> DepartmentsToCreate { get; } = [];
    public SortedSet<string> UnmappedDepartments { get; } = [];
}

// Importação incremental: e-mail novo vira usuário; e-mail existente só tem preenchidos campos vazios.
// Nada que foi editado aqui é sobrescrito, ninguém é reativado e nenhum convite é enviado.
public class WorkspaceUserImportService
{
    public const string SourceName = "google-workspace-json";
    private const int NameMax = 150, EmailMax = 200, TitleMax = 100, DepartmentMax = 100;

    private readonly ApplicationDbContext _db;

    public WorkspaceUserImportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ImportPlan> BuildPlanAsync(IReadOnlyList<DirectoryEntry> entries, UserImportRequest options, Guid? currentUserId)
    {
        var users = await _db.Users.Include(u => u.Department).ToListAsync();
        var byEmail = users.ToDictionary(u => u.Email.ToLowerInvariant());
        var departments = await _db.Departments.ToListAsync();
        var plan = new ImportPlan
        {
            ExistingDepartments = departments
                .GroupBy(d => NormalizeKey(d.Name))
                .ToDictionary(g => g.Key, g => g.First())
        };

        var excluded = new HashSet<string>(
            (options.ExcludedEmails ?? []).Select(e => e.Trim().ToLowerInvariant()));
        var seen = new HashSet<string>();

        foreach (var entry in entries)
        {
            var item = new ImportPlanItem { Entry = entry, Existing = byEmail.GetValueOrDefault(entry.Email) };
            plan.Items.Add(item);

            if (entry.Email.Length > EmailMax || !MailAddress.TryCreate(entry.Email, out _))
            {
                Skip(item, "E-mail inválido.");
                continue;
            }
            if (!seen.Add(entry.Email))
            {
                Skip(item, "E-mail repetido no arquivo.");
                continue;
            }
            if (excluded.Contains(entry.Email))
            {
                Skip(item, "Retirado manualmente desta importação.");
                continue;
            }

            if (item.Existing == null)
                PlanNew(plan, item, options);
            else
                PlanExisting(plan, item, options, currentUserId);
        }

        var inFile = new HashSet<string>(entries.Select(e => e.Email));
        plan.LocalNotInFile = users.Count(u => u.IsActive && !inFile.Contains(u.Email.ToLowerInvariant()));
        return plan;
    }

    public async Task<UserImportResultDto> ApplyAsync(ImportPlan plan, Guid importedById, string fileName)
    {
        var organization = await _db.GetOrganizationAsync();
        var createdDepartments = new Dictionary<string, Department>();
        foreach (var (key, name) in plan.DepartmentsToCreate)
        {
            var department = new Department { Name = Truncate(name, DepartmentMax) };
            _db.Departments.Add(department);
            createdDepartments[key] = department;
        }

        Guid? ResolveDepartment(string? key) =>
            key == null ? null
            : plan.ExistingDepartments.TryGetValue(key, out var existing) ? existing.Id
            : createdDepartments.TryGetValue(key, out var created) ? created.Id
            : null;

        int created = 0, updated = 0, deactivated = 0;
        var toRevoke = new List<Guid>();
        foreach (var item in plan.Items)
        {
            switch (item.Action)
            {
                case ImportAction.Create:
                    _db.Users.Add(new User
                    {
                        Name = Truncate(item.Entry.Name, NameMax),
                        Email = item.Entry.Email,
                        JobTitle = Truncate(item.Entry.JobTitle ?? string.Empty, TitleMax),
                        DepartmentId = ResolveDepartment(item.DepartmentKey),
                        Role = UserRole.Employee,
                        CoinsAvailableToGive = organization.MonthlyCoinsQuota
                    });
                    created++;
                    break;

                case ImportAction.Update:
                case ImportAction.Deactivate:
                    var user = item.Existing!;
                    if (item.SetJobTitle)
                        user.JobTitle = Truncate(item.Entry.JobTitle!, TitleMax);
                    if (item.DepartmentKey != null)
                        user.DepartmentId = ResolveDepartment(item.DepartmentKey);
                    if (item.Action == ImportAction.Deactivate)
                    {
                        user.IsActive = false;
                        toRevoke.Add(user.Id);
                        deactivated++;
                    }
                    else
                    {
                        updated++;
                    }
                    break;
            }
        }

        var result = new UserImportResultDto(
            created, updated, deactivated,
            plan.Items.Count(i => i.Action == ImportAction.Skip),
            createdDepartments.Count);

        _db.UserImports.Add(new UserImport
        {
            ImportedById = importedById,
            Source = SourceName,
            FileName = Truncate(Path.GetFileName(fileName), 255),
            TotalInFile = plan.Items.Count,
            Created = result.Created,
            Updated = result.Updated,
            Deactivated = result.Deactivated,
            Skipped = result.Skipped,
            DepartmentsCreated = result.DepartmentsCreated
        });

        // Tudo ou nada: uma falha no meio não pode deixar metade da importação gravada.
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            await _db.SaveChangesAsync();
            if (toRevoke.Count > 0)
            {
                var now = DateTime.UtcNow;
                await _db.RefreshTokens
                    .Where(t => toRevoke.Contains(t.UserId) && t.RevokedAt == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));
            }
            await transaction.CommitAsync();
        });

        return result;
    }

    public static UserImportPreviewDto ToPreview(ImportPlan plan) => new(
        plan.Items.Count,
        plan.Items.Count(i => i.Action == ImportAction.Create),
        plan.Items.Count(i => i.Action == ImportAction.Update),
        plan.Items.Count(i => i.Action == ImportAction.Deactivate),
        plan.Items.Count(i => i.Action == ImportAction.Unchanged),
        plan.Items.Count(i => i.Action == ImportAction.Skip),
        plan.LocalNotInFile,
        plan.DepartmentsToCreate.Values.OrderBy(n => n).ToList(),
        plan.UnmappedDepartments.ToList(),
        plan.Items
            .OrderBy(i => i.Action)
            .ThenBy(i => i.Entry.Name)
            .Select(i => new UserImportRowDto(
                i.Entry.Email,
                i.Entry.Name,
                i.Entry.JobTitle,
                i.Entry.Department,
                i.Action.ToString(),
                i.Reason,
                i.Changes,
                i.Entry.Suspended,
                i.Entry.NeverSignedIn,
                i.Existing == null ? null : !i.Existing.IsActive ? "Inactive" : i.Existing.ActivatedAt == null ? "Pending" : "Active"))
            .ToList());

    private void PlanNew(ImportPlan plan, ImportPlanItem item, UserImportRequest options)
    {
        var entry = item.Entry;
        if (entry.Suspended)
        {
            Skip(item, "Suspenso no Workspace.");
            return;
        }
        if (entry.NeverSignedIn && !options.IncludeNeverSignedIn)
        {
            Skip(item, "Nunca entrou no Google (provável conta de serviço ou compartilhada).");
            return;
        }
        if (entry.Name.Length == 0)
        {
            Skip(item, "Sem nome no arquivo.");
            return;
        }

        item.Action = ImportAction.Create;
        item.DepartmentKey = ResolveDepartmentKey(plan, entry.Department, options);
    }

    private void PlanExisting(ImportPlan plan, ImportPlanItem item, UserImportRequest options, Guid? currentUserId)
    {
        var entry = item.Entry;
        var user = item.Existing!;
        item.Action = ImportAction.Unchanged;

        if (!user.IsActive)
        {
            item.Reason = entry.Suspended ? null : "Desativado aqui e ativo no Workspace — reative manualmente se for o caso.";
            return;
        }

        if (string.IsNullOrWhiteSpace(user.JobTitle) && !string.IsNullOrEmpty(entry.JobTitle))
        {
            item.SetJobTitle = true;
            item.Changes.Add($"cargo: {entry.JobTitle}");
        }
        if (user.DepartmentId == null && !string.IsNullOrEmpty(entry.Department))
        {
            var key = ResolveDepartmentKey(plan, entry.Department, options);
            if (key != null)
            {
                item.DepartmentKey = key;
                item.Changes.Add($"departamento: {entry.Department}");
            }
        }
        if (item.Changes.Count > 0)
            item.Action = ImportAction.Update;

        if (entry.Suspended)
        {
            if (!options.DeactivateSuspended)
                item.Reason = "Suspenso no Workspace; continua ativo aqui.";
            else if (user.Id == currentUserId)
                item.Reason = "Suspenso no Workspace, mas é a sua própria conta — não desativada.";
            else
            {
                item.Action = ImportAction.Deactivate;
                item.Changes.Add("desativar (suspenso no Workspace)");
            }
        }
    }

    private static string? ResolveDepartmentKey(ImportPlan plan, string? department, UserImportRequest options)
    {
        if (string.IsNullOrWhiteSpace(department))
            return null;
        var key = NormalizeKey(department);
        if (plan.ExistingDepartments.ContainsKey(key) || plan.DepartmentsToCreate.ContainsKey(key))
            return key;
        if (options.CreateMissingDepartments)
        {
            plan.DepartmentsToCreate[key] = department.Trim();
            return key;
        }
        plan.UnmappedDepartments.Add(department.Trim());
        return null;
    }

    private static void Skip(ImportPlanItem item, string reason)
    {
        item.Action = ImportAction.Skip;
        item.Reason = reason;
    }

    private static string NormalizeKey(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }
        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
