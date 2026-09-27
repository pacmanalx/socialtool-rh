using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using SocialTool.Api.Services;

namespace SocialTool.Api.Authorization;

// [RequirePermission(Permissions.UsersInvite)] no controller ou na action.
// Vira a política "perm:<chave>", montada sob demanda pelo PermissionPolicyProvider.
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string Prefix = "perm:";

    public RequirePermissionAttribute(string permission) : base(Prefix + permission)
    {
    }
}

// Só para quem é Admin (configurações da organização, conceder permissões, mexer em administradores).
public class RequireAdminAttribute : AuthorizeAttribute
{
    public const string PolicyName = "admin";

    public RequireAdminAttribute() : base(PolicyName)
    {
    }
}

public record PermissionRequirement(string? Permission) : IAuthorizationRequirement;

public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName == RequireAdminAttribute.PolicyName)
            return Build(null);
        if (policyName.StartsWith(RequirePermissionAttribute.Prefix, StringComparison.Ordinal))
            return Build(policyName[RequirePermissionAttribute.Prefix.Length..]);
        return await base.GetPolicyAsync(policyName);
    }

    private static AuthorizationPolicy Build(string? permission) =>
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();
}

// Permission == null significa "só Admin".
public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly PermissionService _permissions;

    public PermissionHandler(PermissionService permissions)
    {
        _permissions = permissions;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var allowed = requirement.Permission == null
            ? await _permissions.IsAdminAsync()
            : await _permissions.HasAsync(requirement.Permission);
        if (allowed)
            context.Succeed(requirement);
    }
}
