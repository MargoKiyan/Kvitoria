using Kvitoria.Models.Auth;

namespace Kvitoria.Models.Feedback;

public class FeedbackMessage : BaseEntity
{
    private FeedbackMessage()
    {
    }

    public FeedbackMessage(string userId, string subject, string body)
    {
        UserId = userId;
        UpdateContent(subject, body);
    }

    public string UserId { get; private set; } = string.Empty;

    public ApplicationUser? User { get; private set; }

    public string Subject { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public bool IsRead { get; private set; }

    public DateTime? ReadAtUtc { get; private set; }

    public string? AdminReply { get; private set; }

    public DateTime? RepliedAtUtc { get; private set; }

    public string? RepliedByAdminId { get; private set; }

    public ApplicationUser? RepliedByAdmin { get; private set; }

    public void MarkRead()
    {
        IsRead = true;
        ReadAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Reply(string adminUserId, string reply)
    {
        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            throw new ArgumentException("Адміністратор обов'язковий.", nameof(adminUserId));
        }

        if (string.IsNullOrWhiteSpace(reply))
        {
            throw new ArgumentException("Відповідь не може бути порожньою.", nameof(reply));
        }

        AdminReply = reply.Trim();
        RepliedByAdminId = adminUserId;
        RepliedAtUtc = DateTime.UtcNow;
        MarkRead();
    }

    private void UpdateContent(string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Тема звернення обов'язкова.", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Текст звернення обов'язковий.", nameof(body));
        }

        Subject = subject.Trim();
        Body = body.Trim();
        MarkUpdated();
    }
}
