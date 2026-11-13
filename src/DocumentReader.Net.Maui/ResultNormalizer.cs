using System.Globalization;
using System.Text.Json.Nodes;

namespace DocumentReader.Net.Maui;

/// <summary>
/// Folds Android and iOS Document Reader JSON into the same object the Flutter
/// and React Native demos display. Same rules as normalize_result.dart.
/// </summary>
public static class ResultNormalizer
{
    static readonly string[] ImageQaOrder =
    [
        "focus", "glares", "resolution", "colorness", "perspective", "bounds",
        "portrait", "handwritten", "brightness", "occlusion",
    ];

    static readonly string[] VerifyCheckKeys =
    [
        "docType", "expiry", "text", "mrz", "security", "imageQA", "portrait",
    ];

    static readonly HashSet<string> KnownDocKeys = new(StringComparer.Ordinal)
    {
        "errorCode", "documentName", "countryName", "score", "msg", "verification",
        "status", "imageQuality", "ocr", "mrz", "barcode", "authenticity", "images",
        "position", "_locateImageWidth", "_locateImageHeight",
    };

    static readonly HashSet<string> KnownVerifyKeys = new(StringComparer.Ordinal)
    {
        "overall", "docType", "expiry", "text", "mrz", "security", "imageQA",
        "portrait", "reasons",
    };

    public static string NormalizeJson(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.Length == 0)
        {
            return raw;
        }

        try
        {
            JsonNode.Parse(trimmed);
        }
        catch (System.Text.Json.JsonException)
        {
            return raw;
        }

