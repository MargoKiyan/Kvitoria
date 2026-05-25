namespace Kvitoria.ViewModels;

public class DatabaseIssueViewModel
{
    public string Title { get; init; } = "PostgreSQL ще не підключено";

    public string Message { get; init; } = "Запустіть PostgreSQL і перевірте рядок підключення.";

    public static DatabaseIssueViewModel From(Exception exception)
    {
        return new DatabaseIssueViewModel
        {
            Message = exception.GetBaseException().Message
        };
    }
}
