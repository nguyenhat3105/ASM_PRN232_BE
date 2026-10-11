using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TaskTrack.Repo.Models;
using TaskTrack.Repo.Repositories;
using TaskTrack.Service.Contracts;
using AsyncTask = System.Threading.Tasks.Task;

namespace TaskTrack.Service;

public sealed class AuthService(IAccountRepository accounts, IConfiguration configuration, TimeProvider clock) : IAuthService
{
    private static AccountDto Map(SystemAccount account) => new(account.AccountId, account.FullName, account.Email, account.Role, account.CreatedDate);
    private string Secret => configuration["JWT_SECRET"] ?? configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("JWT_SECRET is not configured.");

    public async Task<AccountDto> Register(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await accounts.ByEmail(email, ct) is not null)
            throw new BusinessException(409, "An account with this email already exists.", "email");
        var account = new SystemAccount
        {
            FullName = request.FullName.Trim(), Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), Role = 0,
            CreatedDate = DateTime.SpecifyKind(clock.GetUtcNow().UtcDateTime, DateTimeKind.Unspecified)
        };
        accounts.Add(account);
        await accounts.Save(ct);
        return Map(account);
    }

    public async Task<AuthenticatedAccountDto> Login(LoginRequest request, CancellationToken ct)
    {
        var account = await accounts.ByEmail(request.Email.Trim(), ct);
        if (account is null || !BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
            throw new BusinessException(401, "Email or password is incorrect.");
        return new AuthenticatedAccountDto(CreateToken(account), Map(account));
    }

    public async Task<IReadOnlyList<AccountDto>> Accounts(CancellationToken ct) => (await accounts.List(ct)).Select(Map).ToArray();
    public async Task<AccountDto> Account(int accountId, CancellationToken ct) => Map(await Find(accountId, ct));
    public async Task<AccountDto> UpdateAccount(int accountId, AccountUpdateRequest request, CancellationToken ct)
    {
        var account = await Find(accountId, ct);
        account.FullName = request.FullName.Trim(); account.Role = request.Role;
        await accounts.Save(ct); return Map(account);
    }
    public async AsyncTask DeleteAccount(int accountId, CancellationToken ct)
    {
        var account = await Find(accountId, ct);
        if (await accounts.HasCreatedTasks(accountId, ct))
            throw BusinessException.Invalid("accountId", "Cannot delete an account that created tasks.");
        accounts.Remove(account); await accounts.Save(ct);
    }

    private async Task<SystemAccount> Find(int accountId, CancellationToken ct) => await accounts.ById(accountId, ct) ?? throw BusinessException.NotFound("Account");
    private string CreateToken(SystemAccount account)
    {
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
            new Claim(ClaimTypes.Email, account.Email),
            new Claim(ClaimTypes.Name, account.FullName),
            new Claim(ClaimTypes.Role, account.Role == 1 ? "Admin" : "Staff")
        };
        var token = new JwtSecurityToken(claims: claims, expires: clock.GetUtcNow().AddHours(24).UtcDateTime, signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
