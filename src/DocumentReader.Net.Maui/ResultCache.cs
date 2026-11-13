namespace DocumentReader.Net.Maui;

/// <summary>
/// Parsed result data computed once off the UI thread (Flutter/RN build this when navigating to Result).
/// </summary>
static class ResultCache
{
    public static string RawJson { get; private set; } = "";
    public static string Summary { get; private set; } = "";
    public static IReadOnlyList<FieldRow> FieldRows { get; private set; } = Array.Empty<FieldRow>();
    public static string SecuritySummary { get; private set; } = "";
    public static IReadOnlyList<SecurityRow> SecurityRows { get; private set; } = Array.Empty<SecurityRow>();
    public static IReadOnlyList<ResultImage> Images { get; private set; } = Array.Empty<ResultImage>();
    public static string Pretty { get; private set; } = "";

    public static Task PrepareAsync(string rawJson) =>
        Task.Run(() =>
        {
            RawJson = rawJson;
            var normalized = ResultNormalizer.NormalizeJson(rawJson);
            Summary = ResultView.Summary(normalized);
            FieldRows = ResultView.Rows(normalized);
            SecuritySummary = ResultView.SecuritySummary(normalized);
            SecurityRows = ResultView.SecurityRows(normalized);
            Images = ResultView.Images(normalized);
            Pretty = ResultView.Pretty(normalized);
        });
}
