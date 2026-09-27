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
public class FeedController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IHubContext<SocialFeedHub> _hubContext;

    public FeedController(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IHubContext<SocialFeedHub> hubContext)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _hubContext = hubContext;
    }

    [HttpGet("posts")]
    public async Task<ActionResult<IEnumerable<PostDto>>> GetPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentUserId = _currentUser.UserId;

        var posts = await _dbContext.Posts
            .Include(p => p.Author)
            .Include(p => p.Reactions)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Author)
            .Include(p => p.Recognition)
                .ThenInclude(r => r!.Sender)
            .Include(p => p.Recognition)
                .ThenInclude(r => r!.Receiver)
            .Include(p => p.Recognition)
                .ThenInclude(r => r!.CompanyValue)
            .OrderByDescending(p => p.IsPinned)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = posts.Select(p => MapToPostDto(p, currentUserId)).ToList();
        return Ok(result);
    }

    [Authorize]
    [HttpPost("posts")]
    public async Task<ActionResult<PostDto>> CreatePost([FromBody] CreatePostRequest request)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        if (request.Type == PostType.Announcement &&
            _currentUser.Role is not (nameof(UserRole.HR) or nameof(UserRole.Admin)))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Só o RH e administradores publicam comunicados oficiais." });
        }

        var post = new Post
        {
            AuthorId = _currentUser.UserId.Value,
            Type = request.Type,
            Title = request.Title,
            Content = request.Content,
            ImageUrl = request.ImageUrl,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Posts.Add(post);
        await _dbContext.SaveChangesAsync();

        // Recarrega post com autor
        var savedPost = await _dbContext.Posts
            .Include(p => p.Author)
            .FirstAsync(p => p.Id == post.Id);

        var postDto = MapToPostDto(savedPost, _currentUser.UserId);

        // Notifica em tempo real via SignalR
        await _hubContext.Clients.All.SendAsync("ReceiveNewPost", postDto);

        return Ok(postDto);
    }

    [Authorize]
    [HttpPost("posts/{id:guid}/react")]
    public async Task<ActionResult<PostDto>> ToggleReaction(Guid id, [FromBody] ReactPostRequest request)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var userId = _currentUser.UserId.Value;

        var post = await _dbContext.Posts
            .Include(p => p.Author)
            .Include(p => p.Reactions)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Author)
            .Include(p => p.Recognition)
                .ThenInclude(r => r!.Sender)
            .Include(p => p.Recognition)
                .ThenInclude(r => r!.Receiver)
            .Include(p => p.Recognition)
                .ThenInclude(r => r!.CompanyValue)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
            return NotFound(new { message = "Publicação não encontrada." });

        var existingReaction = post.Reactions.FirstOrDefault(r => r.UserId == userId && r.Type == request.Type);

        if (existingReaction != null)
        {
            _dbContext.PostReactions.Remove(existingReaction);
            post.Reactions.Remove(existingReaction);
        }
        else
        {
            var newReaction = new PostReaction
            {
                PostId = post.Id,
                UserId = userId,
                Type = request.Type,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.PostReactions.Add(newReaction);
            post.Reactions.Add(newReaction);
        }

        await _dbContext.SaveChangesAsync();

        var postDto = MapToPostDto(post, userId);

        await _hubContext.Clients.All.SendAsync("ReceivePostUpdate", postDto);

        return Ok(postDto);
    }

    [Authorize]
    [HttpPost("posts/{id:guid}/comments")]
    public async Task<ActionResult<PostCommentDto>> AddComment(Guid id, [FromBody] CreateCommentRequest request)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { message = "Comentário não pode estar vazio." });

        var post = await _dbContext.Posts.FirstOrDefaultAsync(p => p.Id == id);
        if (post == null)
            return NotFound(new { message = "Publicação não encontrada." });

        var author = await _dbContext.Users.FirstAsync(u => u.Id == _currentUser.UserId.Value);

        var comment = new PostComment
        {
            PostId = id,
            AuthorId = author.Id,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.PostComments.Add(comment);
        await _dbContext.SaveChangesAsync();

        var commentDto = new PostCommentDto(
            comment.Id,
            author.Id,
            author.Name,
            author.AvatarUrl,
            comment.Content,
            comment.CreatedAt
        );

        await _hubContext.Clients.All.SendAsync("ReceivePostComment", new { PostId = id, Comment = commentDto });

        return Ok(commentDto);
    }

    private static PostDto MapToPostDto(Post p, Guid? currentUserId)
    {
        var reactionsByType = p.Reactions
            .GroupBy(r => r.Type.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var currentUserReactions = currentUserId.HasValue
            ? p.Reactions.Where(r => r.UserId == currentUserId.Value).Select(r => r.Type.ToString()).ToList()
            : new List<string>();

        RecognitionSummaryDto? recognitionDto = null;
        if (p.Recognition != null)
        {
            recognitionDto = new RecognitionSummaryDto(
                p.Recognition.Id,
                p.Recognition.SenderId,
                p.Recognition.Sender.Name,
                p.Recognition.ReceiverId,
                p.Recognition.Receiver.Name,
                p.Recognition.Receiver.AvatarUrl,
                p.Recognition.CompanyValue.Title,
                p.Recognition.CompanyValue.Icon,
                p.Recognition.CoinsAmount
            );
        }

        var commentsDto = p.Comments
            .OrderBy(c => c.CreatedAt)
            .Select(c => new PostCommentDto(
                c.Id,
                c.AuthorId,
                c.Author?.Name ?? "Colaborador",
                c.Author?.AvatarUrl,
                c.Content,
                c.CreatedAt
            ))
            .ToList();

        return new PostDto(
            p.Id,
            p.AuthorId,
            p.Author?.Name ?? "Colaborador",
            p.Author?.JobTitle ?? "",
            p.Author?.AvatarUrl,
            p.Type,
            p.Title,
            p.Content,
            p.ImageUrl,
            p.IsPinned,
            p.CreatedAt,
            p.Reactions.Count,
            reactionsByType,
            currentUserReactions,
            recognitionDto,
            commentsDto
        );
    }
}
