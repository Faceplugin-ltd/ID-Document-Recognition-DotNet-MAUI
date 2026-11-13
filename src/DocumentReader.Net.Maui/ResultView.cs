using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DocumentReader.Net.Maui;

public sealed record FieldRow(string Key, string Value, string Source);

public sealed record SecurityRow(string Page, string Check, string Status);

public sealed record ResultImage(string Category, string Source, byte[] Bytes);

/// <summary>
/// Same field, liveness, and image mapping as the Flutter and React Native result screens.
/// </summary>
public static class ResultView
{
    const int LongValue = 300;

    static readonly HashSet<string> SkipKeys = new(StringComparer.Ordinal)
    {
        "checkSums", "contrastPrint", "docFormat", "mrzFormat", "mrzFormatCheckdigit",
        "mrzStringsWithCorrectCheckSums", "numberChecksumValidity", "numberValidity",
        "overallValidity", "symbolMatrix", "images",
    };

    static readonly string[] ImageQaOrder =
    [
        "focus", "glares", "resolution", "colorness", "perspective", "bounds",
        "portrait", "handwritten", "brightness", "occlusion",
    ];

    static readonly string[] VerifyOrder =
    [
        "docType", "expiry", "text", "mrz", "security", "imageQA", "portrait",
    ];

    static readonly HashSet<string> SecurityPageMeta = new(StringComparer.Ordinal)
    {
        "pageIndex", "overall", "label", "pages", "presentation",
    };

    public static string Pretty(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.Length == 0)
        {
            return "(empty response)";
        }

        try
        {
            JsonNode.Parse(trimmed);
        }
        catch (JsonException)
        {
            return "{}";
        }

