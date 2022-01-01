using System;
using System.Collections.Generic;

namespace HanumanInstitute.MediaPlayer.Avalonia.Bass;

/// <summary>
/// Manages the initialization of the BASS output device, ensuring it is initialized once.
/// </summary>
public interface IBassDevice
{
    bool IsInitialized { get; }
    /// <summary>
    /// Initializes the BASS device. It is automatically called before use.
    /// </summary>
    /// <exception cref="InvalidOperationException">Failed to initialize BASS audio output.</exception>
    void Init();
    /// <summary>
    /// Returns a list of plugins that have been successfully loaded.
    /// </summary>
    IReadOnlyList<string> LoadedPlugins { get; }
    /// <summary>
    /// Returns a list of plugins that failed to load.
    /// </summary>
    IReadOnlyList<string> FailedPlugins { get; }
    /// <summary>
    /// Returns a list of formats and file extensions supported by BASS and loaded plugins.
    /// </summary>
    IReadOnlyList<FileExtension> SupportedExtensions { get; }
}
