using TaskTrack.Service.Contracts;

namespace TaskTrack.Service;

public interface IWorkspaceService
{
    Task<IReadOnlyList<DepartmentDto>> Departments(string? name, CancellationToken ct);
    Task<DepartmentDto> Department(int id, CancellationToken ct);
    Task<DepartmentDto> SaveDepartment(int? id, DepartmentRequest request, CancellationToken ct);
    Task DeleteDepartment(int id, CancellationToken ct);
    Task<IReadOnlyList<ProjectDto>> Projects(ProjectFilter filter, CancellationToken ct);
    Task<ProjectDto> Project(int id, CancellationToken ct);
    Task<ProjectDto> SaveProject(int? id, ProjectRequest request, CancellationToken ct);
    Task DeleteProject(int id, CancellationToken ct);
    Task<IReadOnlyList<TaskDto>> Tasks(TaskFilter filter, CancellationToken ct);
    Task<TaskDto> TaskItem(int id, CancellationToken ct);
    Task<TaskDto> SaveTask(int? id, TaskRequest request, CancellationToken ct);
    Task DeleteTask(int id, CancellationToken ct);
    Task<IReadOnlyList<TagDto>> Tags(CancellationToken ct);
    Task<TagDto> SaveTag(int? id, TagRequest request, CancellationToken ct);
    Task DeleteTag(int id, CancellationToken ct);
    Task<IReadOnlyList<TaskDto>> Trash(CancellationToken ct);
    Task<TaskDto> RestoreTask(int id, CancellationToken ct);
    Task<TaskDto> ChangeTaskStatus(int id, short status, CancellationToken ct);
    Task<bool> DatabaseReady(CancellationToken ct);
}
