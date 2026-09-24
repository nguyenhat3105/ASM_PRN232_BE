using Microsoft.EntityFrameworkCore;
using TaskTrack.Repo.Data;
using TaskTrack.Repo.Models;
using WorkTask = TaskTrack.Repo.Models.Task;
using AsyncTask = System.Threading.Tasks.Task;

namespace TaskTrack.Repo.Repositories;

public interface IWorkspaceRepository
{
    Task<List<Department>> Departments(string? name, CancellationToken ct);
    Task<Department?> Department(int id, CancellationToken ct);
    Task<List<Project>> Projects(string? name, short? status, int? departmentId, CancellationToken ct);
    Task<Project?> Project(int id, CancellationToken ct);
    Task<List<WorkTask>> Tasks(string? title, short? status, short? priority, int? projectId, int? tagId, DateOnly? overdueBefore, bool trash, CancellationToken ct);
    Task<WorkTask?> TaskItem(int id, CancellationToken ct);
    Task<List<Tag>> Tags(CancellationToken ct);
    Task<Tag?> Tag(int id, CancellationToken ct);
    Task<bool> HasProjects(int id, CancellationToken ct);
    Task<bool> HasTasks(int id, CancellationToken ct);
    Task<bool> TagUsed(int id, CancellationToken ct);
    Task<bool> TagNameExists(string name, int? except, CancellationToken ct);
    void Add<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;
    AsyncTask Save(CancellationToken ct);
    Task<bool> Ready(CancellationToken ct);
}

public sealed class WorkspaceRepository(TaskManagementContext db) : IWorkspaceRepository
{
    public Task<List<Department>> Departments(string? name, CancellationToken ct) => db.Departments
        .AsNoTracking().Include(d => d.Projects.Where(p => p.IsActive))
        .Where(d => d.IsActive && (name == null || d.DepartmentName.ToLower().Contains(name.ToLower())))
        .OrderBy(d => d.DepartmentId).ToListAsync(ct);
    public Task<Department?> Department(int id, CancellationToken ct) => db.Departments
        .Include(d => d.Projects).ThenInclude(p => p.Tasks).AsSplitQuery()
        .FirstOrDefaultAsync(d => d.DepartmentId == id, ct);
    public Task<List<Project>> Projects(string? name, short? status, int? departmentId, CancellationToken ct) => db.Projects
        .AsNoTracking().Include(p => p.Department).Include(p => p.Tasks.Where(t => t.IsActive))
        .Where(p => p.IsActive && p.Department.IsActive && (name == null || p.ProjectName.ToLower().Contains(name.ToLower()))
            && (!status.HasValue || p.Status == status) && (!departmentId.HasValue || p.DepartmentId == departmentId))
        .OrderBy(p => p.ProjectId).AsSplitQuery().ToListAsync(ct);
    public Task<Project?> Project(int id, CancellationToken ct) => db.Projects.Include(p => p.Department)
        .Include(p => p.Tasks).ThenInclude(t => t.Tags).AsSplitQuery().FirstOrDefaultAsync(p => p.ProjectId == id, ct);
    public Task<List<WorkTask>> Tasks(string? title, short? status, short? priority, int? projectId, int? tagId, DateOnly? overdueBefore, bool trash, CancellationToken ct) => db.Tasks
        .AsNoTracking().Include(t => t.Project).ThenInclude(p => p.Department).Include(t => t.Tags)
        .Where(t => t.IsActive != trash && (trash || (t.Project.IsActive && t.Project.Department.IsActive))
            && (title == null || t.Title.ToLower().Contains(title.ToLower()))
            && (!status.HasValue || t.Status == status) && (!priority.HasValue || t.Priority == priority)
            && (!projectId.HasValue || t.ProjectId == projectId) && (!tagId.HasValue || t.Tags.Any(tag => tag.TagId == tagId))
            && (!overdueBefore.HasValue || (t.DueDate < overdueBefore && t.Status < 2)))
        .OrderBy(t => t.TaskId).AsSplitQuery().ToListAsync(ct);
    public Task<WorkTask?> TaskItem(int id, CancellationToken ct) => db.Tasks.Include(t => t.Project).ThenInclude(p => p.Department)
        .Include(t => t.Tags).FirstOrDefaultAsync(t => t.TaskId == id, ct);
    public Task<List<Tag>> Tags(CancellationToken ct) => db.Tags.OrderBy(t => t.TagName).ToListAsync(ct);
    public Task<Tag?> Tag(int id, CancellationToken ct) => db.Tags.FirstOrDefaultAsync(t => t.TagId == id, ct);
    public Task<bool> HasProjects(int id, CancellationToken ct) => db.Projects.AnyAsync(p => p.DepartmentId == id, ct);
    public Task<bool> HasTasks(int id, CancellationToken ct) => db.Tasks.AnyAsync(t => t.ProjectId == id, ct);
    public Task<bool> TagUsed(int id, CancellationToken ct) => db.Tags.AnyAsync(t => t.TagId == id && t.Tasks.Any(), ct);
    public Task<bool> TagNameExists(string name, int? except, CancellationToken ct) => db.Tags.AnyAsync(t => t.TagName.ToLower() == name.ToLower() && (!except.HasValue || t.TagId != except), ct);
    public void Add<T>(T entity) where T : class => db.Add(entity);
    public void Remove<T>(T entity) where T : class => db.Remove(entity);
    public async AsyncTask Save(CancellationToken ct) => await db.SaveChangesAsync(ct);
    public Task<bool> Ready(CancellationToken ct) => db.Database.CanConnectAsync(ct);
}
