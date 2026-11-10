using OwlSharp.Extensions.GameBanana.Models;

namespace OwlSharp.Extensions.GameBanana.Strategies;

public interface IDownloadStrategy
{
    ValueTask<byte[]> GetBytesAndReportProgress(Uri url, DownloadReporter? reporter, CancellationToken ct = default);
}