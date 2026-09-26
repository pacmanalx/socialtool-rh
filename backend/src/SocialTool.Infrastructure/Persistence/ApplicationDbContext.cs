using Microsoft.EntityFrameworkCore;
using SocialTool.Application.Common.Interfaces;
using SocialTool.Domain.Common;
using SocialTool.Domain.Entities;

namespace SocialTool.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global Multi-tenant Query Filters
        modelBuilder.Entity<Department>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<User>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<CompanyValue>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Post>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Recognition>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<DailyMood>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Feedback>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<OneOnOne>().HasQueryFilter(e => !_tenantContext.HasTenant || e.TenantId == _tenantContext.TenantId);

        // Tenant Configuration
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasIndex(t => t.Subdomain).IsUnique();
            entity.Property(t => t.Name).HasMaxLength(150).IsRequired();
            entity.Property(t => t.Subdomain).HasMaxLength(60).IsRequired();
            entity.Property(t => t.CurrencyName).HasMaxLength(50).HasDefaultValue("SocialCoins");
            entity.Property(t => t.MonthlyCoinsQuota).HasDefaultValue(100);
        });

        // Department Configuration
        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("departments");
            entity.HasIndex(d => d.TenantId);
            entity.Property(d => d.Name).HasMaxLength(100).IsRequired();

            entity.HasOne(d => d.Tenant)
                .WithMany(t => t.Departments)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

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
            entity.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
            entity.Property(u => u.Name).HasMaxLength(150).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(200).IsRequired();
            entity.Property(u => u.JobTitle).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(u => u.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

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
            entity.HasIndex(v => v.TenantId);
            entity.Property(v => v.Title).HasMaxLength(100).IsRequired();
            entity.Property(v => v.Icon).HasMaxLength(50).HasDefaultValue("Award");

            entity.HasOne(v => v.Tenant)
                .WithMany(t => t.CompanyValues)
                .HasForeignKey(v => v.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Post Configuration
        modelBuilder.Entity<Post>(entity =>
        {
            entity.ToTable("posts");
            entity.HasIndex(p => new { p.TenantId, p.CreatedAt });
            entity.Property(p => p.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(p => p.Title).HasMaxLength(200);

            entity.HasOne(p => p.Tenant)
                .WithMany(t => t.Posts)
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

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
            entity.HasIndex(r => new { r.TenantId, r.CreatedAt });

            entity.HasOne(r => r.Tenant)
                .WithMany()
                .HasForeignKey(r => r.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

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
            entity.HasIndex(m => new { m.TenantId, m.UserId, m.Date }).IsUnique();

            entity.HasOne(m => m.Tenant)
                .WithMany()
                .HasForeignKey(m => m.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.User)
                .WithMany(u => u.DailyMoods)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Feedback Configuration
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("feedbacks");
            entity.HasIndex(f => new { f.TenantId, f.CreatedAt });
            entity.Property(f => f.Visibility).HasConversion<string>().HasMaxLength(30);
            entity.Property(f => f.Status).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(f => f.Tenant)
                .WithMany()
                .HasForeignKey(f => f.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

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
            entity.HasIndex(o => new { o.TenantId, o.ScheduledAt });
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(o => o.Tenant)
                .WithMany()
                .HasForeignKey(o => o.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

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

                if (entry.Entity is ITenantEntity tenantEntity && tenantEntity.TenantId == Guid.Empty && _tenantContext.HasTenant)
                {
                    tenantEntity.TenantId = _tenantContext.TenantId!.Value;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
