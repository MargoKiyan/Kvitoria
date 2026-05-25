using Kvitoria.Services;
using Kvitoria.Services.Admin;
using Kvitoria.Services.Auth;
using Kvitoria.Services.Images;
using Kvitoria.Services.Reports;

namespace Kvitoria.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKvitoriaServices(this IServiceCollection services)
    {
        services.AddScoped<IUserContext, HttpUserContext>();
        services.AddScoped<IPlantImageService, PlantImageService>();
        services.AddScoped<IPlantCollectionService, PlantCollectionService>();
        services.AddScoped<IAdminAnalyticsService, AdminAnalyticsService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminPlantCatalogService, AdminPlantCatalogService>();
        services.AddScoped<IAdminFeedbackService, AdminFeedbackService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
