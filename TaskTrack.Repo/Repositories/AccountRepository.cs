using Microsoft.EntityFrameworkCore;
using TaskTrack.Repo.Data;
using TaskTrack.Repo.Models;
using AsyncTask = System.Threading.Tasks.Task;

namespace TaskTrack.Repo.Repositories;

public interface IAccountRepository
{
    System.Threading.Tasks.Task<SystemAccount?> ByEmail(string email, CancellationToken ct);
    System.Threading.Tasks.Task<SystemAccount?> ById(int accountId, CancellationToken ct);
    System.Threading.Tasks.Task<List<SystemAccount>> List(CancellationToken ct);
    System.Threading.Tasks.Task<bool> HasCreatedTasks(int accountId, CancellationToken ct);
    void Add(SystemAccount account);
    void Remove(SystemAccount account);
    AsyncTask Save(CancellationToken ct);
}

public sealed class AccountRepository(TaskManagementContext db) : IAccountRepository
{
    public System.Threading.Tasks.Task<SystemAccount?> ByEmail(string email, CancellationToken ct) => db.SystemAccounts
        .FirstOrDefaultAsync(a => a.Email.ToLower() == email.ToLower(), ct);
    public System.Threading.Tasks.Task<SystemAccount?> ById(int accountId, CancellationToken ct) => db.SystemAccounts
        .FirstOrDefaultAsync(a => a.AccountId == accountId, ct);
    public System.Threading.Tasks.Task<List<SystemAccount>> List(CancellationToken ct) => db.SystemAccounts
        .AsNoTracking().OrderBy(a => a.AccountId).ToListAsync(ct);
    public System.Threading.Tasks.Task<bool> HasCreatedTasks(int accountId, CancellationToken ct) => db.Tasks
        .AnyAsync(t => t.CreatedById == accountId, ct);
    public void Add(SystemAccount account) => db.SystemAccounts.Add(account);
    public void Remove(SystemAccount account) => db.SystemAccounts.Remove(account);
    public AsyncTask Save(CancellationToken ct) => db.SaveChangesAsync(ct);
}
