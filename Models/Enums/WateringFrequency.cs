using System.ComponentModel.DataAnnotations;

namespace Kvitoria.Models.Enums;

public enum WateringFrequency
{
    [Display(Name = "Кожні 3 дні")]
    EveryThreeDays = 3,

    [Display(Name = "Щотижня")]
    Weekly = 7,

    [Display(Name = "Кожні 10 днів")]
    EveryTenDays = 10,

    [Display(Name = "Раз на 2 тижні")]
    EveryTwoWeeks = 14,

    [Display(Name = "Раз на місяць")]
    Monthly = 30
}

public static class WateringFrequencyExtensions
{
    public static int ToDayInterval(this WateringFrequency frequency) => (int)frequency;
}
