using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kvitoria.Extensions;

public static class EnumSelectListExtensions
{
    public static IReadOnlyList<SelectListItem> ToSelectList<TEnum>() where TEnum : struct, Enum
    {
        return Enum.GetValues<TEnum>()
            .Select(value => new SelectListItem
            {
                Value = value.ToString(),
                Text = value.GetDisplayName()
            })
            .ToList();
    }
}
