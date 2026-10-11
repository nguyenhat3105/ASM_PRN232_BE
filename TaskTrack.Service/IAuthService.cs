using TaskTrack.Service.Contracts;

namespace TaskTrack.Service;

public interface IAuthService
{
    Task<AccountDto> Register(RegisterRequest request, CancellationToken ct);
    Task<AuthenticatedAccountDto> Login(LoginRequest request, CancellationToken ct);
    Task<IReadOnlyList<AccountDto>> Accounts(CancellationToken ct);
    Task<AccountDto> Account(int accountId, CancellationToken ct);
    Task<AccountDto> UpdateAccount(int accountId, AccountUpdateRequest request, CancellationToken ct);
    Task DeleteAccount(int accountId, CancellationToken ct);
}
