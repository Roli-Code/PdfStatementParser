using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using PdfStatementParser.Models;

namespace PdfStatementParser.Services;

/// <summary>
/// Helper class for JSON serialization of bank statements
/// </summary>
public static class StatementJsonSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
        }
    };

    /// <summary>
    /// Serializes a statement to JSON string
    /// </summary>
    public static string ToJson(Statement statement, bool indent = true)
    {
        var options = indent ? DefaultOptions : new JsonSerializerOptions(DefaultOptions) { WriteIndented = false };
        return JsonSerializer.Serialize(statement, options);
    }

    /// <summary>
    /// Serializes a statement to JSON bytes
    /// </summary>
    public static byte[] ToJsonBytes(Statement statement, bool indent = true)
    {
        var options = indent ? DefaultOptions : new JsonSerializerOptions(DefaultOptions) { WriteIndented = false };
        return JsonSerializer.SerializeToUtf8Bytes(statement, options);
    }

    /// <summary>
    /// Deserializes a statement from JSON string
    /// </summary>
    public static Statement? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<Statement>(json, DefaultOptions);
    }

    /// <summary>
    /// Deserializes a statement from JSON bytes
    /// </summary>
    public static Statement? FromJsonBytes(byte[] json)
    {
        if (json == null || json.Length == 0)
            return null;

        return JsonSerializer.Deserialize<Statement>(json, DefaultOptions);
    }

    /// <summary>
    /// Gets custom JSON serializer options
    /// </summary>
    public static JsonSerializerOptions GetOptions(bool indent = true)
    {
        return new JsonSerializerOptions(DefaultOptions) { WriteIndented = indent };
    }
}
