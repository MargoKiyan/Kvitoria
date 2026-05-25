using Kvitoria.Models;
using Kvitoria.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kvitoria.ViewModels;

public class PlantListViewModel
{
    public string? Search { get; init; }

    public PlantStatus? Status { get; init; }

    public int TotalCount { get; init; }

    public int FilteredCount { get; init; }

    public int NeedsAttentionCount { get; init; }

    public IReadOnlyList<Plant> Plants { get; init; } = [];

    public IReadOnlyList<SelectListItem> StatusOptions { get; init; } = [];

    public PaginationViewModel Pagination { get; init; } = new();
}
