using Kvitoria.Models;

namespace Kvitoria.ViewModels;

public class DashboardViewModel
{
    public int TotalPlants { get; init; }

    public int NeedsAttentionCount { get; init; }

    public int DueTodayCount { get; init; }

    public int ArchivedCount { get; init; }

    public IReadOnlyList<Plant> PlantsNeedingCare { get; init; } = [];

    public IReadOnlyList<Plant> NewestPlants { get; init; } = [];

    public IReadOnlyList<CareLog> RecentCareLogs { get; init; } = [];

    public DatabaseIssueViewModel? DatabaseIssue { get; init; }

    public bool IsDatabaseAvailable => DatabaseIssue is null;

    public static DashboardViewModel Unavailable(Exception exception)
    {
        return new DashboardViewModel
        {
            DatabaseIssue = DatabaseIssueViewModel.From(exception)
        };
    }
}
