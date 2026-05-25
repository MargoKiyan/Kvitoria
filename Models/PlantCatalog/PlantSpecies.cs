namespace Kvitoria.Models.PlantCatalog;

public class PlantSpecies : BaseEntity
{
    private PlantSpecies() {}

    public PlantSpecies(string name)
    {
        Update(name);
    }

    public string Name { get; private set; } = string.Empty;

    public ICollection<PlantVariety> Varieties { get; private set; } = new List<PlantVariety>();

    public ICollection<Plant> Plants { get; private set; } = new List<Plant>();

    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Назва виду рослини обов'язкова.", nameof(name));
        }

        Name = name.Trim();
        MarkUpdated();
    }
}
