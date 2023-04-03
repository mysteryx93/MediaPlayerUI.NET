using System;
using System.Collections.Generic;

namespace HanumanInstitute.MediaPlayer.Avalonia.Bass;

/// <summary>
/// Manages the initialization of the BASS output device, ensuring it is initialized once.
/// </summary>
public interface IBassDevice : IDisposable
{
    /// <summary>
    /// Gets whether Init has been called.
    /// </summary>
    bool IsDeviceInitialized { get; }
    /// <summary>
    /// Initializes the BASS device with no output device.
    /// </summary>
    void InitNoSound();
    /// <summary>
    /// Initializes the BASS device. It is automatically called before use. If called multiple times with different output sample rates,
    /// the device will be re-initialized with the new sample rate.
    /// </summary>
    /// <param name="deviceId">The device to use... -1 = default device, 0 = no sound, 1 = first real output device.</param>
    /// <param name="outputSampleRate">The output sample rate (only on Linux).</param>
    /// <exception cref="InvalidOperationException">Failed to initialize BASS audio output.</exception>
    void Init(int deviceId = -1, int? outputSampleRate = null);
    /// <summary>
    /// Loads all BASS plugins.
    /// </summary>
    void InitPlugins();
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
    /// <summary>
    /// Verify that some plugins have been loaded. Throws an exception with the log if no plugin is loaded.
    /// </summary>
    /// <exception cref="InvalidOperationException">No plugin has been loaded.</exception>
    public void VerifyPlugins();
    /// <summary>
    /// Fees all BASS resources.
    /// </summary>
    public void Free();
}
