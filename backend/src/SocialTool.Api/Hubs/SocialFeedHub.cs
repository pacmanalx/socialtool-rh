using Microsoft.AspNetCore.SignalR;
using SocialTool.Application.Common.Interfaces;

namespace SocialTool.Api.Hubs;

public class SocialFeedHub : Hub
{
    private readonly ITenantContext _tenantContext;

    public SocialFeedHub(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override async Task OnConnectedAsync()
    {
        if (_tenantContext.HasTenant)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{_tenantContext.TenantId}");
        }
        await base.OnConnectedAsync();
    }

    public async Task JoinTenantGroup(string tenantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
    }
}
