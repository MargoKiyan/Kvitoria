using Kvitoria.Models;
using Kvitoria.Models.Auth;
using Kvitoria.Models.Feedback;
using Kvitoria.Models.Enums;
using Kvitoria.Models.PlantCatalog;
using Kvitoria.Models.Reporting;
using Kvitoria.ViewModels.Validation;
using System.ComponentModel.DataAnnotations;

namespace Kvitoria.ViewModels;

public class AdminDashboardViewModel
{
    public int RegisteredUsers { get; init; }

    public int DeletedUsers { get; init; }

    public int ActiveUsers { get; init; }

    public int TotalPlants { get; init; }

    public int PlantSpeciesCount { get; init; }

    public int PlantVarietiesCount { get; init; }

    public decimal AveragePlantsPerUser { get; init; }

    public int CareLogsCount { get; init; }

    public int FeedbackUnreadCount { get; init; }

    public IReadOnlyList<ReportMetric> Metrics { get; init; } = [];
}

public class AdminUsersViewModel
{
    public string? Search { get; init; }

    public string Status { get; init; } = "all";

    public string Role { get; init; } = "all";

    public string Sort { get; init; } = "login_asc";

    public int TotalUsers { get; init; }

    public int ActiveUsers { get; init; }

    public int DeletedUsers { get; init; }

    public IReadOnlyList<AdminUserRowViewModel> Users { get; init; } = [];

    public PaginationViewModel Pagination { get; init; } = new();
}

public class AdminUserRowViewModel
{
    public ApplicationUser User { get; init; } = new();

    public IReadOnlyList<string> Roles { get; init; } = [];
}

public class AdminUserEditViewModel
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть логін.")]
    [RegularExpression(AccountValidationRules.LoginPattern, ErrorMessage = AccountValidationRules.LoginError)]
    [StringLength(40, MinimumLength = 5, ErrorMessage = "Логін має містити від 5 до 40 символів.")]
    [Display(Name = "Логін")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть ім'я.")]
    [StringLength(80, ErrorMessage = "Ім'я має бути до 80 символів.")]
    [Display(Name = "Ім'я")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть дату народження.")]
    [BirthDateRange]
    [DataType(DataType.Date)]
    [Display(Name = "Дата народження")]
    public DateOnly? BirthDate { get; set; }

    [Display(Name = "Акаунт видалено")]
    public bool IsDeleted { get; set; }
}

public class AdminPlantsViewModel
{
    public string? Search { get; init; }

    public string Sort { get; init; } = "species_asc";

    public string VariantFilter { get; init; } = "all";

    public int SpeciesCount { get; init; }

    public int VarietyCount { get; init; }

    public AdminPlantSpeciesFormViewModel SpeciesForm { get; init; } = new();

    public AdminPlantVarietyFormViewModel VarietyForm { get; init; } = new();

    public IReadOnlyList<PlantSpecies> SpeciesOptions { get; init; } = [];

    public IReadOnlyList<PlantSpecies> Species { get; init; } = [];

    public IReadOnlyList<PlantVariety> Varieties { get; init; } = [];

    public PaginationViewModel Pagination { get; init; } = new();
}

public class AdminPlantSpeciesFormViewModel
{
    [Required(ErrorMessage = "Вкажіть назву виду.")]
    [StringLength(120, ErrorMessage = "Назва виду має бути до 120 символів.")]
    [Display(Name = "Назва виду")]
    public string Name { get; set; } = string.Empty;
}

public class AdminPlantVarietyFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Оберіть вид.")]
    [Display(Name = "Вид рослини")]
    public int PlantSpeciesId { get; set; }

    [Required(ErrorMessage = "Вкажіть назву форми або сорту.")]
    [StringLength(80, ErrorMessage = "Назва має бути до 80 символів.")]
    [Display(Name = "Назва")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Тип")]
    public PlantVariantType Type { get; set; } = PlantVariantType.Variety;
}

public class AdminFeedbackViewModel
{
    public IReadOnlyList<FeedbackMessage> Messages { get; init; } = [];

    public PaginationViewModel Pagination { get; init; } = new();
}

public class AdminActionResult
{
    private AdminActionResult(bool succeeded, bool notFound, string message)
    {
        Succeeded = succeeded;
        NotFound = notFound;
        Message = message;
    }

    public bool Succeeded { get; }

    public bool NotFound { get; }

    public string Message { get; }

    public static AdminActionResult Success(string message)
    {
        return new AdminActionResult(true, false, message);
    }

    public static AdminActionResult Failure(string message)
    {
        return new AdminActionResult(false, false, message);
    }

    public static AdminActionResult Missing(string message = "Запис не знайдено.")
    {
        return new AdminActionResult(false, true, message);
    }
}
