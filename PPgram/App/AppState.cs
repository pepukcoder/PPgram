using CommunityToolkit.Mvvm.ComponentModel;
using PPgram.Net;
using System;

namespace PPgram.App;

/// <summary>
/// Singleton to keep application state and settings synchronized
/// </summary>
internal sealed partial class AppState : ObservableObject
{
    private static readonly Lazy<AppState> lazy = new(() => new AppState());
    public static AppState Instance => lazy.Value;
    private AppState() { }
     
    /// <summary>
    /// Path to platform-specific downloads folder
    /// </summary>
    public string? DownloadsFolder { get; set; } = null;
    /// <summary>
    /// Settings for connection to server
    /// </summary>
    public ConnectionOptions ConnectionOptions { get; set; } = new()
    {
        Host = "127.0.0.1",
        JsonPort = 3000,
        FilesPort = 8080
    };
}
