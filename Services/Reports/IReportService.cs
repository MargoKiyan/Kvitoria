namespace Kvitoria.Services.Reports;

public interface IReportService
{
    Task<FileInfo> GenerateTextReportAsync(CancellationToken cancellationToken = default);

    Task<FileInfo> GenerateWordReportAsync(CancellationToken cancellationToken = default);
}
