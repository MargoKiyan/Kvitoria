using System.ComponentModel.DataAnnotations;
using Kvitoria.Models.Feedback;

namespace Kvitoria.ViewModels;

public class FeedbackFormViewModel
{
    [Required(ErrorMessage = "Вкажіть тему.")]
    [StringLength(140, ErrorMessage = "Тема має бути до 140 символів.")]
    [Display(Name = "Тема")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Напишіть повідомлення.")]
    [StringLength(2500, ErrorMessage = "Повідомлення має бути до 2500 символів.")]
    [Display(Name = "Повідомлення")]
    public string Body { get; set; } = string.Empty;
}

public class FeedbackPageViewModel
{
    public FeedbackFormViewModel Form { get; init; } = new();

    public IReadOnlyList<FeedbackMessage> Messages { get; init; } = [];

    public PaginationViewModel Pagination { get; init; } = new();
}

public class FeedbackListViewModel
{
    public IReadOnlyList<FeedbackMessage> Messages { get; init; } = [];

    public int UnreadCount { get; init; }
}

public class AdminFeedbackReplyViewModel
{
    [Required(ErrorMessage = "Напишіть відповідь.")]
    [StringLength(2500, ErrorMessage = "Відповідь має бути до 2500 символів.")]
    [Display(Name = "Відповідь")]
    public string Reply { get; set; } = string.Empty;
}
