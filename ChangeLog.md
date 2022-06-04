 

v1.0.1

- Added code documentation
- Moved Source and Title properties from implementations to PlayerHostBase
- Updated online documentation
- Implemented IDisposable for MediaPlayer.Wpf.Mpv
- MediaPlayer.Avalonia.Bass, added EffectsAntiAlias and EffectsAntiAliasLength to customize output
- MediaPlayer.Avalonia.Bass, added EffectsQuick to reduce CPU usage
- MediaPlayer.Avalonia.Bass, added EffectsSampleRateConversion and EffectsFloat to improve quality
- Split WPF and Avalonia into 2 solutions
- Include DLLs in sample projects
- Added BassDlls projects to facilitate adding BASS for various platforms
- BASS errors now throw BassException instead of InvalidOperationException

TODO:
- Updated MediaPlayer.Wpf.Mpv to support `mpv-2.dll`
- Updated MediaPlayer.Avalonia volume design