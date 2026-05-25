using System.ComponentModel.DataAnnotations;

namespace Kvitoria.Models.Enums;

public enum PlantVariantType
{
    [Display(Name = "Форма")]
    Form = 1,

    [Display(Name = "Сорт")]
    Variety = 2
}
