using Microsoft.EntityFrameworkCore;
using SocialTool.Application.Common.Interfaces;
using SocialTool.Domain.Entities;
using SocialTool.Domain.Enums;

namespace SocialTool.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        // Garante aplicação de migrations
        await context.Database.MigrateAsync();

        // Se já existe tenant demo, não duplica
        if (await context.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Subdomain == "demo"))
        {
            return;
        }

        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Demo Company",
            Subdomain = "demo",
            CurrencyName = "SocialCoins",
            MonthlyCoinsQuota = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Tenants.Add(tenant);

        // Departamentos
        var deptTi = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Tecnologia & Inovação",
            CreatedAt = DateTime.UtcNow
        };
        var deptRh = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Gente & Gestão (RH)",
            CreatedAt = DateTime.UtcNow
        };
        var deptVendas = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Comercial & Expansão",
            CreatedAt = DateTime.UtcNow
        };
        context.Departments.AddRange(deptTi, deptRh, deptVendas);

        // Valores da Empresa
        var valEquipe = new CompanyValue
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = "Espírito de Equipe",
            Description = "Crescemos juntos com apoio mútuo, empatia e colaboração contínua.",
            Icon = "Users",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var valInovacao = new CompanyValue
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = "Inovação & Agilidade",
            Description = "Buscamos novas soluções descomplicadas para surpreender nossos consultores.",
            Icon = "Zap",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var valResultados = new CompanyValue
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = "Foco em Resultados",
            Description = "Compromisso com excelência e entrega consistente de valor.",
            Icon = "TrendingUp",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var valCliente = new CompanyValue
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = "Foco nas Pessoas",
            Description = "Cuidamos das relações humanas e reconhecemos cada conquista.",
            Icon = "Heart",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.CompanyValues.AddRange(valEquipe, valInovacao, valResultados, valCliente);

        // Usuários
        var defaultPasswordHash = passwordHasher.HashPassword("123456");

        var userAdmin = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DepartmentId = deptTi.Id,
            Name = "Alexandre Pereira",
            Email = "alexandre.pereira@example.com",
            PasswordHash = defaultPasswordHash,
            JobTitle = "Tech Lead & Arquiteto",
            Role = UserRole.Admin,
            CoinsAvailableToGive = 100,
            CoinsBalanceToSpend = 150,
            HireDate = new DateOnly(2022, 3, 1),
            BirthDate = new DateOnly(1990, 5, 14),
            AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var userRh = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DepartmentId = deptRh.Id,
            Name = "Juliana Santos",
            Email = "juliana.santos@example.com",
            PasswordHash = defaultPasswordHash,
            JobTitle = "Gerente de DHO & Cultura",
            Role = UserRole.HR,
            CoinsAvailableToGive = 100,
            CoinsBalanceToSpend = 80,
            HireDate = new DateOnly(2023, 9, 21),
            BirthDate = new DateOnly(1988, 11, 20),
            AvatarUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var userGestor = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DepartmentId = deptTi.Id,
            Name = "Carlos Andrade",
            Email = "carlos.andrade@example.com",
            PasswordHash = defaultPasswordHash,
            JobTitle = "Coordenador de Engenharia",
            Role = UserRole.Leader,
            CoinsAvailableToGive = 70,
            CoinsBalanceToSpend = 120,
            HireDate = new DateOnly(2021, 6, 15),
            AvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var userDevFrontend = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DepartmentId = deptTi.Id,
            ManagerId = userGestor.Id,
            Name = "Mariana Costa",
            Email = "mariana.costa@example.com",
            PasswordHash = defaultPasswordHash,
            JobTitle = "Desenvolvedora Frontend Pleno",
            Role = UserRole.Employee,
            CoinsAvailableToGive = 100,
            CoinsBalanceToSpend = 110,
            HireDate = new DateOnly(2023, 1, 10),
            BirthDate = new DateOnly(1996, 7, 28),
            AvatarUrl = "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=150",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var userDevBackend = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DepartmentId = deptTi.Id,
            ManagerId = userGestor.Id,
            Name = "Lucas Fernandes",
            Email = "lucas.fernandes@example.com",
            PasswordHash = defaultPasswordHash,
            JobTitle = "Desenvolvedor Backend .NET",
            Role = UserRole.Employee,
            CoinsAvailableToGive = 100,
            CoinsBalanceToSpend = 45,
            HireDate = new DateOnly(2024, 2, 1),
            AvatarUrl = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(userAdmin, userRh, userGestor, userDevFrontend, userDevBackend);
        await context.SaveChangesAsync();

        // Liderança de Departamentos (atribuída após criação dos usuários para evitar ciclo no EF Core)
        deptTi.LeaderId = userGestor.Id;
        deptRh.LeaderId = userRh.Id;

        // Publicação 1: Boas-vindas corporativa (Anúncio oficial do RH)
        var postAnuncio = new Post
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AuthorId = userRh.Id,
            Type = PostType.Announcement,
            Title = "🚀 Bem-vindos ao SocialTool RH!",
            Content = "É com muito orgulho que estreamos nossa nova plataforma de engajamento, cultura e reconhecimento contínuo! Aqui vamos celebrar conquistas, trocar elogios com SocialCoins, acompanhar nossos 1-on-1s e construir o futuro juntos. Aproveite sua cota mensal de moedas para reconhecer quem faz a diferença!",
            IsPinned = true,
            CreatedAt = DateTime.UtcNow.AddHours(-10)
        };

        // Reações ao anúncio
        postAnuncio.Reactions.Add(new PostReaction { PostId = postAnuncio.Id, UserId = userAdmin.Id, Type = ReactionType.Rocket });
        postAnuncio.Reactions.Add(new PostReaction { PostId = postAnuncio.Id, UserId = userDevFrontend.Id, Type = ReactionType.Heart });
        postAnuncio.Reactions.Add(new PostReaction { PostId = postAnuncio.Id, UserId = userDevBackend.Id, Type = ReactionType.Clap });

        // Comentários ao anúncio
        postAnuncio.Comments.Add(new PostComment
        {
            PostId = postAnuncio.Id,
            AuthorId = userAdmin.Id,
            Content = "Parabéns a todo o time de RH e Engenharia pelo lançamento! Ficou incrível! 👏",
            CreatedAt = DateTime.UtcNow.AddHours(-8)
        });

        // Publicação 2: Reconhecimento com SocialCoins
        var postReconhecimento = new Post
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AuthorId = userGestor.Id,
            Type = PostType.Recognition,
            Content = "Mariana mandou muito bem na refatoração da nova interface do módulo de avaliações! Comunicação impecável e velocidade recorde na entrega. Obrigado pela dedicação!",
            CreatedAt = DateTime.UtcNow.AddHours(-3)
        };

        var recognition = new Recognition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SenderId = userGestor.Id,
            ReceiverId = userDevFrontend.Id,
            CompanyValueId = valEquipe.Id,
            PostId = postReconhecimento.Id,
            CoinsAmount = 30,
            Message = postReconhecimento.Content,
            CreatedAt = DateTime.UtcNow.AddHours(-3)
        };
        postReconhecimento.Recognition = recognition;

        postReconhecimento.Reactions.Add(new PostReaction { PostId = postReconhecimento.Id, UserId = userDevBackend.Id, Type = ReactionType.Clap });
        postReconhecimento.Reactions.Add(new PostReaction { PostId = postReconhecimento.Id, UserId = userDevFrontend.Id, Type = ReactionType.Heart });

        // Publicação 3: Celebração de Aniversário de Empresa
        var postCeleb = new Post
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AuthorId = userRh.Id,
            Type = PostType.Celebration,
            Title = "🎉 3 anos de casa!",
            Content = "Hoje comemoramos 3 anos da Juliana Santos na nossa jornada! Muito obrigado por transformar nossa cultura todos os dias com tanto brilho e dedicação. 🎂✨",
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
        postCeleb.Reactions.Add(new PostReaction { PostId = postCeleb.Id, UserId = userGestor.Id, Type = ReactionType.Party });
        postCeleb.Reactions.Add(new PostReaction { PostId = postCeleb.Id, UserId = userDevFrontend.Id, Type = ReactionType.Heart });

        context.Posts.AddRange(postAnuncio, postReconhecimento, postCeleb);
        context.Recognitions.Add(recognition);

        await context.SaveChangesAsync();
    }
}