        var node = JsonNode.Parse(ResultNormalizer.NormalizeJson(trimmed));
        return JsonSerializer.Serialize(Sanitize(node), new JsonSerializerOptions { WriteIndented = true });
    }

    public static string Summary(string raw)
    {
        var obj = ResultNormalizer.AsView(raw);
        if (obj == null)
        {
            return raw.Length > 200 ? raw[..200] : raw;
        }

        if (ResultNormalizer.NonEmpty(obj["msg"]) is string message)
        {
            return message;
        }

        var err = ErrorCode(obj);
        var scoreNode = obj["score"];
        var score = ResultNormalizer.AsDouble(scoreNode) is double number
            ? number.ToString("0.000", CultureInfo.InvariantCulture)
            : scoreNode != null ? ResultNormalizer.Text(scoreNode) : "—";
        var verification = VerificationMap(obj);
        var verificationText = verification?["overall"] != null
            ? LabelValue(verification["overall"], true, ReasonsForCheck("overall", verification, obj))
            : "Not checked";
        return string.Join('\n',
        [
            $"Status: {(err == 0 ? "OK" : "Failed")} (errorCode={err})",
            $"Document: {ResultNormalizer.NonEmpty(obj["documentName"]) ?? "—"}",
            $"Country: {ResultNormalizer.NonEmpty(obj["countryName"]) ?? "—"}",
            $"Verification: {verificationText}",
            $"Score: {score}",
        ]);
    }

    public static IReadOnlyList<FieldRow> Rows(string raw)
    {
        var obj = ResultNormalizer.AsView(raw);
        if (obj == null)
        {
            return [];
        }

        var rows = new List<FieldRow>
        {
            new("documentName", ResultNormalizer.NonEmpty(obj["documentName"]) ?? "", "meta"),
            new("countryName", ResultNormalizer.NonEmpty(obj["countryName"]) ?? "", "meta"),
            new("score", obj["score"] != null ? ResultNormalizer.Text(obj["score"]) : "—", "meta"),
            new("errorCode", obj["errorCode"] != null ? ResultNormalizer.Text(obj["errorCode"]) : "", "meta"),
        };
        rows.AddRange(RowsFromVerification(obj));
        rows.AddRange(RowsFromImageQuality(obj["imageQuality"]));
        rows.AddRange(RowsFromMap(AsObject(obj["ocr"]), "OCR"));
        rows.AddRange(RowsFromMap(AsObject(obj["mrz"]), "MRZ"));
        rows.AddRange(RowsFromMap(AsObject(obj["barcode"]), "Barcode"));
        return rows.Where(row => row.Value.Length > 0 && row.Value != "null").ToList();
    }

    public static string SecuritySummary(string raw)
    {
        var sec = AsObject(ResultNormalizer.AsView(raw)?["security"]);
        if (sec == null)
        {
            return "No liveness checks in this response. If you expected checks, this license may not include liveness.";
        }

        var pages = SecurityPages(sec);
        var docLabel = ResultNormalizer.NonEmpty(sec["label"]) ?? StatusCell(sec["overall"]);
        if (pages.Count == 0)
        {
            return docLabel.Length > 0
                ? $"Document: {docLabel}\nNo per-page checks in this response."
                : "No liveness checks in this response.";
        }

        var buffer = new StringBuilder("Document: ");
        buffer.Append(docLabel.Length == 0 ? "—" : docLabel);
        foreach (var page in pages)
        {
            var name = PageSide(page["pageIndex"]);
            var pageLabel = ResultNormalizer.NonEmpty(page["label"]) ?? StatusCell(page["overall"]);
            buffer.Append('\n').Append(name).Append(": ").Append(pageLabel.Length == 0 ? "—" : pageLabel);
        }

        return buffer.ToString();
    }

    public static IReadOnlyList<SecurityRow> SecurityRows(string raw)
    {
        var sec = AsObject(ResultNormalizer.AsView(raw)?["security"]);
        if (sec == null)
        {
            return [];
        }

        var pages = SecurityPages(sec);
        if (pages.Count == 0)
        {
            var docLabel = ResultNormalizer.NonEmpty(sec["label"]) ?? StatusCell(sec["overall"]);
            return docLabel.Length == 0 ? [] : [new SecurityRow("—", "Overall", docLabel)];
        }

        var rows = new List<SecurityRow>();
        foreach (var page in pages)
        {
            var name = PageSide(page["pageIndex"]);
            var pageLabel = ResultNormalizer.NonEmpty(page["label"]) ?? StatusCell(page["overall"]);
            rows.Add(new SecurityRow(name, "Overall", pageLabel.Length == 0 ? "—" : pageLabel));
            foreach (var entry in page)
            {
                if (SecurityPageMeta.Contains(entry.Key))
                {
                    continue;
                }

                AppendSecurityValue(rows, name, entry.Key, entry.Value);
            }
        }

        return rows;
    }

    public static IReadOnlyList<ResultImage> Images(string raw)
    {
        var obj = ResultNormalizer.AsView(raw);
        if (obj?["images"] is not JsonArray list)
        {
            return [];
        }

        var output = new List<ResultImage>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in list)
        {
            var b64 = "";
            var category = "Image";
            var source = "";
            if (item is JsonValue)
            {
                b64 = ResultNormalizer.NonEmpty(item) ?? "";
            }
            else if (AsObject(item) is JsonObject data)
            {
                b64 = ResultNormalizer.NonEmpty(data["image"])
                    ?? ResultNormalizer.NonEmpty(data["value"])
                    ?? ResultNormalizer.NonEmpty(data["data"])
                    ?? "";
                var name = ResultNormalizer.NonEmpty(data["name"])
                    ?? ResultNormalizer.NonEmpty(data["fieldName"])
                    ?? ResultNormalizer.NonEmpty(data["role"])
                    ?? "";
                category = name.Length == 0 ? "Image" : name;
                source = ResultNormalizer.NonEmpty(data["source"]) ?? "";
            }

            if (b64.Length < 32)
            {
                continue;
            }

            var key = category.ToLowerInvariant() + "|" + source.ToLowerInvariant();
            if (!seen.Add(key))
            {
                continue;
            }

            var payload = b64.Contains("base64,", StringComparison.Ordinal)
                ? b64[(b64.IndexOf("base64,", StringComparison.Ordinal) + 7)..]
                : b64;
            payload = Regex.Replace(payload, @"\s", "");
            try
            {
                var bytes = Convert.FromBase64String(payload);
                if (bytes.Length > 0)
                {
                    output.Add(new ResultImage(category, source, bytes));
                }
            }
            catch (FormatException)
            {
            }
        }

        return output;
    }

    static JsonNode? Sanitize(JsonNode? value)
    {
        if (value is JsonArray list)
        {
            var copy = new JsonArray();
            foreach (var item in list)
            {
                copy.Add(Sanitize(item));
            }

            return copy;
        }

        if (value is JsonObject obj)
        {
            var copy = new JsonObject();
            foreach (var entry in obj)
            {
                copy[entry.Key] = Sanitize(entry.Value);
            }

            return copy;
        }

        if (value is JsonValue json && json.TryGetValue<string>(out var text) && text.Length > LongValue)
        {
            return SummarizeLong(text);
        }

        return value == null ? null : JsonNode.Parse(value.ToJsonString());
    }

    static string SummarizeLong(string value)
    {
        var type = "string";
        if (value.StartsWith("/9j/", StringComparison.Ordinal) || value.StartsWith("data:image/jpeg", StringComparison.Ordinal))
        {
            type = "jpeg";
        }
        else if (value.StartsWith("iVBOR", StringComparison.Ordinal) || value.StartsWith("data:image/png", StringComparison.Ordinal))
        {
            type = "png";
        }
        else if (value.StartsWith("R0lGOD", StringComparison.Ordinal) || value.StartsWith("data:image/gif", StringComparison.Ordinal))
        {
            type = "gif";
        }
        else if (value.StartsWith("Qk", StringComparison.Ordinal) && value.Length > 100)
        {
            type = "bmp";
        }
        else if (Regex.IsMatch(value, "^[A-Za-z0-9+/=]+$"))
        {
            type = "base64";
        }

        return $"{type}, {value.Length} chars";
    }

    static int ErrorCode(JsonObject obj)
    {
        if (ResultNormalizer.AsDouble(obj["errorCode"]) is double number)
        {
            return (int)number;
        }

        return int.TryParse(ResultNormalizer.Text(obj["errorCode"]), out var parsed) ? parsed : -1;
    }

    static List<FieldRow> RowsFromMap(JsonObject? data, string source)
    {
        if (data == null)
        {
            return [];
        }

        var rows = new List<FieldRow>();
        foreach (var key in data.Select(entry => entry.Key).OrderBy(key => key, StringComparer.Ordinal))
        {
            if (SkipKeys.Contains(key) || Regex.IsMatch(key, "^field\\d+$"))
            {
                continue;
            }

            var value = data[key];
            if (value is JsonObject or JsonArray)
            {
                continue;
            }

            rows.Add(new FieldRow(key, ResultNormalizer.Text(value), source));
        }

        return rows;
    }

    static List<FieldRow> RowsFromImageQuality(JsonNode? value)
    {
        var checks = ResultNormalizer.ExtractImageQualityChecks(value);
        if (checks.Count == 0)
        {
            return [];
        }

        var rows = new List<FieldRow>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in ImageQaOrder)
        {
            if (!checks.ContainsKey(key))
            {
                continue;
            }

            seen.Add(key);
            rows.Add(QaRow(key, checks[key]));
        }

        foreach (var key in checks.Select(entry => entry.Key).OrderBy(key => key, StringComparer.Ordinal))
        {
            if (seen.Contains(key))
            {
                continue;
            }

            rows.Add(QaRow(key, checks[key]));
        }

        return rows;
    }

    static FieldRow QaRow(string key, JsonNode? value)
    {
        var code = CheckResultCode(value);
        return new FieldRow(key, code != null ? CheckResultLabel(code.Value) : ResultNormalizer.Text(value), "Image QA");
    }

    static List<FieldRow> RowsFromVerification(JsonObject obj)
    {
        var verification = VerificationMap(obj);
        if (verification == null)
        {
            return [];
        }

        var rows = new List<FieldRow>();
        if (verification["overall"] != null)
        {
            rows.Add(new FieldRow(
                "overall",
                LabelValue(verification["overall"], true, ReasonsForCheck("overall", verification, obj)),
                "Verify"));
        }

        foreach (var key in VerifyOrder)
        {
            if (verification[key] == null)
            {
                continue;
            }

            rows.Add(new FieldRow(
                key,
                LabelValue(verification[key], false, ReasonsForCheck(key, verification, obj)),
                "Verify"));
        }

        return rows;
    }

    static JsonObject? VerificationMap(JsonObject obj)
    {
        JsonObject? verification = null;
        if (AsObject(obj["verification"]) is JsonObject direct && direct.Count > 0)
        {
            verification = Clone(direct);
        }
        else if (AsObject(obj["status"]) is JsonObject status)
        {
            verification = new JsonObject();
            if (status["overallStatus"] != null)
            {
                verification["overall"] = CloneNode(status["overallStatus"]);
            }

            if (AsObject(status["detailsOptical"]) is JsonObject optical)
            {
                foreach (var key in new[] { "docType", "expiry", "text", "mrz", "security", "imageQA" })
                {
                    if (optical[key] != null)
                    {
                        verification[key] = CloneNode(optical[key]);
                    }
                }
            }

            if (status["portrait"] != null)
            {
                verification["portrait"] = CloneNode(status["portrait"]);
            }

            if (verification.Count == 0)
            {
                verification = null;
            }
        }

        if (verification == null)
        {
            return null;
        }

        var qa = ImageQaStatusFromChecks(obj);
        if (qa != null)
        {
            verification["imageQA"] = JsonValue.Create(qa.Value);
            AsObject(verification["reasons"])?.Remove("imageQA");
        }

        return verification;
    }

    static double? ImageQaStatusFromChecks(JsonObject obj)
    {
        var checks = ResultNormalizer.ExtractImageQualityChecks(obj["imageQuality"]);
        if (checks.Count == 0)
        {
            return null;
        }

        double worst = 1;
        var any = false;
        foreach (var value in checks)
        {
            var code = CheckResultCode(value.Value);
            if (code == null)
            {
                continue;
            }

            any = true;
            worst = WorseCheckResult(worst, code.Value);
        }

        return any ? CheckResultToStatus(worst) : null;
    }

    static List<string> ReasonsForCheck(string key, JsonObject verification, JsonObject obj)
    {
        var code = ResultNormalizer.AsDouble(verification[key]);
        if (code != null && code != 1)
        {
            return [];
        }

        if (AsObject(verification["reasons"]) is JsonObject reasons)
        {
            var stored = StringList(reasons[key]);
            if (stored.Count > 0)
            {
                return stored;
            }
        }

        return key switch
        {
            "imageQA" => FailedImageQaReasons(obj),
            "security" => FailedAuthReasons(obj["authenticity"]),
            "docType" => ["not recognized"],
            "expiry" => ["expired"],
            "text" => ["comparison failed"],
            "mrz" => ["checksums"],
            "portrait" => ["mismatch"],
            "overall" => VerifyOrder.Where(item => ResultNormalizer.AsDouble(verification[item]) == 1).ToList(),
            _ => [],
        };
    }

    static List<string> FailedImageQaReasons(JsonObject obj)
    {
        var checks = ResultNormalizer.ExtractImageQualityChecks(obj["imageQuality"]);
        var output = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in ImageQaOrder)
        {
            if (!checks.ContainsKey(key) || CheckResultCode(checks[key]) != 0)
            {
                continue;
            }

            seen.Add(key);
            output.Add(key);
        }

        foreach (var key in checks.Select(entry => entry.Key).OrderBy(key => key, StringComparer.Ordinal))
        {
            if (seen.Contains(key) || CheckResultCode(checks[key]) != 0)
            {
                continue;
            }

            output.Add(key);
        }

        return output;
    }

    static List<string> FailedAuthReasons(JsonNode? value)
    {
        var output = new List<string>();
        Walk(value);
        return output;

        void Walk(JsonNode? node)
        {
            if (node is JsonArray list)
            {
                foreach (var item in list)
                {
                    Walk(item);
                }

                return;
            }

            if (node is not JsonObject data)
            {
                return;
            }

            foreach (var key in new[] { "liveness", "barcode", "IPI", "ipi", "imagePattern", "faceMatch", "photoEmbedding" })
            {
                if (ResultNormalizer.AsDouble(data[key]) == 0)
                {
                    output.Add(key);
                    continue;
                }

                if (data[key] is not JsonArray items)
                {
                    continue;
                }

                foreach (var item in items)
                {
                    if (item is not JsonObject entry)
                    {
                        continue;
                    }

                    var result = ResultNormalizer.AsDouble(entry["result"] ?? entry["elementResult"]) ?? 1;
                    if (result != 0)
                    {
                        continue;
                    }

                    output.Add(ResultNormalizer.NonEmpty(entry["type"])
                        ?? ResultNormalizer.NonEmpty(entry["elementDiagnoseName"])
                        ?? key);
                }
            }
        }
    }

    static string LabelValue(JsonNode? value, bool overall, IReadOnlyList<string> reasons)
    {
        var code = ResultNormalizer.AsDouble(value);
        if (code != null)
        {
            var label = overall ? OverallLabel(code.Value) : CheckLabel(code.Value);
            if (code == 1 && reasons.Count > 0)
            {
                return $"{label} ({string.Join(", ", reasons)})";
            }

            return label;
        }

        var text = ResultNormalizer.NonEmpty(value);
        return text ?? (value == null ? "" : ResultNormalizer.Text(value));
    }

    static string OverallLabel(double code) => (int)code switch
    {
        0 => "Verified",
        1 => "Not verified",
        2 => "Not checked",
        _ => code.ToString(CultureInfo.InvariantCulture),
    };

    static string CheckLabel(double code) => (int)code switch
    {
        0 => "Pass",
        1 => "Fail",
        2 => "Not checked",
        _ => code.ToString(CultureInfo.InvariantCulture),
    };

    static string CheckResultLabel(double code) => (int)code switch
    {
        0 => "Fail",
        1 => "Pass",
        2 => "Not checked",
        _ => code.ToString(CultureInfo.InvariantCulture),
    };

    static double? CheckResultCode(JsonNode? value)
    {
        var number = ResultNormalizer.AsDouble(value);
        if (number == null)
        {
            return null;
        }

        if (number is 0 or 1 or 2)
        {
            return number;
        }

        if (number > 0 && number <= 1)
        {
            return number >= 0.9 ? 1 : 0;
        }

        return number;
    }

    static double WorseCheckResult(double a, double b)
    {
        if (a == 0 || b == 0)
        {
            return 0;
        }

        if (a == 2 || b == 2)
        {
            return 2;
        }

        return 1;
    }

    static double CheckResultToStatus(double checkResult)
    {
        if (checkResult == 0)
        {
            return 1;
        }

        if (checkResult == 2)
        {
            return 2;
        }

        return 0;
    }

    static List<string> StringList(JsonNode? value)
    {
        if (value is JsonArray list)
        {
            return list
                .Where(item => item is JsonValue)
                .Select(ResultNormalizer.Text)
                .Where(text => text.Length > 0 && text != "null")
                .ToList();
        }

        var text = ResultNormalizer.NonEmpty(value);
        return text == null ? [] : [text];
    }

    static List<JsonObject> SecurityPages(JsonObject sec)
    {
        if (sec["pages"] is JsonArray pages && pages.Count > 0)
        {
            return pages.Select(AsObject).Where(page => page != null).Cast<JsonObject>().ToList();
        }

        var flat = new JsonObject();
        foreach (var entry in sec)
        {
            if (!SecurityPageMeta.Contains(entry.Key))
            {
                flat[entry.Key] = CloneNode(entry.Value);
            }
        }

        if (flat.Count == 0)
        {
            return [];
        }

        if (sec["overall"] != null)
        {
            flat["overall"] = CloneNode(sec["overall"]);
        }

        if (sec["label"] != null)
        {
            flat["label"] = CloneNode(sec["label"]);
        }

        flat["pageIndex"] = 0;
        return [flat];
    }

    static void AppendSecurityValue(List<SecurityRow> rows, string pageName, string key, JsonNode? raw)
    {
        var title = CheckTitle(key, raw);
        var dict = AsObject(raw);
        if (dict == null)
        {
            rows.Add(new SecurityRow(pageName, title, StatusCell(raw)));
            return;
        }

        if (dict["score"] != null && AsObject(dict["score"]) == null)
        {
            rows.Add(new SecurityRow(pageName, title, StatusCell(raw)));
            return;
        }

        var kind = StatusKind(raw);
        rows.Add(new SecurityRow(pageName, title, StatusCell(dict["result"])));
        var checks = AsObject(dict["checks"]);
        if (checks == null)
        {
            return;
        }

        if (kind != "fail" && !checks.Any(entry => StatusKind(entry.Value) == "fail"))
        {
            return;
        }

        foreach (var entry in checks)
        {
            if (StatusKind(entry.Value) == "notChecked" && kind != "fail")
            {
                continue;
            }

            rows.Add(new SecurityRow(pageName, "  → " + CheckTitle(entry.Key, entry.Value), StatusCell(entry.Value)));
        }
    }

    static string CheckTitle(string key, JsonNode? raw)
    {
        var title = ResultNormalizer.NonEmpty(AsObject(raw)?["title"]);
        return string.IsNullOrWhiteSpace(title) ? HumanizeKey(key) : title.Trim();
    }

    static string HumanizeKey(string key)
    {
        var spaced = new StringBuilder();
        foreach (var ch in key)
        {
            if (char.IsUpper(ch) && char.ToLower(ch, CultureInfo.InvariantCulture) != ch && spaced.Length > 0)
            {
                spaced.Append(' ');
            }

            spaced.Append(ch);
        }

        var text = spaced.ToString().Trim();
        if (text.Length == 0)
        {
            return key;
        }

        return char.ToUpper(text[0], CultureInfo.InvariantCulture) + text[1..];
    }

    static string PageSide(JsonNode? value)
    {
        var number = ResultNormalizer.AsDouble(value) is double parsed
            ? (int)parsed
            : int.TryParse(ResultNormalizer.Text(value), out var integer) ? integer : 0;
        return number switch
        {
            0 => "Front",
            1 => "Back",
            _ => $"Page {number}",
        };
    }

    static string StatusKind(JsonNode? value)
    {
        var current = value;
        var data = AsObject(current);
        if (data != null)
        {
            if (data["score"] != null && AsObject(data["score"]) == null)
            {
                return "score";
            }

            current = data["result"] ?? data["label"];
        }

        if (current is JsonValue number &&
            (number.TryGetValue<int>(out _) || number.TryGetValue<long>(out _) || number.TryGetValue<double>(out _)))
        {
            return "score";
        }

        var text = ResultNormalizer.Text(current).Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "");
        if (text is "success" or "pass" or "ok" or "authentic" or "1")
        {
            return "success";
        }

        if (text is "notchecked" or "wasnotdone" or "2")
        {
            return "notChecked";
        }

        if (text is "fail" or "failed" or "error" or "notauthentic" or "0")
        {
            return "fail";
        }

        return "other";
    }

    static string StatusCell(JsonNode? value)
    {
        return StatusKind(value) switch
        {
            "success" => "Pass",
            "notChecked" => "Not checked",
            "fail" => "Fail",
            "score" => FormatScore(AsObject(value) is JsonObject data ? data["score"] ?? data["result"] : value),
            _ => OtherStatus(value),
        };
    }

    static string OtherStatus(JsonNode? value)
    {
        var data = AsObject(value);
        var current = data != null ? data["result"] ?? data["label"] : value;
        return current == null ? "" : ResultNormalizer.Text(current);
    }

    static string FormatScore(JsonNode? value)
    {
        var number = ResultNormalizer.AsDouble(value);
        if (number == null)
        {
            return value == null ? "" : ResultNormalizer.Text(value);
        }

        var text = number.Value.ToString("0.0", CultureInfo.InvariantCulture);
        while (text.EndsWith('0'))
        {
            text = text[..^1];
        }

        if (text.EndsWith('.'))
        {
            text = text[..^1];
        }

        return (text.Length == 0 ? "0" : text) + "%";
    }

    public static int DocumentPercent(string raw)
    {
        var obj = ResultNormalizer.AsView(raw);
        if (obj?["score"] == null)
        {
            return 0;
        }

        var score = ResultNormalizer.AsDouble(obj["score"]) ?? 0;
        var pct = score <= 1.0 ? score * 100.0 : score;
        return (int)Math.Clamp(pct, 0, 100);
    }

    public static IReadOnlyList<DocPoint>? DocumentCorners(string raw)
    {
        var obj = ResultNormalizer.AsView(raw);
        if (obj?["position"] is not JsonObject pos)
        {
            return null;
        }

        if (pos["corners"] is JsonArray corners && corners.Count >= 4)
        {
            var points = new List<DocPoint>(4);
            for (var i = 0; i < 4; i++)
            {
                if (corners[i] is not JsonObject point)
                {
                    return null;
                }

                var x = ResultNormalizer.AsDouble(point["x"]);
                var y = ResultNormalizer.AsDouble(point["y"]);
                if (x == null || y == null)
                {
                    return null;
                }

                points.Add(new DocPoint(x.Value, y.Value));
            }

            return points;
        }

        var left = ResultNormalizer.AsDouble(pos["left"]);
        var top = ResultNormalizer.AsDouble(pos["top"]);
        var right = ResultNormalizer.AsDouble(pos["right"]);
        var bottom = ResultNormalizer.AsDouble(pos["bottom"]);
        if (left == null || top == null || right == null || bottom == null || right <= left || bottom <= top)
        {
            return null;
        }

        return
        [
            new DocPoint(left.Value, top.Value),
            new DocPoint(right.Value, top.Value),
            new DocPoint(right.Value, bottom.Value),
            new DocPoint(left.Value, bottom.Value),
        ];
    }

    public static (double Width, double Height)? LocateImageSize(string raw)
    {
        var obj = ResultNormalizer.AsView(raw);
        if (obj == null)
        {
            return null;
        }

        var w = ResultNormalizer.AsDouble(obj["_locateImageWidth"]);
        var h = ResultNormalizer.AsDouble(obj["_locateImageHeight"]);
        if (w == null || h == null || w <= 0 || h <= 0)
        {
            return null;
        }

        return (w.Value, h.Value);
    }

    public static (double Width, double Height) UprightSnapshotSize(double width, double height) =>
        width > height ? (height, width) : (width, height);

    public static IReadOnlyList<DocPoint>? MapUprightCornersToView(
        IReadOnlyList<DocPoint> corners,
        double imageW,
        double imageH,
        double viewW,
        double viewH)
    {
        if (corners.Count < 4 || imageW <= 1 || imageH <= 1 || viewW <= 1 || viewH <= 1)
        {
            return null;
        }

        var scale = viewW / imageW > viewH / imageH ? viewW / imageW : viewH / imageH;
        var dx = (viewW - imageW * scale) / 2;
        var dy = (viewH - imageH * scale) / 2;
        return corners
            .Select(c => new DocPoint(c.X * scale + dx, (imageH - c.Y) * scale + dy))
            .ToList();
    }

    static JsonObject? AsObject(JsonNode? value) => value as JsonObject;

    static JsonObject Clone(JsonObject value) => (JsonObject)JsonNode.Parse(value.ToJsonString())!;

    static JsonNode? CloneNode(JsonNode? value) => value == null ? null : JsonNode.Parse(value.ToJsonString());
}

public readonly record struct DocPoint(double X, double Y);
