namespace SocialTool.Domain.Authorization;

// Catálogo das permissões administrativas que o Admin concede, pessoa a pessoa, aos gestores de RH.
// A chave é o que fica gravado no banco: nunca renomeie uma chave existente (as concessões gravadas se perderiam).
public static class Permissions
{
    public const string UsersView = "users.view";
    public const string UsersInvite = "users.invite";
    public const string UsersEdit = "users.edit";
    public const string UsersImport = "users.import";
    public const string UsersRevoke = "users.revoke";
    public const string UsersAuthorize = "users.authorize";
    public const string UsersAssignRoles = "users.assign_roles";
    public const string StructureManage = "structure.manage";
    public const string FeedAnnounce = "feed.announce";
    public const string SurveysManage = "surveys.manage";
    public const string ReportsView = "reports.view";
    public const string ReportsExport = "reports.export";
    public const string SensitiveViewIndividual = "sensitive.view_individual";
    public const string AuditView = "audit.view";

    public static readonly IReadOnlyList<PermissionDefinition> Catalog =
    [
        new(UsersView, "Usuários", "Ver usuários", "Acessa a lista de colaboradores e seus dados cadastrais."),
        new(UsersInvite, "Usuários", "Cadastrar e convidar", "Cadastra pessoas e envia (ou reenvia) convites de acesso."),
        new(UsersEdit, "Usuários", "Editar cadastro", "Altera nome, cargo e departamento."),
        new(UsersImport, "Usuários", "Importar do Workspace", "Carrega usuários a partir da exportação do Google Workspace."),
        new(UsersRevoke, "Usuários", "Revogar acesso", "Desativa contas; a pessoa perde o acesso na hora."),
        new(UsersAuthorize, "Usuários", "Liberar acesso", "Reativa contas desativadas."),
        new(UsersAssignRoles, "Usuários", "Alterar papel", "Muda o papel de colaboradores (nunca para ou de Administrador)."),
        new(StructureManage, "Estrutura", "Gerenciar estrutura", "Cria unidades, departamentos e setores e aloca as pessoas nas áreas."),
        new(FeedAnnounce, "Comunicação", "Publicar comunicados", "Publica comunicados oficiais no feed."),
        new(SurveysManage, "Enquetes", "Criar e gerenciar enquetes", "Cria enquetes, escalas de resposta, acompanha o resultado e encerra."),
        new(ReportsView, "Relatórios", "Ver relatórios", "Consulta relatórios de engajamento, humor e reconhecimento.", Available: false),
        new(ReportsExport, "Relatórios", "Exportar relatórios", "Baixa os relatórios em planilha.", Available: false),
        new(SensitiveViewIndividual, "Dados sensíveis", "Ver dado individual",
            "Abre respostas de enquetes identificadas (e, no futuro, humor e feedback) de uma pessoa. Cada acesso exige motivo e fica registrado."),
        new(AuditView, "Auditoria", "Ver auditoria", "Consulta o registro de ações administrativas."),
    ];

    // O que um gestor de RH recebe ao ganhar o papel; o Admin ajusta depois, pessoa a pessoa.
    public static readonly IReadOnlyList<string> HrDefaults =
    [
        UsersView, UsersInvite, UsersEdit, UsersImport, UsersRevoke, UsersAuthorize,
        StructureManage, FeedAnnounce, SurveysManage, ReportsView,
    ];

    private static readonly HashSet<string> Keys = Catalog.Select(p => p.Key).ToHashSet();

    public static bool IsKnown(string key) => Keys.Contains(key);

    public static IReadOnlySet<string> All => Keys;
}

// Available = false: a permissão já pode ser concedida, mas o recurso ainda não foi construído.
public record PermissionDefinition(string Key, string Group, string Label, string Description, bool Available = true);
