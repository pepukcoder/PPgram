using System;

namespace PPgram.App;

/// <summary>
/// Singleton to keep application state and settings synchronized
/// </summary>
internal sealed class AppState
{
    private static readonly Lazy<AppState> lazy = new(() => new AppState());
    public static AppState Instance => lazy.Value;
    private AppState() { }
    /// <summary>
    /// Path to platform-specific downloads folder
    /// </summary>
    public string? DownloadsFolder { get; set; } = null;
}
