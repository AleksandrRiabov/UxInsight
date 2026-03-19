using UxInsight.Models;

namespace UxInsight.Services;

public interface IAnalysisService
{
    Task<AnalysisResult> AnalyzeAsync(DateTimeOffset? from = null, DateTimeOffset? to = null);
    Task<AnalysisResult?> GetLatestAsync();
    Task<List<AnalysisResult>> GetHistoryAsync(int take = 10);
}
