using Kvitoria.Models.Enums;

namespace Kvitoria.Models.PlantCatalog;

public class PlantVariety : BaseEntity
{
    private PlantVariety() {}

    public PlantVariety(int plantSpeciesId, string name, PlantVariantType type)
    {
        PlantSpeciesId = plantSpeciesId;
        Update(name, type);
    }

    public int PlantSpeciesId { get; private set; }

    public PlantSpecies? Species { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public PlantVariantType Type { get; private set; } = PlantVariantType.Variety;

    public ICollection<Plant> Plants { get; private set; } = new List<Plant>();

    public void Update(string name, PlantVariantType type)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Назва форми або сорту обов'язкова.", nameof(name));
        }

        Name = name.Trim();
        Type = type;
        MarkUpdated();
    }
}