        return Build(trimmed).ToJsonString();
    }

    public static JsonObject? AsView(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        JsonObject doc;
        try
        {
            doc = Build(trimmed);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }

        return doc.Count == 0 ? null : doc;
    }

    public static JsonObject ExtractImageQualityChecks(JsonNode? value)
    {
        var asObj = AsObject(value);
        if (asObj != null)
        {
            if (AsObject(asObj["checks"]) is JsonObject checks)
            {
                return CloneObject(checks);
            }

            var known = new HashSet<string>(ImageQaOrder);
            var named = new JsonObject();
            foreach (var entry in asObj)
            {
                if (known.Contains(entry.Key) && entry.Value is not JsonObject && entry.Value is not JsonArray)
                {
                    named[entry.Key] = Clone(entry.Value);
                }
            }

            if (named.Count > 0)
            {
                return named;
            }
        }

        var pages = new List<JsonNode?>();
        if (value is JsonArray list)
        {
            foreach (var item in list)
            {
                pages.Add(item);
            }
        }
        else if (asObj?["list"] is JsonArray nested)
        {
            pages.Add(nested);
        }

        var output = new JsonObject();
        foreach (var page in pages)
        {
            JsonArray checks;
            if (AsObject(page) is JsonObject pageObj)
            {
                if (AsObject(pageObj["checks"]) is JsonObject nestedChecks)
                {
                    foreach (var entry in nestedChecks)
                    {
                        output[entry.Key] = Clone(entry.Value);
                    }

                    continue;
                }

                checks = pageObj["list"] as JsonArray ?? new JsonArray();
            }
            else if (page is JsonArray pageList)
            {
                checks = pageList;
            }
            else
            {
                continue;
            }

            foreach (var item in checks)
            {
                var cell = AsObject(item);
                if (cell == null)
                {
                    continue;
                }

                var result = cell["result"] ?? cell["Result"];
                var name = NonEmpty(cell["id"]) ?? NonEmpty(cell["name"]) ?? "";
                if (name.Length > 0)
                {
                    output[name] = Clone(result);
                    continue;
                }

                var type = AsInt(cell["type"] ?? cell["Type"]);
                if (type == null)
                {
                    continue;
                }

                output[IdName(type.Value)] = Clone(result);
            }
        }

        return output;
    }

    static JsonObject Build(string raw)
    {
        var root = ParseRoot(raw);
        if (root == null)
        {
            return new JsonObject();
        }

        var output = CloneObject(root);
        if (AsObject(output["verification"]) is JsonObject rawVerification)
        {
            var flat = CanonicalizeVerification(rawVerification);
            if (flat != null)
            {
                output["verification"] = flat;
            }
        }

        if (!HasFlatVerification(output) && AsObject(output["status"]) is JsonObject status)
        {
            var mapped = VerificationFromStatus(status);
            if (mapped != null)
            {
                output["verification"] = mapped;
            }
        }

        if (output["imageQuality"] != null)
        {
            var checks = ExtractImageQualityChecks(output["imageQuality"]);
            if (checks.Count > 0)
            {
                output["imageQuality"] = new JsonObject { ["checks"] = checks };
            }
        }

        if (output["images"] is JsonArray)
        {
            var images = NormalizeImages(output["images"]);
            if (images != null)
            {
                output["images"] = images;
            }
        }

        return ToDocJson(output);
    }

    static JsonObject ToDocJson(JsonObject source)
    {
        var extra = new JsonObject();
        foreach (var entry in source)
        {
            if (!KnownDocKeys.Contains(entry.Key))
            {
                extra[entry.Key] = Clone(entry.Value);
            }
        }

        var doc = extra;
        SetNumber(doc, "errorCode", source["errorCode"]);
        SetString(doc, "documentName", source["documentName"]);
        SetString(doc, "countryName", source["countryName"]);
        SetNumber(doc, "score", source["score"]);
        SetString(doc, "msg", source["msg"]);
        if (AsObject(source["verification"]) is JsonObject verification)
        {
            doc["verification"] = VerificationToJson(verification);
        }

        if (source["status"] != null)
        {
            doc["status"] = Clone(source["status"]);
        }

        if (AsObject(source["imageQuality"]) is JsonObject imageQuality)
        {
            doc["imageQuality"] = CloneObject(imageQuality);
        }

        CopyObject(doc, "ocr", source);
        CopyObject(doc, "mrz", source);
        CopyObject(doc, "barcode", source);
        if (source["authenticity"] != null)
        {
            doc["authenticity"] = Clone(source["authenticity"]);
        }

        if (NormalizeImages(source["images"]) is JsonArray images)
        {
            doc["images"] = images;
        }

        if (source["position"] != null)
        {
            doc["position"] = Clone(source["position"]);
        }

        SetNumber(doc, "_locateImageWidth", source["_locateImageWidth"]);
        SetNumber(doc, "_locateImageHeight", source["_locateImageHeight"]);
        return doc;
    }

    static JsonObject VerificationToJson(JsonObject raw)
    {
        var extra = new JsonObject();
        foreach (var entry in raw)
        {
            if (!KnownVerifyKeys.Contains(entry.Key))
            {
                extra[entry.Key] = Clone(entry.Value);
            }
        }

        var output = extra;
        SetNumber(output, "overall", raw["overall"]);
        foreach (var key in VerifyCheckKeys)
        {
            SetNumber(output, key, raw[key]);
        }

        if (AsObject(raw["reasons"]) is JsonObject reasons)
        {
            var mapped = new JsonObject();
            foreach (var entry in reasons)
            {
                if (entry.Value is JsonArray list)
                {
                    var values = new JsonArray();
                    foreach (var item in list)
                    {
                        var text = Text(item);
                        if (text.Length > 0 && text != "null")
                        {
                            values.Add(text);
                        }
                    }

                    if (values.Count > 0)
                    {
                        mapped[entry.Key] = values;
                    }
                }
                else if (NonEmpty(entry.Value) is string one)
                {
                    mapped[entry.Key] = new JsonArray(one);
                }
            }

            if (mapped.Count > 0)
            {
                output["reasons"] = mapped;
            }
        }

        return output;
    }

    static JsonObject? VerificationFromStatus(JsonObject status)
    {
        var mapped = new JsonObject();
        if (status["overallStatus"] != null && AsDouble(status["overallStatus"]) is double overall)
        {
            mapped["overall"] = JsonValue.Create(OverallStatusToFacePlugin(overall));
        }

        var optical = AsObject(status["detailsOptical"]);
        if (optical != null)
        {
            foreach (var key in VerifyCheckKeys)
            {
                if (key == "portrait" || optical[key] == null)
                {
                    continue;
                }

                mapped[key] = AsDouble(optical[key]) is double number
                    ? JsonValue.Create(CheckStatusToFacePlugin(number))
                    : Clone(optical[key]);
            }
        }

        var portrait = status["portrait"] ?? optical?["portrait"];
        if (portrait != null)
        {
            mapped["portrait"] = AsDouble(portrait) is double number
                ? JsonValue.Create(CheckStatusToFacePlugin(number))
                : Clone(portrait);
        }

        return mapped.Count == 0 ? null : VerificationToJson(mapped);
    }

    static JsonObject? CanonicalizeVerification(JsonObject raw)
    {
        var checks = AsObject(raw["checks"]);
        var nested = checks != null && checks.Any(entry => entry.Value is JsonObject);
        if (!nested)
        {
            var flat = CloneObject(raw);
            if (flat["overall"] == null && raw["result"] != null && AsDouble(raw["result"]) is double result)
            {
                flat["overall"] = JsonValue.Create(result);
            }

            flat.Remove("checks");
            flat.Remove("result");
            flat.Remove("label");
            return flat.Count == 0 ? null : VerificationToJson(flat);
        }

        var mapped = new JsonObject();
        if (AsDouble(raw["result"] ?? raw["overall"]) is double overallRaw)
        {
            mapped["overall"] = JsonValue.Create(OverallStatusToFacePlugin(overallRaw));
        }

        var reasons = new JsonObject();
        if (AsObject(raw["reasons"]) is JsonObject existing)
        {
            foreach (var entry in existing)
            {
                if (entry.Value is JsonArray list)
                {
                    var values = new JsonArray();
                    foreach (var item in list)
                    {
                        var text = Text(item);
                        if (text.Length > 0 && text != "null")
                        {
                            values.Add(text);
                        }
                    }

                    if (values.Count > 0)
                    {
                        reasons[entry.Key] = values;
                    }
                }
                else if (NonEmpty(entry.Value) is string one)
                {
                    reasons[entry.Key] = new JsonArray(one);
                }
            }
        }

        foreach (var key in VerifyCheckKeys)
        {
            var cell = checks![key];
            if (cell == null)
            {
                continue;
            }

            if (cell is JsonObject cellObject)
            {
                if (AsDouble(cellObject["result"] ?? cellObject["Result"]) is double number)
                {
                    mapped[key] = JsonValue.Create(CheckStatusToFacePlugin(number));
                }

                var reason = NonEmpty(cellObject["reason"]) ?? NonEmpty(cellObject["Reason"]);
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    reasons[key] = new JsonArray(reason.Trim());
                }
            }
            else if (AsDouble(cell) is double number)
            {
                mapped[key] = JsonValue.Create(CheckStatusToFacePlugin(number));
            }
        }

        if (reasons.Count > 0)
        {
            mapped["reasons"] = reasons;
        }

        return mapped.Count == 0 ? null : VerificationToJson(mapped);
    }

    static bool HasFlatVerification(JsonObject obj)
    {
        var verification = AsObject(obj["verification"]);
        if (verification == null || verification.Count == 0)
        {
            return false;
        }

        var hasField = verification["overall"] != null || VerifyCheckKeys.Any(key => verification[key] != null);
        if (!hasField)
        {
            return false;
        }

        var checks = AsObject(verification["checks"]);
        if (checks == null)
        {
            return true;
        }

        return !checks.Any(entry => entry.Value is JsonObject);
    }

    static JsonArray? NormalizeImages(JsonNode? value)
    {
        if (value is not JsonArray list)
        {
            return null;
        }

        var output = new JsonArray();
        foreach (var item in list)
        {
            if (item is JsonValue && NonEmpty(item) is string text && text.Length >= 32)
            {
                output.Add(new JsonObject { ["name"] = "Image", ["image"] = text });
                continue;
            }

            var data = AsObject(item);
            if (data == null)
            {
                continue;
            }

            var image = NonEmpty(data["image"]) ?? NonEmpty(data["value"]) ?? NonEmpty(data["data"]) ?? "";
            if (image.Length < 32)
            {
                continue;
            }

            var name = NonEmpty(data["name"]) ?? NonEmpty(data["fieldName"]) ?? NonEmpty(data["role"]) ?? "Image";
            var card = new JsonObject { ["name"] = name, ["image"] = image };
            if (data["source"] is JsonValue)
            {
                var source = NonEmpty(data["source"]);
                if (source != null)
                {
                    card["source"] = source;
                }
            }

            output.Add(card);
        }

        return output;
    }

    static double OverallStatusToFacePlugin(double code) => code == 1 ? 0 : code == 0 ? 1 : code;

    static double CheckStatusToFacePlugin(double code) => code == 1 ? 0 : code == 0 ? 1 : code;

    static string IdName(int type) => type switch
    {
        0 => "glares",
        1 => "focus",
        2 => "resolution",
        3 => "colorness",
        4 => "perspective",
        5 => "bounds",
        7 => "portrait",
        8 => "handwritten",
        9 => "brightness",
        10 => "occlusion",
        _ => type.ToString(CultureInfo.InvariantCulture),
    };

    static JsonObject? ParseRoot(string raw)
    {
        try
        {
            return JsonNode.Parse(raw) as JsonObject;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    static JsonObject? AsObject(JsonNode? value) => value as JsonObject;

    static JsonObject CloneObject(JsonObject value) => (JsonObject)JsonNode.Parse(value.ToJsonString())!;

    static JsonNode? Clone(JsonNode? value) => value == null ? null : JsonNode.Parse(value.ToJsonString());

    public static double? AsDouble(JsonNode? value)
    {
        if (value is not JsonValue json)
        {
            return null;
        }

        if (json.TryGetValue<double>(out var number))
        {
            return number;
        }

        if (json.TryGetValue<int>(out var integer))
        {
            return integer;
        }

        if (json.TryGetValue<long>(out var longer))
        {
            return longer;
        }

        if (json.TryGetValue<string>(out var text) &&
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    static int? AsInt(JsonNode? value) => AsDouble(value) is double number ? (int)number : null;

    public static string? NonEmpty(JsonNode? value)
    {
        if (value is JsonValue json && json.TryGetValue<string>(out var text) && text.Length > 0)
        {
            return text;
        }

        return null;
    }

    public static string Text(JsonNode? value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is JsonValue json)
        {
            if (json.TryGetValue<string>(out var text))
            {
                return text;
            }

            if (json.TryGetValue<bool>(out var flag))
            {
                return flag ? "true" : "false";
            }
        }

        return value.ToJsonString();
    }

    static void SetNumber(JsonObject target, string key, JsonNode? value)
    {
        if (AsDouble(value) is double number)
        {
            target[key] = JsonValue.Create(number % 1 == 0 ? (long)number : number);
        }
    }

    static void SetString(JsonObject target, string key, JsonNode? value)
    {
        var text = NonEmpty(value);
        if (text != null)
        {
            target[key] = text;
        }
    }

    static void CopyObject(JsonObject target, string key, JsonObject source)
    {
        if (AsObject(source[key]) is JsonObject value)
        {
            target[key] = CloneObject(value);
        }
    }
}
