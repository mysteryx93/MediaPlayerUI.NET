# MediaPlayerUI.NET

A .NET/WPF generic media player UI to use with any media player

# MpvPlayerUI.NET

An implementation of the UI over [Mpv.NET](https://github.com/hudec117/Mpv.NET)

## Features

* Full customizable UI with seek bar and volume control
* Fullscreen support with UI displaying on hover
* Supports mouse and keyboard shortcuts
* Can be customized to work with any media player

## Usage with MpvPlayer

1. Add [MpvPlayerUI.NET NuGet](https://www.nuget.org/packages/MpvPlayerUI.NET/) to your project.

2. Drop this code into your page.
```csharp
<MpvPlayer:MpvMediaPlayer x:Name="Player" />
```

3. If you want to load a file when the window loads, add this to the constructor
```csharp
Player.MediaPlayerInitialized += (o, e) => {
    Player.Host.Load(@"MyVideo.mp4");
};
```

4. To add keyboard shortcuts, add this code to your window

```csharp
<Window.InputBindings>
    <KeyBinding Key="Space" Command="{Binding UI.PlayPauseCommand, ElementName=Player}" />
    <KeyBinding Key="Right" Command="{Binding UI.SeekForwardCommand, ElementName=Player}" />
    <KeyBinding Key="Right" Modifiers="Ctrl" Command="{Binding UI.SeekForwardLargeCommand, ElementName=Player}" />
    <KeyBinding Key="Left" Command="{Binding UI.SeekBackCommand, ElementName=Player}" />
    <KeyBinding Key="Left" Modifiers="Ctrl" Command="{Binding UI.SeekBackLargeCommand, ElementName=Player}" />
    <KeyBinding Key="Up" Command="{Binding UI.VolumeUpCommand, ElementName=Player}" />
    <KeyBinding Key="Down" Command="{Binding UI.VolumeDownCommand, ElementName=Player}" />
    <KeyBinding Key="Enter" Modifiers="Alt" Command="{Binding UI.ToggleFullScreenCommand, ElementName=Player}" />
</Window.InputBindings>
```

## Usage with other media players

1. Add [MediaPlayerUI NuGet](https://www.nuget.org/packages/MediaPlayerUI.NET/) to your project.

2. Download MpvPlayerUI.NET from GitHub.

3. Edit MpvMediaPlayerHost, MpvMediaPlayer, and Generic.xaml to suit your needs.

## Prerequisites

To use the wrapper (and user control) you will need libmpv.

1. Download libmpv from https://mpv.srsfckn.biz/ ("dev" version)
2. Extract "mpv-1.dll" from either the "32" or "64" directories into your project.
    (A "lib" folder in your project is common practice)
3. Include the file in your IDE and instruct your build system to copy the DLL to output.
    * In Visual Studio this can be achieved so:
        1. In your Solution Explorer click the "Show All Files" button at the top.
        2. You should see the DLL show up, right click on it and select "Include In Project".
        3. Right click on the DLL and select "Properties", then change the value for "Copy to Output Directory" to "Copy Always".
4. Done!

If you wish to compile libmpv yourself, there is a [guide](https://github.com/mpv-player/mpv/blob/master/DOCS/compile-windows.md) available in the mpv repository.

## Licensing

The libmpv C API *specifically* is licensed under [ICS](https://choosealicense.com/licenses/isc/), this means that a wrapper such as this can be licensed under [MIT](https://choosealicense.com/licenses/mit/).

The rest of libmpv is licensed under [GPLv2](https://choosealicense.com/licenses/gpl-2.0/) by default, which means that any work utilising this wrapper in conjunction with libmpv is subject to GPLv2, unless libmpv is compiled using [LGPL](https://choosealicense.com/licenses/lgpl-2.1/).

See [here](https://github.com/mpv-player/mpv#license) for more information.
