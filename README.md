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
    Player.Host.Source = "MyVideo.mp4";
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

[See here](https://github.com/hudec117/Mpv.NET#prerequisites)

## Licensing

[See here](https://github.com/hudec117/Mpv.NET#licensing)
