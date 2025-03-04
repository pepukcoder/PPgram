﻿using System;
using System.IO;

namespace PPgram.Shared;

internal record PPPath
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
    /// <summary> ...local/PPgram/cache/avatars </summary>
    public static readonly string AvatarCacheFolder = Path.Combine(CacheFolder, "avatars");
    // files
    /// <summary> ...local/PPgram/session.sesf </summary>
    public static readonly string SessionFile = Path.Combine(BaseFolder, "session.sesf");
    /// <summary> ...local/PPgram/settings/connection.setf </summary>
    public static readonly string ConnectionFile = Path.Combine(SettingsFolder, "connection.setf");
    /// <summary> ...local/PPgram/settings/folders.setf </summary>
    public static readonly string FoldersFile = Path.Combine(SettingsFolder, "folders.setf");
    /// <summary> ...local/PPgram/settings/app.setf </summary>
    public static readonly string AppSettingsFile = Path.Combine(SettingsFolder, "app.setf");
    /// <summary> ...local/PPgram/cache/cachelinks.db </summary>
    public static readonly string CacheDBFile = Path.Combine(CacheFolder, "cachelinks.db");
}
