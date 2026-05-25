using Kvitoria.Data;
using Kvitoria.Models.Reporting;
using Kvitoria.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Kvitoria.Services.Admin;

public class AdminAnalyticsService(KvitoriaDbContext dbContext) : IAdminAnalyticsService
{
    public async Task<AdminDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var users = await dbContext.Users.AsNoTracking().ToListAsync(cancellationToken);
        var totalPlants = await dbContext.Plants.AsNoTracking().CountAsync(cancellationToken);
        var plantSpeciesCount = await dbContext.PlantSpecies.AsNoTracking().CountAsync(cancellationToken);
        var plantVarietiesCount = await dbContext.PlantVarieties.AsNoTracking().CountAsync(cancellationToken);
        var careLogsCount = await dbContext.CareLogs.AsNoTracking().CountAsync(cancellationToken);
        var feedbackUnreadCount = await dbContext.FeedbackMessages.AsNoTracking()
            .CountAsync(message => !message.IsRead, cancellationToken);

        var activeUsers = users.Count(user => !user.IsDeleted);
        var deletedUsers = users.Count(user => user.IsDeleted);
        var averagePlants = activeUsers == 0 ? 0 : (decimal)totalPlants / activeUsers;

        var metrics = BuildMetrics(
            new ReportMetric("Зареєстровано користувачів", users.Count),
            new ReportMetric("Активних користувачів", activeUsers),
            new ReportMetric("Видалених користувачів", deletedUsers),
            new ReportMetric("Рослин у базі", totalPlants),
            new ReportMetric("Видів рослин у каталозі", plantSpeciesCount),
            new ReportMetric("Форм і сортів у каталозі", plantVarietiesCount),
            new ReportMetric("Середня кількість рослин у користувачів", averagePlants),
            new ReportMetric("Записів догляду", careLogsCount),
            new ReportMetric("Непрочитаних звернень", feedbackUnreadCount));

        return new AdminDashboardViewModel
        {
            RegisteredUsers = users.Count,
            ActiveUsers = activeUsers,
            DeletedUsers = deletedUsers,
            TotalPlants = totalPlants,
            PlantSpeciesCount = plantSpeciesCount,
            PlantVarietiesCount = plantVarietiesCount,
            AveragePlantsPerUser = averagePlants,
            CareLogsCount = careLogsCount,
            FeedbackUnreadCount = feedbackUnreadCount,
            Metrics = metrics
        };
    }

    private static IReadOnlyList<ReportMetric> BuildMetrics(params ReportMetric[] metrics)
    {
        var validMetrics = new List<ReportMetric>();
        var total = 0;

        foreach (var metric in metrics)
        {
            AddMetric(ref total, validMetrics, metric);
        }

        return validMetrics;
    }

    private static void AddMetric(ref int total, ICollection<ReportMetric> metrics, ReportMetric metric)
    {
        if (ReportMetric.TryCreate(metric.Name, metric.Value, out var parsedMetric))
        {
            metrics.Add(parsedMetric);
            total++;
        }
    }
}
