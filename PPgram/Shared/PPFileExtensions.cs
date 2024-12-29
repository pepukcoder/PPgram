using System;
using System.Collections.Generic;

namespace PPgram.Shared;

internal static class PPFileExtensions
{
    public static readonly HashSet<string> PhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".heic"
    };
    public static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".webm", ".flv"
    };
}