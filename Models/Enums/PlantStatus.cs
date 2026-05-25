using System.ComponentModel.DataAnnotations;

namespace Kvitoria.Models.Enums;

public enum PlantStatus
{
    [Display(Name = "Росте")]
    Growing = 1,

    [Display(Name = "На адаптації")]
    Adapting = 2,

    [Display(Name = "Потребує уваги")]
    NeedsCare = 3,

    [Display(Name = "У карантині")]
    Quarantine = 4,

    [Display(Name = "Вибула з колекції")]
    Archived = 5
}
