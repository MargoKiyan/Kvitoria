using System.ComponentModel.DataAnnotations;

namespace Kvitoria.Models.Enums;

public enum CareActivityType
{
    [Display(Name = "Полив")]
    Watering = 1,

    [Display(Name = "Підживлення")]
    Fertilizing = 2,

    [Display(Name = "Пересадка")]
    Repotting = 3,

    [Display(Name = "Огляд")]
    Inspection = 4,

    [Display(Name = "Обрізка")]
    Pruning = 5,

    [Display(Name = "Обробка")]
    Treatment = 6
}
