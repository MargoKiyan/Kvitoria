namespace Kvitoria.Services.Auth;

public interface IUserContext
{
    string? UserId { get; }

    bool IsAuthenticated { get; }

    bool IsAdmin { get; }

    bool IsUser { get; }
}
