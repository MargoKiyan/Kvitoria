using System.ComponentModel.DataAnnotations;

namespace Kvitoria.Models.Enums;

public enum LightRequirement
{
    [Display(Name = "Яскраве розсіяне")]
    BrightIndirect = 1,

    [Display(Name = "Півтінь")]
    PartialShade = 2,

    [Display(Name = "Пряме сонце")]
    DirectSun = 3,

    [Display(Name = "Слабке освітлення")]
    LowLight = 4,

    [Display(Name = "Фітолампа")]
    GrowLight = 5
}
