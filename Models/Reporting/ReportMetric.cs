namespace Kvitoria.Models.Reporting;

public readonly struct ReportMetric
{
    public ReportMetric(string name, decimal value, string unit = "")
    {
        Name = name;
        Value = value;
        Unit = unit;
    }

    public string Name { get; }

    public decimal Value { get; }

    public string Unit { get; }

    public string ToDisplayString()
    {
        return string.IsNullOrWhiteSpace(Unit)
            ? $"{Name}: {Value:0.##}"
            : $"{Name}: {Value:0.##} {Unit}";
    }

    public static bool TryCreate(string name, decimal value, out ReportMetric metric)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            metric = default;
            return false;
        }

        metric = new ReportMetric(name.Trim(), value);
        return true;
    }
}
