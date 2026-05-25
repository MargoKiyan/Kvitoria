using System.Globalization;

namespace Kvitoria.ViewModels;

public class PaginationViewModel
{
    public const int DefaultPageSize = 6;

    public int CurrentPage { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;

    public int TotalItems { get; init; }

    public string Action { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string?> RouteValues { get; init; } = new Dictionary<string, string?>();

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));

    public int Skip => (CurrentPage - 1) * PageSize;

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage => CurrentPage < TotalPages;

    public int FirstItemNumber => TotalItems == 0 ? 0 : Skip + 1;

    public int LastItemNumber => Math.Min(Skip + PageSize, TotalItems);

    public static PaginationViewModel Create(
        int page,
        int pageSize,
        int totalItems,
        string action,
        IReadOnlyDictionary<string, string?>? routeValues = null)
    {
        pageSize = Math.Max(1, pageSize);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        var currentPage = Math.Clamp(page, 1, totalPages);

        return new PaginationViewModel
        {
            CurrentPage = currentPage,
            PageSize = pageSize,
            TotalItems = totalItems,
            Action = action,
            RouteValues = routeValues ?? new Dictionary<string, string?>()
        };
    }

    public IDictionary<string, string> GetRouteValues(int page)
    {
        var values = RouteValues
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .ToDictionary(item => item.Key, item => item.Value!);
        values["page"] = page.ToString(CultureInfo.InvariantCulture);

        return values;
    }
}
