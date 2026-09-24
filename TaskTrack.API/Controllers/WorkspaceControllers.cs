using Microsoft.AspNetCore.Mvc;
using TaskTrack.Service;
using TaskTrack.Service.Contracts;
namespace TaskTrack.API.Controllers;
[ApiController, Route("api/departments")]
public sealed class DepartmentsController(IWorkspaceService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(await service.Departments(null,ct));
    [HttpGet("search")] public async Task<IActionResult> Search([FromQuery]string? name,CancellationToken ct) => Ok(await service.Departments(name,ct));
    [HttpGet("{id:int}")] public async Task<IActionResult> Get(int id,CancellationToken ct) => Ok(await service.Department(id,ct));
    [HttpPost] public async Task<IActionResult> Create(DepartmentRequest request,CancellationToken ct) {var item=await service.SaveDepartment(null,request,ct);return CreatedAtAction(nameof(Get),new{id=item.DepartmentId},item);}
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id,DepartmentRequest request,CancellationToken ct) => Ok(await service.SaveDepartment(id,request,ct));
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id,CancellationToken ct) {await service.DeleteDepartment(id,ct);return NoContent();}
}
[ApiController, Route("api/projects")]
public sealed class ProjectsController(IWorkspaceService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(await service.Projects(new(),ct));
    [HttpGet("search")] public async Task<IActionResult> Search([FromQuery]ProjectFilter filter,CancellationToken ct) => Ok(await service.Projects(filter,ct));
    [HttpGet("department/{departmentId:int}")] public async Task<IActionResult> ByDepartment(int departmentId,CancellationToken ct) {await service.Department(departmentId,ct);return Ok(await service.Projects(new(){DepartmentId=departmentId},ct));}
    [HttpGet("{id:int}")] public async Task<IActionResult> Get(int id,CancellationToken ct) => Ok(await service.Project(id,ct));
    [HttpPost] public async Task<IActionResult> Create(ProjectRequest request,CancellationToken ct) {var item=await service.SaveProject(null,request,ct);return CreatedAtAction(nameof(Get),new{id=item.ProjectId},item);}
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id,ProjectRequest request,CancellationToken ct) => Ok(await service.SaveProject(id,request,ct));
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id,CancellationToken ct) {await service.DeleteProject(id,ct);return NoContent();}
}
[ApiController, Route("api/tasks")]
public sealed class TasksController(IWorkspaceService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(await service.Tasks(new(),ct));
    [HttpGet("search")] public async Task<IActionResult> Search([FromQuery]TaskFilter filter,CancellationToken ct) => Ok(await service.Tasks(filter,ct));
    [HttpGet("project/{projectId:int}")] public async Task<IActionResult> ByProject(int projectId,CancellationToken ct) {await service.Project(projectId,ct);return Ok(await service.Tasks(new(){ProjectId=projectId},ct));}
    [HttpGet("{id:int}")] public async Task<IActionResult> Get(int id,CancellationToken ct) => Ok(await service.TaskItem(id,ct));
    [HttpPost] public async Task<IActionResult> Create(TaskRequest request,CancellationToken ct) {var item=await service.SaveTask(null,request,ct);return CreatedAtAction(nameof(Get),new{id=item.TaskId},item);}
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id,TaskRequest request,CancellationToken ct) => Ok(await service.SaveTask(id,request,ct));
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id,CancellationToken ct) {await service.DeleteTask(id,ct);return NoContent();}
    [HttpGet("trash")] public async Task<IActionResult> Trash(CancellationToken ct) => Ok(await service.Trash(ct));
    [HttpPost("{id:int}/restore")] public async Task<IActionResult> Restore(int id,CancellationToken ct) => Ok(await service.RestoreTask(id,ct));
    [HttpPatch("{id:int}/status")] public async Task<IActionResult> Status(int id,StatusRequest request,CancellationToken ct) => Ok(await service.ChangeTaskStatus(id,request.Status!.Value,ct));
}
[ApiController, Route("api/tags")]
public sealed class TagsController(IWorkspaceService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(await service.Tags(ct));
    [HttpPost] public async Task<IActionResult> Create(TagRequest request,CancellationToken ct) {var item=await service.SaveTag(null,request,ct);return StatusCode(201,item);}
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id,TagRequest request,CancellationToken ct) => Ok(await service.SaveTag(id,request,ct));
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id,CancellationToken ct) {await service.DeleteTag(id,ct);return NoContent();}
}
