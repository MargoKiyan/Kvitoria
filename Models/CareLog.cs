using Kvitoria.Models.Enums;

namespace Kvitoria.Models;

public class CareLog : BaseEntity
{
    private CareLog()
    {
    }

    public CareLog(int plantId, CareActivityType activityType, DateOnly performedOn, string? notes)
    {
        PlantId = plantId;
        UpdateDetails(activityType, performedOn, notes);
    }

    public int PlantId { get; private set; }

    public Plant? Plant { get; private set; }

    public CareActivityType ActivityType { get; private set; } = CareActivityType.Inspection;

    public DateOnly PerformedOn { get; private set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public string? Notes { get; private set; }

    public void UpdateDetails(CareActivityType activityType, DateOnly performedOn, string? notes)
    {
        ActivityType = activityType;
        PerformedOn = performedOn;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        MarkUpdated();
    }
}
