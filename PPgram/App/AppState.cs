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

#region fetching
    /// <summary>
    /// Defines how often fetching requests will be send while scrolling (in milliseconds)
    /// </summary>
    public int MessagesFetchDelay { get; set; } = 200;
    /// <summary>
    /// Defines how many messages will be fetched while scrolling chat
    /// </summary>
    public int MessagesFetchAmount { get; set; } = 5;
    /// <summary>
    /// Defines how many messages need to be left out of scroll viewport to trigger prefetching
    /// </summary>
    public int MessagesFetchThreshold { get; set; } = 3;
#endregion
    /// <summary>
    /// Path to platform-specific downloads folder
    /// </summary>
    public string? DownloadsFolder { get; set; } = null;
    /// <summary>
    /// Settings for server connection
    /// </summary>
    public ConnectionOptions ConnectionOptions { get; set; } = new()
    {
        Host = "127.0.0.1",
        JsonPort = 3000,
        FilesPort = 8080
    };
}
