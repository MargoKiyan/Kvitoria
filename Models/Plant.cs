using Kvitoria.Models.Auth;
using Kvitoria.Models.Enums;
using Kvitoria.Models.PlantCatalog;

namespace Kvitoria.Models;

public class Plant : BaseEntity
{
    private Plant()
    {
    }

    public Plant(
        string name,
        string species,
        PlantStatus status,
        LightRequirement lightRequirement,
        WateringFrequency wateringFrequency)
    {
        UpdateProfile(
            name,
            species,
            variety: null,
            status,
            lightRequirement,
            wateringFrequency,
            location: null,
            potDiameterCm: null,
            acquisitionDate: null,
            acquisitionSource: null,
            imageUrl: null,
            notes: null);
    }

    public string Name { get; private set; } = string.Empty;

    public string? UserId { get; private set; }

    public ApplicationUser? Owner { get; private set; }

    public int? PlantSpeciesId { get; private set; }

    public PlantSpecies? CatalogSpecies { get; private set; }

    public int? PlantVarietyId { get; private set; }

    public PlantVariety? CatalogVariety { get; private set; }

    public string Species { get; private set; } = string.Empty;

    public string? Variety { get; private set; }

    public PlantStatus Status { get; private set; } = PlantStatus.Growing;

    public LightRequirement LightRequirement { get; private set; } = LightRequirement.BrightIndirect;

    public WateringFrequency WateringFrequency { get; private set; } = WateringFrequency.Weekly;

    public string? Location { get; private set; }

    public decimal? PotDiameterCm { get; private set; }

    public DateOnly? AcquisitionDate { get; private set; }

    public string? AcquisitionSource { get; private set; }

    public DateOnly? LastWateredDate { get; private set; }

    public DateOnly? NextWateringDate { get; private set; }

    public string? ImageUrl { get; private set; }

    public string? Notes { get; private set; }

    public ICollection<CareLog> CareLogs { get; private set; } = new List<CareLog>();

    public void AssignOwner(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("Власник рослини обов'язковий.", nameof(userId));
        }

        UserId = userId;
        MarkUpdated();
    }

    public void AssignCatalog(PlantSpecies species, PlantVariety? variety)
    {
        ArgumentNullException.ThrowIfNull(species);

        if (species.Id <= 0)
        {
            throw new InvalidOperationException("Обраний вид рослини має бути збережений у каталозі.");
        }

        if (variety is not null && variety.PlantSpeciesId != species.Id)
        {
            throw new InvalidOperationException("Форма або сорт не належить до обраного виду рослини.");
        }

        PlantSpeciesId = species.Id;
        PlantVarietyId = variety?.Id;
        Species = species.Name;
        Variety = variety?.Name;
        MarkUpdated();
    }

    public void UpdateProfile(
        string name,
        string species,
        string? variety,
        PlantStatus status,
        LightRequirement lightRequirement,
        WateringFrequency wateringFrequency,
        string? location,
        decimal? potDiameterCm,
        DateOnly? acquisitionDate,
        string? acquisitionSource,
        string? imageUrl,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Назва рослини обов'язкова.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(species))
        {
            throw new ArgumentException("Вид рослини обов'язковий.", nameof(species));
        }

        if (potDiameterCm is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(potDiameterCm), "Діаметр горщика не може бути від'ємним.");
        }

        Name = name.Trim();
        Species = species.Trim();
        Variety = Normalize(variety);
        Status = status;
        LightRequirement = lightRequirement;
        WateringFrequency = wateringFrequency;
        Location = Normalize(location);
        PotDiameterCm = potDiameterCm;
        AcquisitionDate = acquisitionDate;
        AcquisitionSource = Normalize(acquisitionSource);
        ImageUrl = Normalize(imageUrl);
        Notes = Normalize(notes);

        MarkUpdated();
    }

    public void UpdateCareSchedule(DateOnly? lastWateredDate)
    {
        LastWateredDate = lastWateredDate;
        NextWateringDate = lastWateredDate?.AddDays(WateringFrequency.ToDayInterval());
        MarkUpdated();
    }

    public CareLog RegisterCare(CareActivityType activityType, DateOnly performedOn, string? notes)
    {
        if (activityType == CareActivityType.Watering && HasWateringOn(performedOn))
        {
            throw new InvalidOperationException("Полив для цієї рослини вже занотовано на обрану дату.");
        }

        var careLog = new CareLog(Id, activityType, performedOn, notes);
        CareLogs.Add(careLog);

        if (activityType == CareActivityType.Watering)
        {
            MarkWatered(performedOn);
        }
        else
        {
            MarkUpdated();
        }

        return careLog;
    }

    public void MarkWatered(DateOnly performedOn)
    {
        LastWateredDate = performedOn;
        NextWateringDate = performedOn.AddDays(WateringFrequency.ToDayInterval());
        MarkUpdated();
    }

    public bool NeedsAttention(DateOnly today)
    {
        return Status != PlantStatus.Archived
            && (Status == PlantStatus.NeedsCare
                || NextWateringDate.HasValue && NextWateringDate.Value <= today);
    }

    public bool HasWateringOn(DateOnly date)
    {
        return CareLogs.Any(log =>
            log.ActivityType == CareActivityType.Watering
            && log.PerformedOn == date);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
