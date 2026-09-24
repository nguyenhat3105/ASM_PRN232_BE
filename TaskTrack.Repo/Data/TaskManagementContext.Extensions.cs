using Microsoft.EntityFrameworkCore;
namespace TaskTrack.Repo.Data;
public partial class TaskManagementContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Low (0) is a real priority. Use an out-of-range sentinel so EF sends 0
        // instead of omitting it and letting the database default to Medium (1).
        modelBuilder.Entity<Models.Task>().Property(t => t.Priority).HasSentinel((short)-1);
    }
}
