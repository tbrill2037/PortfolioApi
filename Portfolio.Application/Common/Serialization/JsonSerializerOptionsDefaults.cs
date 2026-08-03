using System.Text.Json;

namespace Portfolio.Application.Common.Serialization;

/// <summary>
/// Provides default JSON serialization settings used throughout the application.
/// </summary>
public static class JsonSerializerOptionsDefaults
{
    public static readonly JsonSerializerOptions Default =
        new()
        {
            PropertyNameCaseInsensitive = true
        };
}