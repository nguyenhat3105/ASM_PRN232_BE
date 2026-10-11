namespace TaskTrack.Service.Contracts;

public sealed record TagDto(int TagId, string TagName, string? Color);
public sealed record DepartmentDto(int DepartmentId, string DepartmentName, string DepartmentDescription,
    bool IsActive, int ProjectCount, IReadOnlyList<ProjectDto>? Projects = null);
public sealed record ProjectDto(int ProjectId, string ProjectName, string? Description, DateOnly StartDate,
    DateOnly? EndDate, short Status, int DepartmentId, string DepartmentName, bool IsActive,
    DateTime CreatedDate, int TaskCount, int CompletedTaskCount, IReadOnlyList<TaskDto>? Tasks = null);
public sealed record TaskDto(int TaskId, string Title, string? Description, short Status, short Priority,
    DateOnly? DueDate, int ProjectId, string ProjectName, bool IsActive, DateTime CreatedDate,
    DateTime? ModifiedDate, IReadOnlyList<TagDto> Tags);
public sealed record AccountDto(int AccountId, string FullName, string Email, short Role, DateTime CreatedDate);
public sealed record AuthenticatedAccountDto(string Token, AccountDto Account);
