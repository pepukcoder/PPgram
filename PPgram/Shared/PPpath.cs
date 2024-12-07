using System;
using System.IO;

namespace PPgram.Shared;

internal record PPpath
{
    // folders
    public static readonly string LocalAppFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    public static readonly string UserFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    public static readonly string DesktopDownloadsFolder = Path.Combine(UserFolder, "Downloads", "PPgram");
    /// <summary> ...local/PPgram/ </summary>
    public static readonly string BaseFolder = Path.Combine(LocalAppFolder, "PPgram");
    /// <summary> ...local/PPgram/settings </summary>
    public static readonly string SettingsFolder = Path.Combine(BaseFolder, "settings");
    /// <summary> ...local/PPgram/cache </summary>
    public static readonly string CacheFolder = Path.Combine(BaseFolder, "cache");
    /// <summary> ...local/PPgram/cache/files </summary>
    public static readonly string FileCacheFolder = Path.Combine(CacheFolder, "files");

    // files
    /// <summary> ...local/PPgram/session.sesf </summary>
    public static readonly string SessionFile = Path.Combine(BaseFolder, "session.sesf");
    /// <summary> ...local/PPgram/settings/connection.sesf </summary>
    public static readonly string ConnectionFile = Path.Combine(SettingsFolder, "connection.setf");
    /// <summary> ...local/PPgram/settings/app.sesf </summary>
    public static readonly string AppSettingsFile = Path.Combine(SettingsFolder, "app.setf");
}
