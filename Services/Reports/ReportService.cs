using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Kvitoria.Models.Reporting;
using Kvitoria.Services.Admin;

namespace Kvitoria.Services.Reports;

public class ReportService(
    IAdminAnalyticsService analyticsService,
    IWebHostEnvironment environment) : IReportService
{
    public async Task<FileInfo> GenerateTextReportAsync(CancellationToken cancellationToken = default)
    {
        var report = await analyticsService.GetDashboardAsync(cancellationToken);
        var file = CreateReportFile("txt");

        await using (var stream = new FileStream(file.FullName, FileMode.Create, FileAccess.Write, FileShare.None))
        await using (var writer = new StreamWriter(stream))
        {
            await writer.WriteLineAsync("Kvitoria admin report");
            await writer.WriteLineAsync($"Generated at UTC: {DateTime.UtcNow:O}");
            await writer.WriteLineAsync();

            foreach (var metric in report.Metrics)
            {
                await writer.WriteLineAsync(metric.ToDisplayString());
            }
        }

        return file;
    }

    public async Task<FileInfo> GenerateWordReportAsync(CancellationToken cancellationToken = default)
    {
        var report = await analyticsService.GetDashboardAsync(cancellationToken);
        var file = CreateReportFile("docx");

        using var document = WordprocessingDocument.Create(file.FullName, WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        var body = mainPart.Document.Body ?? new Body();

        body.Append(CreateParagraph("Kvitoria admin report", true));
        body.Append(CreateParagraph($"Generated at UTC: {DateTime.UtcNow:O}", false));

        foreach (var metric in report.Metrics)
        {
            body.Append(CreateParagraph(metric.ToDisplayString(), false));
        }

        mainPart.Document.Save();

        await Task.CompletedTask;
        return file;
    }

    private FileInfo CreateReportFile(string extension)
    {
        var directory = new DirectoryInfo(Path.Combine(environment.ContentRootPath, "App_Data", "Reports"));

        if (!directory.Exists)
        {
            directory.Create();
        }

        return new FileInfo(Path.Combine(
            directory.FullName,
            $"kvitoria-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{extension}"));
    }

    private static Paragraph CreateParagraph(string text, bool bold)
    {
        var run = new Run(new Text(text));

        if (bold)
        {
            run.RunProperties = new RunProperties(new Bold());
        }

        return new Paragraph(run);
    }
}
