using SocialTool.Domain.Common;

namespace SocialTool.Domain.Entities;

// Permissão administrativa concedida a uma pessoa (chaves em Domain.Authorization.Permissions).
// Administradores não têm linhas aqui: eles podem tudo.
public class UserPermission : BaseEntity
{
    public Guid UserId { get; set; }
    public string Permission { get; set; } = string.Empty;
    public Guid? GrantedById { get; set; }

    public User User { get; set; } = null!;
}
