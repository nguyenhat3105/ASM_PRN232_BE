using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TaskTrack.Repo.Data;
using TaskTrack.Repo.Repositories;
using TaskTrack.Service;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("TaskManagement");
var url = builder.Configuration["DATABASE_URL"];
if (!string.IsNullOrWhiteSpace(url))
{
    if (url.StartsWith("postgres://") || url.StartsWith("postgresql://"))
    {
        var uri = new Uri(url); var credentials = uri.UserInfo.Split(':',2);
        var parsed = new NpgsqlConnectionStringBuilder { Host=uri.Host, Port=uri.Port>0?uri.Port:5432,
            Database=Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')), Username=Uri.UnescapeDataString(credentials[0]),
            Password=credentials.Length>1?Uri.UnescapeDataString(credentials[1]):"", SslMode=SslMode.Prefer };
        connection=parsed.ConnectionString;
    }
    else connection=url;
}
builder.Services.AddDbContext<TaskManagementContext>(options=>options.UseNpgsql(connection));
builder.Services.AddScoped<IWorkspaceRepository,WorkspaceRepository>();
builder.Services.AddScoped<IWorkspaceService,WorkspaceService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(TimeZoneInfo.FindSystemTimeZoneById(builder.Configuration["BusinessTimeZone"]??"Asia/Ho_Chi_Minh"));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options=>options.AddDefaultPolicy(policy=>policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>()??["http://localhost:3000"])
    .AllowAnyHeader().AllowAnyMethod()));
var app=builder.Build();
app.UseExceptionHandler(handler=>handler.Run(async context=>
{
    var exception=context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var status=500;var message="An unexpected error occurred. Please try again.";string? field=null;
    if(exception is BusinessException business){status=business.Status;message=business.Message;field=business.Field;}
    else if(exception is DbUpdateException {InnerException:PostgresException pg})
    {
        if(pg.SqlState==PostgresErrorCodes.UniqueViolation){status=400;message="This value already exists.";field="tagName";}
        else if(pg.SqlState==PostgresErrorCodes.ForeignKeyViolation){status=400;message="Linked records prevent this operation. Refresh and try again.";field="relationship";}
    }
    context.Response.StatusCode=status;
    ProblemDetails problem=field is null?new ProblemDetails():new ValidationProblemDetails(new Dictionary<string,string[]>{{field,[message]}});
    problem.Status=status;problem.Title=status==404?"Not found":status==400?"Validation failed":"Server error";problem.Detail=message;
    problem.Extensions["traceId"]=context.TraceIdentifier;
    await context.Response.WriteAsJsonAsync(problem,cancellationToken:context.RequestAborted);
}));
app.UseCors();
if(app.Environment.IsDevelopment()||builder.Configuration.GetValue<bool>("Swagger:Enabled")){app.UseSwagger();app.UseSwaggerUI();}
app.MapGet("/health/live",()=>Results.Ok(new{status="ok"}));
app.MapGet("/health/ready",async(IWorkspaceService service,CancellationToken ct)=>await service.DatabaseReady(ct)?Results.Ok(new{status="ready"}):Results.StatusCode(503));
app.MapControllers();
app.Run();
public partial class Program { }
