using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SocialTool.Api.DTOs;
using SocialTool.Api.Hubs;
using SocialTool.Application.Common.Interfaces;
using SocialTool.Domain.Entities;
using SocialTool.Domain.Enums;
using SocialTool.Infrastructure.Persistence;

namespace SocialTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecognitionController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IHubContext<SocialFeedHub> _hubContext;

    public RecognitionController(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IHubContext<SocialFeedHub> hubContext)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _hubContext = hubContext;
    }

    [HttpGet("values")]
    public async Task<ActionResult<IEnumerable<CompanyValueDto>>> GetCompanyValues()
    {
        var values = await _dbContext.CompanyValues
            .Where(v => v.IsActive)
            .OrderBy(v => v.Title)
            .Select(v => new CompanyValueDto(v.Id, v.Title, v.Description, v.Icon))
            .ToListAsync();

        return Ok(values);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<PostDto>> SendRecognition([FromBody] SendRecognitionRequest request)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var senderId = _currentUser.UserId.Value;

        if (senderId == request.ReceiverId)
            return BadRequest(new { message = "Você não pode enviar reconhecimento para si mesmo." });

        if (request.CoinsAmount <= 0)
            return BadRequest(new { message = "A quantidade de moedas deve ser maior que zero." });

        var sender = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == senderId);
        if (sender == null)
            return NotFound(new { message = "Remetente não encontrado." });

        if (sender.CoinsAvailableToGive < request.CoinsAmount)
        {
            return BadRequest(new { message = $"Saldo insuficiente. Você possui {sender.CoinsAvailableToGive} moedas disponíveis para doação." });
        }

        var receiver = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.ReceiverId && u.IsActive);
        if (receiver == null)
            return NotFound(new { message = "Colaborador destinatário não encontrado." });

        var companyValue = await _dbContext.CompanyValues.FirstOrDefaultAsync(v => v.Id == request.CompanyValueId && v.IsActive);
        if (companyValue == null)
            return NotFound(new { message = "Valor corporativo não encontrado." });

        // Debita moedas do remetente e credita no saldo do destinatário
        sender.CoinsAvailableToGive -= request.CoinsAmount;
        receiver.CoinsBalanceToSpend += request.CoinsAmount;

        // Cria post no mural social
        var post = new Post
        {
            AuthorId = senderId,
            Type = PostType.Recognition,
            Content = request.Message,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Posts.Add(post);

        // Cria registro de reconhecimento
        var recognition = new Recognition
        {
            SenderId = senderId,
            ReceiverId = receiver.Id,
            CompanyValueId = companyValue.Id,
            PostId = post.Id,
            CoinsAmount = request.CoinsAmount,
            Message = request.Message,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Recognitions.Add(recognition);

        await _dbContext.SaveChangesAsync();

        var postDto = new PostDto(
            post.Id,
            sender.Id,
            sender.Name,
            sender.JobTitle,
            sender.AvatarUrl,
            post.Type,
            post.Title,
            post.Content,
            post.ImageUrl,
            false,
            post.CreatedAt,
            0,
            new Dictionary<string, int>(),
            new List<string>(),
            new RecognitionSummaryDto(
                recognition.Id,
                sender.Id,
                sender.Name,
                receiver.Id,
                receiver.Name,
                receiver.AvatarUrl,
                companyValue.Title,
                companyValue.Icon,
                recognition.CoinsAmount
            ),
            new List<PostCommentDto>()
        );

        await _hubContext.Clients.All.SendAsync("ReceiveNewPost", postDto);

        return Ok(postDto);
    }

    [HttpGet("leaderboard")]
    public async Task<ActionResult<IEnumerable<LeaderboardItemDto>>> GetLeaderboard()
    {
        var topUsers = await _dbContext.Users
            .Where(u => u.IsActive)
            .OrderByDescending(u => u.CoinsBalanceToSpend)
            .Take(10)
            .Select(u => new LeaderboardItemDto(
                u.Id,
                u.Name,
                u.JobTitle,
                u.AvatarUrl,
                u.CoinsBalanceToSpend,
                u.RecognitionsReceived.Count
            ))
            .ToListAsync();

        return Ok(topUsers);
    }
}
