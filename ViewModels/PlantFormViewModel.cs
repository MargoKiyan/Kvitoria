using System.ComponentModel.DataAnnotations;
using Kvitoria.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kvitoria.ViewModels;

public class PlantFormViewModel
{
    public int? Id { get; init; }

    [Required(ErrorMessage = "Вкажіть назву рослини.")]
    [StringLength(80, ErrorMessage = "Назва має бути до 80 символів.")]
    [Display(Name = "Назва в колекції")]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Оберіть вид рослини з каталогу.")]
    [Display(Name = "Вид рослини")]
    public int PlantSpeciesId { get; set; }

    [Display(Name = "Форма або сорт")]
    public int? PlantVarietyId { get; set; }

    public string Species { get; init; } = string.Empty;

    public string? Variety { get; init; }

    [Display(Name = "Статус")]
    public PlantStatus Status { get; set; } = PlantStatus.Growing;

    [Display(Name = "Освітлення")]
    public LightRequirement LightRequirement { get; set; } = LightRequirement.BrightIndirect;

    [Display(Name = "Частота поливу")]
    public WateringFrequency WateringFrequency { get; set; } = WateringFrequency.Weekly;

    [StringLength(80, ErrorMessage = "Локація має бути до 80 символів.")]
    [Display(Name = "Локація")]
    public string? Location { get; set; }

    [Range(0.1, 99.9, ErrorMessage = "Діаметр має бути від 0.1 до 99.9 см.")]
    [Display(Name = "Діаметр горщика, см")]
    public decimal? PotDiameterCm { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Дата отримання")]
    public DateOnly? AcquisitionDate { get; set; }

    [StringLength(120, ErrorMessage = "Джерело має бути до 120 символів.")]
    [Display(Name = "Джерело")]
    public string? AcquisitionSource { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Останній полив")]
    public DateOnly? LastWateredDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Наступний полив")]
    public DateOnly? NextWateringDate { get; init; }

    [StringLength(500, ErrorMessage = "Шлях фото має бути до 500 символів.")]
    [Display(Name = "Поточне фото")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Фото рослини")]
    public IFormFile? Photo { get; set; }

    [StringLength(1000, ErrorMessage = "Нотатки мають бути до 1000 символів.")]
    [Display(Name = "Нотатки")]
    public string? Notes { get; set; }

    public IReadOnlyList<SelectListItem> StatusOptions { get; init; } = [];

    public IReadOnlyList<SelectListItem> LightRequirementOptions { get; init; } = [];

    public IReadOnlyList<SelectListItem> WateringFrequencyOptions { get; init; } = [];

    public IReadOnlyList<PlantSpeciesOptionViewModel> SpeciesOptions { get; init; } = [];

    public IReadOnlyList<PlantVarietyOptionViewModel> VarietyOptions { get; init; } = [];
}

public class PlantSpeciesOptionViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}

public class PlantVarietyOptionViewModel
{
    public int Id { get; init; }

    public int PlantSpeciesId { get; init; }

    public string Name { get; init; } = string.Empty;

    public PlantVariantType Type { get; init; }

    public string TypeName { get; init; } = string.Empty;
}
