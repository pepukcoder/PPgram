using CommunityToolkit.Mvvm.ComponentModel;
using PPgram.Net;
using System;

namespace PPgram.App;

/// <summary>
/// Stores application state and settings
/// </summary>
internal sealed partial class AppState : ObservableObject
{
    private static readonly Lazy<AppState> lazy = new(() => new AppState());
    public static AppState Instance => lazy.Value;
    private AppState() { }

    #region messages
    /// <summary>
    /// Defines time before all messages except latest <see cref="MessagesFetchMinimum">minimum</see> will be unloaded from chat (in seconds)
    /// </summary>
    public int MessagesUnloadTime { get; set; } = 60;
    /// <summary>
    /// Defines how many messages should always be fetched
    /// </summary>
    public int MessagesFetchMinimum { get; set; } = 30;
    /// <summary>
    /// Defines how many messages will be fetched while scrolling chat
    /// </summary>
    public int MessagesFetchAmount { get; set; } = 20;
    /// <summary>
    /// Defines how many messages need to be left out of scroll viewport to trigger prefetching
    /// </summary>
    public int MessagesFetchThreshold { get; set; } = 5;
    /// <summary>
    /// Defines the delay for fetch requests while scrolling (in milliseconds)
    /// </summary>
    public int MessagesFetchDelay { get; set; } = 200;
    #endregion
    #region files
    /// <summary>
    /// Defines if client should auto download files
    /// </summary>
    public bool FilesAutoDownload { get; set; } = false;
    /// <summary>
    /// Defines maximum size for auto downloading files (in bytes)
    /// </summary>
    public int FilesAutoDownloadMaxSize { get; set; } = 50000;
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
        JsonHost = "127.0.0.1",
        FilesHost = "127.0.0.1",
        JsonPort = 3000,
        FilesPort = 8080
    };
}
