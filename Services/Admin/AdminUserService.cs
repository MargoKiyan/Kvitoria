using Kvitoria.Data;
using Kvitoria.Models.Auth;
using Kvitoria.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kvitoria.Services.Admin;

public class AdminUserService(
    KvitoriaDbContext dbContext,
    UserManager<ApplicationUser> userManager) : IAdminUserService
{
    public async Task<AdminUsersViewModel> GetUsersAsync(
        string? search,
        string status,
        string role,
        string sort,
        int page,
        CancellationToken cancellationToken = default)
    {
        var totalUsers = await dbContext.Users.CountAsync(cancellationToken);
        var deletedUsers = await dbContext.Users.CountAsync(user => user.IsDeleted, cancellationToken);
        var query = ApplySearch(dbContext.Users.AsNoTracking(), search);
        query = ApplyStatusFilter(query, status);
        query = ApplySorting(query, sort);

        var users = await query.ToListAsync(cancellationToken);
        var rows = await BuildRowsAsync(users, role);
        var pagination = PaginationViewModel.Create(
            page,
            PaginationViewModel.DefaultPageSize,
            rows.Count,
            "Users",
            new Dictionary<string, string?>
            {
                ["search"] = search,
                ["status"] = status,
                ["role"] = role,
                ["sort"] = sort
            });

        return new AdminUsersViewModel
        {
            Search = search,
            Status = status,
            Role = role,
            Sort = sort,
            TotalUsers = totalUsers,
            ActiveUsers = totalUsers - deletedUsers,
            DeletedUsers = deletedUsers,
            Users = rows
                .Skip(pagination.Skip)
                .Take(pagination.PageSize)
                .ToList(),
            Pagination = pagination
        };
    }

    public async Task<AdminActionResult> DeleteAllRegularUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users.ToListAsync(cancellationToken);
        var deletedCount = 0;
        var failedCount = 0;

        foreach (var user in users)
        {
            if (await userManager.IsInRoleAsync(user, ApplicationRoleNames.Admin))
            {
                continue;
            }

            var result = await userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                deletedCount++;
                continue;
            }

            failedCount++;
        }

        if (failedCount > 0)
        {
            return AdminActionResult.Failure(
                $"Видалено акаунтів користувачів: {deletedCount}. Не вдалося видалити: {failedCount}. Адміністраторів не змінено.");
        }

        return deletedCount == 0
            ? AdminActionResult.Success("Немає користувачів для видалення. Адміністраторів не змінено.")
            : AdminActionResult.Success($"Видалено акаунтів користувачів з бази даних: {deletedCount}. Адміністраторів не змінено.");
    }

    private static IQueryable<ApplicationUser> ApplySearch(IQueryable<ApplicationUser> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var pattern = $"%{search.Trim()}%";
        return query.Where(user =>
            user.Email != null && EF.Functions.ILike(user.Email, pattern)
            || user.UserName != null && EF.Functions.ILike(user.UserName, pattern)
            || EF.Functions.ILike(user.FullName, pattern));
    }

    private static IQueryable<ApplicationUser> ApplyStatusFilter(IQueryable<ApplicationUser> query, string status)
    {
        return status switch
        {
            "active" => query.Where(user => !user.IsDeleted),
            "deleted" => query.Where(user => user.IsDeleted),
            _ => query
        };
    }

    private static IQueryable<ApplicationUser> ApplySorting(IQueryable<ApplicationUser> query, string sort)
    {
        return sort switch
        {
            "login_desc" => query.OrderByDescending(user => user.UserName),
            "email_asc" => query.OrderBy(user => user.Email),
            "email_desc" => query.OrderByDescending(user => user.Email),
            "name_asc" => query.OrderBy(user => user.FullName),
            "name_desc" => query.OrderByDescending(user => user.FullName),
            "registered_asc" => query.OrderBy(user => user.RegisteredAtUtc),
            "registered_desc" => query.OrderByDescending(user => user.RegisteredAtUtc),
            _ => query.OrderBy(user => user.UserName)
        };
    }

    private async Task<List<AdminUserRowViewModel>> BuildRowsAsync(
        IReadOnlyList<ApplicationUser> users,
        string role)
    {
        var rows = new List<AdminUserRowViewModel>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);

            if (role != "all" && !roles.Contains(role))
            {
                continue;
            }

            rows.Add(new AdminUserRowViewModel
            {
                User = user,
                Roles = roles.ToList()
            });
        }

        return rows;
    }
}
