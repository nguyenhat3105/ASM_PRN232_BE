using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTrack.Service;
using TaskTrack.Service.Contracts;

namespace TaskTrack.API.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(IAuthService service) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var account = await service.Register(request, ct);
        return StatusCode(StatusCodes.Status201Created, account);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct) => Ok(await service.Login(request, ct));
}

[ApiController, Authorize(Roles = "Admin"), Route("api/accounts")]
public sealed class AccountsController(IAuthService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(await service.Accounts(ct));
    [HttpGet("{id:int}")] public async Task<IActionResult> Get(int id, CancellationToken ct) => Ok(await service.Account(id, ct));
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, AccountUpdateRequest request, CancellationToken ct) => Ok(await service.UpdateAccount(id, request, ct));
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) { await service.DeleteAccount(id, ct); return NoContent(); }
}
