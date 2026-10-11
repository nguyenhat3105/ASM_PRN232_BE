using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;
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
        var uri = new Uri(url); var credentials = uri.UserInfo.Split(':', 2);
        var parsed = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : "",
            SslMode = SslMode.Prefer
        };
        connection = parsed.ConnectionString;
    }
    else connection = url;
}
builder.Services.AddDbContext<TaskManagementContext>(options => options.UseNpgsql(connection));
builder.Services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(TimeZoneInfo.FindSystemTimeZoneById(builder.Configuration["BusinessTimeZone"] ?? "Asia/Ho_Chi_Minh"));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
var jwtSecret = builder.Configuration["JWT_SECRET"] ?? builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret)) throw new InvalidOperationException("JWT_SECRET must be configured.");
var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = true,
        ValidateIssuerSigningKey = true, IssuerSigningKey = jwtKey, ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization", In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3000"])
    .AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var status = 500; var message = "An unexpected error occurred. Please try again."; string? field = null;
    if (exception is BusinessException business) { status = business.Status; message = business.Message; field = business.Field; }
    else if (exception is DbUpdateException { InnerException: PostgresException pg })
    {
        if (pg.SqlState == PostgresErrorCodes.UniqueViolation) { status = 400; message = "This value already exists."; field = "tagName"; }
        else if (pg.SqlState == PostgresErrorCodes.ForeignKeyViolation) { status = 400; message = "Linked records prevent this operation. Refresh and try again."; field = "relationship"; }
    }
    context.Response.StatusCode = status;
    ProblemDetails problem = field is null ? new ProblemDetails() : new ValidationProblemDetails(new Dictionary<string, string[]> { { field, [message] } });
    problem.Status = status; problem.Title = status switch { 404 => "Not found", 400 => "Validation failed", 401 => "Unauthorized", 403 => "Forbidden", 409 => "Conflict", _ => "Server error" }; problem.Detail = message;
    problem.Extensions["traceId"] = context.TraceIdentifier;
    await context.Response.WriteAsJsonAsync(problem, cancellationToken: context.RequestAborted);
}));
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled")) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", async (IWorkspaceService service, CancellationToken ct) => await service.DatabaseReady(ct) ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503));
app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskManagementContext>();
    await AuthSchemaInitializer.EnsureCreated(db, CancellationToken.None);
    await AuthSchemaInitializer.EnsureAdmin(db, builder.Configuration["ADMIN_EMAIL"], builder.Configuration["ADMIN_PASSWORD"], builder.Configuration["ADMIN_FULL_NAME"], CancellationToken.None);
}
app.Run();
public partial class Program { }
