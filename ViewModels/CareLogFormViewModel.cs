using System.ComponentModel.DataAnnotations;
using Kvitoria.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kvitoria.ViewModels;

public class CareLogFormViewModel
{
    [Required]
    public int PlantId { get; set; }

    public string PlantName { get; set; } = string.Empty;

    [Display(Name = "Тип догляду")]
    public CareActivityType ActivityType { get; set; } = CareActivityType.Watering;

    [Required(ErrorMessage = "Вкажіть дату догляду.")]
    [DataType(DataType.Date)]
    [Display(Name = "Дата")]
    public DateOnly PerformedOn { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    [StringLength(700, ErrorMessage = "Нотатка має бути до 700 символів.")]
    [Display(Name = "Нотатка")]
    public string? Notes { get; set; }

    public IReadOnlyList<SelectListItem> ActivityTypeOptions { get; init; } = [];
}
