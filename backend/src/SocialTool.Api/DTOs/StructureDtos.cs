namespace SocialTool.Api.DTOs;

public record AreaDto(Guid Id, string Name, string Kind, Guid? ParentId, string Path, int MemberCount, int TotalMembers);

public record SaveAreaRequest(string? Name, string? Kind, Guid? ParentId);

public record AreaMemberDto(Guid Id, string Name, string Email, string JobTitle, bool IsActive);

public record AreaMembersRequest(IReadOnlyList<Guid>? UserIds);
