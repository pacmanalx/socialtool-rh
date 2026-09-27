using Microsoft.EntityFrameworkCore;
using SocialTool.Domain.Common;
using SocialTool.Domain.Entities;

namespace SocialTool.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<User> Users => Set<User>();
    public DbSet<CompanyValue> CompanyValues => Set<CompanyValue>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostReaction> PostReactions => Set<PostReaction>();
    public DbSet<PostComment> PostComments => Set<PostComment>();
    public DbSet<Recognition> Recognitions => Set<Recognition>();
    public DbSet<DailyMood> DailyMoods => Set<DailyMood>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<OneOnOne> OneOnOnes => Set<OneOnOne>();
    public DbSet<OneOnOnePoint> OneOnOnePoints => Set<OneOnOnePoint>();
    public DbSet<OneOnOneAction> OneOnOneActions => Set<OneOnOneAction>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("organization");
            entity.Property(o => o.Name).HasMaxLength(150).IsRequired();
            entity.Property(o => o.CurrencyName).HasMaxLength(50).HasDefaultValue("SocialCoins");
            entity.Property(o => o.MonthlyCoinsQuota).HasDefaultValue(100);
            entity.Property(o => o.GoogleWorkspaceDomain).HasMaxLength(200);
        });

        // Department Configuration
        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("departments");
            entity.Property(d => d.Name).HasMaxLength(100).IsRequired();

            entity.HasOne(d => d.ParentDepartment)
                .WithMany(d => d.SubDepartments)
                .HasForeignKey(d => d.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Leader)
                .WithMany()
                .HasForeignKey(d => d.LeaderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.GoogleSubject).IsUnique();
            entity.Property(u => u.Name).HasMaxLength(150).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(200).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(500);
            entity.Property(u => u.GoogleSubject).HasMaxLength(100);
            entity.Property(u => u.JobTitle).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(u => u.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(u => u.Manager)
                .WithMany(u => u.Subordinates)
                .HasForeignKey(u => u.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // CompanyValue Configuration
        modelBuilder.Entity<CompanyValue>(entity =>
        {
            entity.ToTable("company_values");
            entity.Property(v => v.Title).HasMaxLength(100).IsRequired();
            entity.Property(v => v.Icon).HasMaxLength(50).HasDefaultValue("Award");
        });

        // Post Configuration
        modelBuilder.Entity<Post>(entity =>
        {
            entity.ToTable("posts");
            entity.HasIndex(p => p.CreatedAt);
            entity.Property(p => p.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(p => p.Title).HasMaxLength(200);

            entity.HasOne(p => p.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PostReaction Configuration
        modelBuilder.Entity<PostReaction>(entity =>
        {
            entity.ToTable("post_reactions");
            entity.HasIndex(r => new { r.PostId, r.UserId, r.Type }).IsUnique();
            entity.Property(r => r.Type).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(r => r.Post)
                .WithMany(p => p.Reactions)
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.User)
                .WithMany(u => u.Reactions)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PostComment Configuration
        modelBuilder.Entity<PostComment>(entity =>
        {
            entity.ToTable("post_comments");
            entity.HasIndex(c => new { c.PostId, c.CreatedAt });

            entity.HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Recognition Configuration
        modelBuilder.Entity<Recognition>(entity =>
        {
            entity.ToTable("recognitions");
            entity.HasIndex(r => r.CreatedAt);

            entity.HasOne(r => r.Sender)
                .WithMany(u => u.RecognitionsSent)
                .HasForeignKey(r => r.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Receiver)
                .WithMany(u => u.RecognitionsReceived)
                .HasForeignKey(r => r.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.CompanyValue)
                .WithMany(v => v.Recognitions)
                .HasForeignKey(r => r.CompanyValueId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Post)
                .WithOne(p => p.Recognition)
                .HasForeignKey<Recognition>(r => r.PostId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // DailyMood Configuration
        modelBuilder.Entity<DailyMood>(entity =>
        {
            entity.ToTable("daily_moods");
            entity.HasIndex(m => new { m.UserId, m.Date }).IsUnique();

            entity.HasOne(m => m.User)
                .WithMany(u => u.DailyMoods)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Feedback Configuration
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("feedbacks");
            entity.HasIndex(f => f.CreatedAt);
            entity.Property(f => f.Visibility).HasConversion<string>().HasMaxLength(30);
            entity.Property(f => f.Status).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(f => f.Sender)
                .WithMany()
                .HasForeignKey(f => f.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.Receiver)
                .WithMany()
                .HasForeignKey(f => f.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OneOnOne Configuration
        modelBuilder.Entity<OneOnOne>(entity =>
        {
            entity.ToTable("one_on_ones");
            entity.HasIndex(o => o.ScheduledAt);
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(o => o.Leader)
                .WithMany()
                .HasForeignKey(o => o.LeaderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Led)
                .WithMany()
                .HasForeignKey(o => o.LedId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OneOnOnePoint Configuration
        modelBuilder.Entity<OneOnOnePoint>(entity =>
        {
            entity.ToTable("one_on_one_points");

            entity.HasOne(p => p.OneOnOne)
                .WithMany(o => o.Points)
                .HasForeignKey(p => p.OneOnOneId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.CreatedBy)
                .WithMany()
                .HasForeignKey(p => p.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OneOnOneAction Configuration
        modelBuilder.Entity<OneOnOneAction>(entity =>
        {
            entity.ToTable("one_on_one_actions");

            entity.HasOne(a => a.OneOnOne)
                .WithMany(o => o.Actions)
                .HasForeignKey(a => a.OneOnOneId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Assignee)
                .WithMany()
                .HasForeignKey(a => a.AssigneeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.ToTable("user_tokens");
            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.HasIndex(t => new { t.UserId, t.Purpose });
            entity.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(t => t.Purpose).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.HasIndex(t => t.UserId);
            entity.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(t => t.CreatedByIp).HasMaxLength(64);
            entity.Property(t => t.UserAgent).HasMaxLength(300);

            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                    entry.Entity.Id = Guid.NewGuid();

                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

public static class OrganizationQueries
{
    // A instalação tem exatamente uma organização, criada na primeira subida.
    public static Task<Organization> GetOrganizationAsync(this ApplicationDbContext db, CancellationToken ct = default) =>
        db.Organizations.OrderBy(o => o.CreatedAt).FirstAsync(ct);
}
