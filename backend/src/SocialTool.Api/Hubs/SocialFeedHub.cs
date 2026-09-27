using Microsoft.AspNetCore.SignalR;

namespace SocialTool.Api.Hubs;

// Só aceita conexões autenticadas (política padrão de Program.cs); os controllers publicam para Clients.All.
public class SocialFeedHub : Hub
{
}
