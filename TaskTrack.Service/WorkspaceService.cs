using TaskTrack.Repo.Models;
using TaskTrack.Repo.Repositories;
using TaskTrack.Service.Contracts;
using WorkTask = TaskTrack.Repo.Models.Task;
using AsyncTask = System.Threading.Tasks.Task;

namespace TaskTrack.Service;

public sealed class WorkspaceService(IWorkspaceRepository repo, TimeProvider clock, TimeZoneInfo businessTimeZone) : IWorkspaceService
{
    private DateTime Now => DateTime.SpecifyKind(clock.GetUtcNow().UtcDateTime, DateTimeKind.Unspecified);
    private DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), businessTimeZone).DateTime);
    private static TagDto Map(Tag t) => new(t.TagId, t.TagName, t.Color);
    private static TaskDto Map(WorkTask t) => new(t.TaskId, t.Title, t.Description, t.Status, t.Priority, t.DueDate, t.ProjectId, t.Project.ProjectName, t.IsActive, t.CreatedDate, t.ModifiedDate, t.Tags.OrderBy(x => x.TagId).Select(Map).ToArray());
    private static ProjectDto Map(Project p, bool detail = false) => new(p.ProjectId, p.ProjectName, p.Description, p.StartDate, p.EndDate, p.Status, p.DepartmentId, p.Department.DepartmentName, p.IsActive, p.CreatedDate, p.Tasks.Count(t => t.IsActive), p.Tasks.Count(t => t.IsActive && t.Status == 2), detail ? p.Tasks.Where(t => t.IsActive).OrderBy(t => t.TaskId).Select(Map).ToArray() : null);
    private static DepartmentDto Map(Department d, bool detail = false) => new(d.DepartmentId, d.DepartmentName, d.DepartmentDescription, d.IsActive, d.Projects.Count(p => p.IsActive), detail ? d.Projects.Where(p => p.IsActive).OrderBy(p => p.ProjectId).Select(p => Map(p)).ToArray() : null);
    private async Task<Department> ActiveDepartment(int id, CancellationToken ct) { var d = await repo.Department(id, ct); return d is { IsActive: true } ? d : throw BusinessException.NotFound("Department"); }
    private async Task<Project> ActiveProject(int id, CancellationToken ct) { var p = await repo.Project(id, ct); return p is { IsActive: true, Department.IsActive: true } ? p : throw BusinessException.NotFound("Project"); }
    private async Task<WorkTask> ActiveTask(int id, CancellationToken ct) { var t = await repo.TaskItem(id, ct); return t is { IsActive: true, Project.IsActive: true, Project.Department.IsActive: true } ? t : throw BusinessException.NotFound("Task"); }
    public async Task<IReadOnlyList<DepartmentDto>> Departments(string? name, CancellationToken ct) => (await repo.Departments(name?.Trim(), ct)).Select(d => Map(d)).ToArray();
    public async Task<DepartmentDto> Department(int id, CancellationToken ct) => Map(await ActiveDepartment(id, ct), true);
    public async Task<DepartmentDto> SaveDepartment(int? id, DepartmentRequest request, CancellationToken ct)
    {
        var d = id.HasValue ? await ActiveDepartment(id.Value, ct) : new Department { IsActive = true };
        d.DepartmentName = request.DepartmentName.Trim(); d.DepartmentDescription = request.DepartmentDescription.Trim();
        if (!id.HasValue) repo.Add(d); await repo.Save(ct); return Map(d);
    }
    public async AsyncTask DeleteDepartment(int id, CancellationToken ct)
    { var d = await ActiveDepartment(id, ct); if (await repo.HasProjects(id, ct)) throw BusinessException.Invalid("departmentId", "Cannot delete a department with linked projects, including inactive projects."); repo.Remove(d); await repo.Save(ct); }
    public async Task<IReadOnlyList<ProjectDto>> Projects(ProjectFilter filter, CancellationToken ct) => (await repo.Projects(filter.Name?.Trim(), filter.Status, filter.DepartmentId, ct)).Select(p => Map(p)).ToArray();
    public async Task<ProjectDto> Project(int id, CancellationToken ct) => Map(await ActiveProject(id, ct), true);
    public async Task<ProjectDto> SaveProject(int? id, ProjectRequest request, CancellationToken ct)
    {
        var p = id.HasValue ? await ActiveProject(id.Value, ct) : new Project { IsActive = true, CreatedDate = Now };
        var department = await repo.Department(request.DepartmentId, ct);
        if (department is not { IsActive: true }) throw BusinessException.Invalid("departmentId", "Choose an active department.");
        p.ProjectName = request.ProjectName.Trim(); p.Description = request.Description?.Trim(); p.StartDate = request.StartDate!.Value; p.EndDate = request.EndDate; p.Status = request.Status; p.DepartmentId = department.DepartmentId; p.Department = department;
        if (!id.HasValue) repo.Add(p); await repo.Save(ct); return Map(p);
    }
    public async AsyncTask DeleteProject(int id, CancellationToken ct)
    { var p = await ActiveProject(id, ct); if (await repo.HasTasks(id, ct)) throw BusinessException.Invalid("projectId", "Cannot delete a project with linked tasks, including tasks in trash."); repo.Remove(p); await repo.Save(ct); }
    public async Task<IReadOnlyList<TaskDto>> Tasks(TaskFilter filter, CancellationToken ct) => (await repo.Tasks(filter.Title?.Trim(), filter.Status, filter.Priority, filter.ProjectId, filter.TagId, filter.Overdue == true ? Today : null, false, ct)).Select(Map).ToArray();
    public async Task<TaskDto> TaskItem(int id, CancellationToken ct) => Map(await ActiveTask(id, ct));
    public async Task<TaskDto> SaveTask(int? id, TaskRequest request, CancellationToken ct)
    {
        var t = id.HasValue ? await ActiveTask(id.Value, ct) : new WorkTask { IsActive = true, CreatedDate = Now };
        var p = await repo.Project(request.ProjectId, ct);
        if (p is not { IsActive: true, Department.IsActive: true }) throw BusinessException.Invalid("projectId", "Choose an active project in an active department.");
        var ids = (request.TagIDs ?? []).Distinct().ToArray(); var tags = (await repo.Tags(ct)).Where(tag => ids.Contains(tag.TagId)).ToArray();
        if (tags.Length != ids.Length) throw BusinessException.Invalid("tagIDs", "One or more selected tags do not exist.");
        t.Title = request.Title.Trim(); t.Description = request.Description?.Trim(); t.Status = request.Status; t.Priority = request.Priority; t.DueDate = request.DueDate; t.ProjectId = p.ProjectId; t.Project = p;
        foreach (var existing in t.Tags.Where(tag => !ids.Contains(tag.TagId)).ToArray()) t.Tags.Remove(existing);
        foreach (var tag in tags.Where(tag => t.Tags.All(existing => existing.TagId != tag.TagId))) t.Tags.Add(tag);
        if (id.HasValue) t.ModifiedDate = Now; else repo.Add(t);
        await repo.Save(ct); return Map(t);
    }
    public async AsyncTask DeleteTask(int id, CancellationToken ct) { var t = await ActiveTask(id, ct); t.IsActive = false; t.ModifiedDate = Now; await repo.Save(ct); }
    public async Task<IReadOnlyList<TagDto>> Tags(CancellationToken ct) => (await repo.Tags(ct)).Select(Map).ToArray();
    public async Task<TagDto> SaveTag(int? id, TagRequest request, CancellationToken ct)
    { var t = id.HasValue ? await repo.Tag(id.Value, ct) ?? throw BusinessException.NotFound("Tag") : new Tag(); var name = request.TagName.Trim(); if (await repo.TagNameExists(name, id, ct)) throw BusinessException.Invalid("tagName", "A tag with this name already exists."); t.TagName = name; t.Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.ToUpperInvariant(); if (!id.HasValue) repo.Add(t); await repo.Save(ct); return Map(t); }
    public async AsyncTask DeleteTag(int id, CancellationToken ct) { var t = await repo.Tag(id, ct) ?? throw BusinessException.NotFound("Tag"); if (await repo.TagUsed(id, ct)) throw BusinessException.Invalid("tagId", "Cannot delete a tag used by any task, including tasks in trash."); repo.Remove(t); await repo.Save(ct); }
    public async Task<IReadOnlyList<TaskDto>> Trash(CancellationToken ct) => (await repo.Tasks(null, null, null, null, null, null, true, ct)).Select(Map).ToArray();
    public async Task<TaskDto> RestoreTask(int id, CancellationToken ct) { var t = await repo.TaskItem(id, ct) ?? throw BusinessException.NotFound("Task"); if (t.IsActive) throw BusinessException.Invalid("taskId", "Task is already active."); if (!t.Project.IsActive || !t.Project.Department.IsActive) throw BusinessException.Invalid("projectId", "Restore requires an active project and department."); t.IsActive = true; t.ModifiedDate = Now; await repo.Save(ct); return Map(t); }
    public async Task<TaskDto> ChangeTaskStatus(int id, short status, CancellationToken ct) { var t = await ActiveTask(id, ct); t.Status = status; t.ModifiedDate = Now; await repo.Save(ct); return Map(t); }
    public Task<bool> DatabaseReady(CancellationToken ct) => repo.Ready(ct);
}
