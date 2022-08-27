using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
// ReSharper disable StringLiteralTypo
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace HanumanInstitute.MediaPlayer.Avalonia.Bass;

/// <inheritdoc />
public sealed class BassDevice : IBassDevice
{
    /// <summary>
    /// Returns a singleton instance of BassDevice.
    /// </summary>
    public static BassDevice Instance { get; } = new();

    /// <summary>
    /// Gets whether Init has been called.
    /// </summary>
    public bool IsInitialized { get; private set; }
    private readonly StringBuilder _log = new(); 
    private readonly object _initLock = new();

    /// <summary>
    /// Releases BASS resources.
    /// </summary>
    ~BassDevice()
    {
        Dispose(false);
    }

    /// <inheritdoc />
    public void Init(int deviceId = -1)
    {
        if (!IsInitialized)
        {
            _log.Append("Initializing... ");
            lock (_initLock)
            {
                if (!IsInitialized)
                {
                    // TODO: allow customizing search path?
                    if (!ManagedBass.Bass.Init(deviceId, 48000))
                    {
                        _log.AppendLine("Failed.");
                        throw new InvalidOperationException("Failed to initialize BASS audio output.");
                    }
                    _log.AppendLine("Done.");

                    var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                    LoadPlugins(exePath);
                    IsInitialized = true;
                }
            }
        }
    }

    /// <summary>
    /// Returns the initialization and plugins log.
    /// </summary>
    public string Log => _log.ToString();

    /// <inheritdoc />
    public IReadOnlyList<string> LoadedPlugins
    {
        get
        {
            Init();
            return _loadedPlugins;   
        }
    }
    private readonly List<string> _loadedPlugins = new();

    /// <inheritdoc />
    public IReadOnlyList<string> FailedPlugins
    {
        get
        {
            Init();
            return _failedPlugins;            
        }
    }
    private readonly List<string> _failedPlugins = new();

    /// <inheritdoc />
    public IReadOnlyList<FileExtension> SupportedExtensions
    {
        get
        {
            Init();
            return _supportedExtensions;            
        }
    }
    private readonly List<FileExtension> _supportedExtensions = new();

    private void LoadPlugins(string path)
    {
        _log.AppendLine($"Loading plugins from: {path}");
            
        // Get bass plugins files pattern per operating system.
        var pattern = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "bass*.dll" :
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "libbass*.so" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "libbass*.dylib" : null;
        if (pattern == null) { return; }

        _supportedExtensions.Add(new FileExtension("BASS built-in", new[] { ".mp3", ".mp2", ".mp1", ".ogg", ".wav", ".aif" }));

        // look for plugins (in the executable's directory)
        var exclude = new List<string> { pattern.Replace("*", ""), pattern.Replace("*", "_fx") };
        var retryList = new List<Tuple<string, string>>();
        try
        {
            foreach (var plugin in Directory.EnumerateFiles(path, pattern))
            {
                var fileName = Path.GetFileName(plugin);
                if (!exclude.Contains(fileName))
                {
                    if (!TryLoadPlugin(plugin, fileName))
                    {
                        retryList.Add(Tuple.Create(plugin, fileName));
                    }
                }
            }
        }
        catch
        {
            // ignored
        }

        // Load order seems important; some plugins will fail but load if we try again after others are loaded.
        foreach (var plugin in retryList)
        {
            if (!TryLoadPlugin(plugin.Item1, plugin.Item2))
            {
                _failedPlugins.Add(plugin.Item2);
            }
        }
    }

    private bool TryLoadPlugin(string filePath, string fileName)
    {
        _log.Append($"{fileName} ... ");
        int hPlugin;
        if ((hPlugin = ManagedBass.Bass.PluginLoad(filePath)) == 0)
        {
            _log.AppendLine("Failed.");
            return false;
        }
        else
        {
            _log.AppendLine("Success!");
            // plugin loaded...
            _loadedPlugins.Add(fileName);
            var pInfo = ManagedBass.Bass.PluginGetInfo(hPlugin);

            // get plugin info to add to the file selector filter...
            foreach (var format in pInfo.Formats)
            {
                var extensions = format.FileExtensions.Replace("*", "").Split(';');
                _supportedExtensions.Add(new FileExtension(format.Name, extensions));
            }

            return true;
        }
    }

    private bool _disposed;
    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing">The disposing parameter should be false when called from a finalizer, and true when called from the IDisposable.Dispose method.</param>
    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Managed resources.
            }

            // Unmanaged resources.
            ManagedBass.Bass.Free();

            _disposed = true;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
