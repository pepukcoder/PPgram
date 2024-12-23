using System;
using System.Collections.Generic;

namespace PPgram.Shared;

internal static class PPFileFormats
{
    public static readonly HashSet<string> PhotoFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "jpg", "jpeg", "png", "heic"
    };

    public static readonly HashSet<string> VideoFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "mp4", "mov", "webm", "flv"
    };
}