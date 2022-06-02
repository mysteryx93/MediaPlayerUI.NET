# MediaPlayer.NET

A .NET media player UI to use with any media player, for WPF and Avalonia

Currently supports MPV and NAudio for WPF, and BASS for Avalonia

## Features

* Full customizable UI with seek bar and volume control
* Fullscreen support with UI displaying on hover (WPF)
* Supports mouse and keyboard shortcuts
* Can be customized to work with any media player
* Designed with MVVM and databinding in mind

[Todo](#todo)  
[Overview](#overview)  
[Using MPV Player in WPF](#using-mpv-player-in-wpf)  
[Using NAudio Player in WPF](#using-naudio-player-in-wpf)
[Using BASS Player in Avalonia](#using-bass-player-in-avalonia)
[Using Custom Players](#using-custom-players)
[Licensing](#licensing)

## TODO

- Integrate MPV video player in Avalonia (OpenGL integration, full-screen UI support)
- Avalonia style isn't completed (volume bar / speed bar)
- Contributions are welcomed to add new styles and color themes for both WPF and Avalonia!

![Screenshot](https://github.com/mysteryx93/MediaPlayerUI.NET/blob/master/Screenshot.png)

## Overview

The basic syntax is to create a `MediaPlayer` with an implementation deriving from `PlayerHostBase` inside.

```
<media:MediaPlayer x:Name="Player">
    <media:NAudioPlayerHost Source="MyAudio.mp3" />
</media:MediaPlayer>
```

### MediaPlayer Properties

[View all properties in the Wiki](wiki/MediaPlayer-Members)

#### ChangeVolumeOnMouseWheel

Gets or sets whether to change volume with the mouse wheel.

#### IsPlayPauseVisible

Gets or sets whether the Play/Pause button is visible.

#### IsStopVisible

Gets or sets whether the Stop button is visible.

#### IsLoopVisible

Gets or sets whether the Loop button is visible.

#### IsVolumeVisible

Gets or sets whether the volume is visible.

#### IsSpeedVisible

Gets or sets whether the Speed button is visible.

#### IsSeekBarVisible

Gets or sets whether the seek bar is visible.

#### MouseFullScreen

Gets or sets the mouse action that will trigger full-screen mode.

#### MousePause

Gets or sets the mouse action that will trigger pause.

#### PositionDisplay

Gets or sets how time is displayed within the player.

#### SeekMinInterval

Gets or sets the interval in milliseconds between consecutive seeks. Default = 500.


### PlayerHostBase Properties

[View all properties in the Wiki](wiki/PlayerHostBase-Members)

#### AutoPlay

Gets or sets whether to auto-play the file when setting the source.

#### IsPlaying

Gets or sets whether the media file is playing or paused.

#### IsVideoVisible

Gets or sets whether to show the video.

#### Loop

Gets or sets whether to loop.

#### Position

Gets or sets the playback position.

#### Source

Gets or sets the path to the media file to play.
If resetting the same file path, you may need to first set an empty string before resetting the value to ensure it detects the value change.

#### SpeedFloat

Gets or sets the speed multiplier.

#### SpeedInt

Gets or sets the speed as an integer, where normal playback is 0. Useful for binding to a slider.

#### Title

Gets or sets the display title of the media file.
A title is set by default and you can override it using this property.

#### Volume

Gets or sets the volume.



## Using MPV Player in WPF

1. Add [MediaPlayer.Wpf.Mpv](https://www.nuget.org/packages/MediaPlayer.Wpf.Mpv/) to your project.

2. Drop this code into your page.
```csharp
xmlns:media="https://github.com/mysteryx93/MediaPlayerUI.NET"
...
<media:MediaPlayer x:Name="Player">
    <media:MpvPlayerHost Source="MyVideo.mp4" />
</media:MediaPlayer>
```

3. [Download the latest version of libmpv from here.](https://mpv.io/installation/) MPV will require a different DLL for x64 and x86.

Copy the DLL into the project folder and add this to your project file

```xml
<ItemGroup>
    <None Update="mpv-1.dll">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

Optionally, set DllPath on MpvPlayerHost to help find the DLL.

4. To add keyboard shortcuts, add this code to your window

```xaml
<Window.InputBindings>
    <KeyBinding Key="Space" Command="{Binding PlayPauseCommand, ElementName=Player}" />
    <KeyBinding Key="Right" Command="{Binding SeekCommand, ElementName=Player}" CommandParameter="{media:Int32 1}" />
    <KeyBinding Key="Right" Modifiers="Ctrl" Command="{Binding SeekCommand, ElementName=Player}" CommandParameter="{media:Int32 10}" />
    <KeyBinding Key="Left" Command="{Binding SeekCommand, ElementName=Player}" CommandParameter="{media:Int32 -1}" />
    <KeyBinding Key="Left" Modifiers="Ctrl" Command="{Binding SeekCommand, ElementName=Player}" CommandParameter="{media:Int32 -10}" />
    <KeyBinding Key="Up" Command="{Binding ChangeVolumeCommand, ElementName=Player}" CommandParameter="{media:Int32 5}" />
    <KeyBinding Key="Down" Command="{Binding ChangeVolumeCommand, ElementName=Player}" CommandParameter="{media:Int32 -5}" />
    <KeyBinding Key="Enter" Modifiers="Alt" Command="{Binding ToggleFullScreenCommand, ElementName=Player}" />
</Window.InputBindings>
```

## Using NAudio Player in WPF

Audio-only.

1. Add [MediaPlayer.Wpf.NAudio](https://www.nuget.org/packages/MediaPlayer.Wpf.NAudio/) to your project.

2. Drop this code into your page.
```csharp
xmlns:media="https://github.com/mysteryx93/MediaPlayerUI.NET"
...
<media:MediaPlayer x:Name="Player">
    <media:NAudioPlayerHost Source="MyAudio.mp3" />
</media:MediaPlayer>
```

### NAudioPlayerHost Properties

[Standard PlayerHostBase properties](wiki/PlayerHostBase-Members)

#### Rate

Gets or sets the playback rate as a double, where 1.0 is normal speed, 0.5 is half-speed, and 2 is double-speed. Default = 1.

#### Pitch

Gets or sets the playback pitch as a double, rising or lowering the pitch by given factor without altering speed. Default = 1.

#### PositionRefreshMilliseconds

Gets or sets the interval in milliseconds at which the position bar is updated.

#### UseEffects

Gets or sets whether to enable pitch-shifting effects.
By default, effects are enabled if Rate, Pitch or Speed are set before loading a media file.
If file is loaded at normal speed and you want to allow changing it later, this property forces initializing the effects module.
This property must be set before playback. Default = False.

#### VolumeBoost

Gets or sets a value that will be multiplied to the volume. Default = 1.

#### Event: MediaError 

Occurs when the player throws an error.

#### Event: MediaFinished

Occurs when media playback is finished.

## Using BASS Player in Avalonia

Audio-only. Supports altering Pitch, Rate and Speed. Set UseEffects="True" or set Pitch/Rate/Speed at design-time.

> BASS is free for non-commercial use. If you are a non-commercial entity (eg. an individual) and you are not making any money from your product (through sales, advertising, etc) then you can use BASS in it for free. Otherwise, [a license will be required.](https://www.un4seen.com/)

Pitch-shifting audio quality is better with BASS than with NAudio/SpeedTouch.

1. Add [MediaPlayer.Avalonia.Bass](https://www.nuget.org/packages/MediaPlayer.Avalonia.Bass/) to your project.

2. Add styles to App.axaml

```xaml
<Application.Styles>
    <FluentTheme Mode="Light"/>
    <StyleInclude Source="avares://MediaPlayer.Avalonia/Styles/Default/Theme.axaml" />
    <StyleInclude Source="avares://MediaPlayer.Avalonia/Styles/Colors/Gray.axaml" />
</Application.Styles>
```

4. Drop this code into your page.
```csharp
xmlns:media="https://github.com/mysteryx93/MediaPlayerUI.NET"
...
<media:MediaPlayer x:Name="Player">
    <media:BassPlayerHost Source="MyAudio.mp3" />
</media:MediaPlayer>
```

3. [Download the latest version of BASS and BASS FX from here.](https://www.un4seen.com/)

`bass.dll` and `bass_fx.dll` will be required, and you can add other plugins to support more input formats. This library will auto-load all plugins in the folder.

4. [Create a BassDlls project like this](https://github.com/mysteryx93/MediaPlayerUI.NET/blob/master/Avalonia.Bass/BassDevice.cs)

Simply reference that project and you'll get all the DLLs for each platform.
   

### BassPlayerHost Properties

#### EffectsQuick

Gets or sets whether to use the quick mode that substantially speeds up the algorithm but may degrade the sound quality by a small amount. Default = False.

#### EffectsAntiAlias

Gets or sets whether to enable the Anti-Alias filter. Default = False.

#### EffectsAntiAliasLength

Gets or sets the Anti-Alias filter length. Default = 32.

#### EffectsSampleRateConversion

Gets or sets the sample rate conversion quality... 0 = linear interpolation, 1 = 8 point sinc interpolation, 2 = 16 point sinc interpolation, 3 = 32 point sinc interpolation, 4 = 64 point sinc interpolation. Default = 2, set to 4 for best quality.

#### Pitch

Gets or sets the playback pitch as a double, rising or lowering the pitch by given factor without altering speed. Default = 1.

#### PositionRefreshMilliseconds

Gets or sets the interval in milliseconds at which the position bar is updated.

#### Rate

Gets or sets the playback rate as a double, where 1.0 is normal speed, 0.5 is half-speed, and 2 is double-speed. Default = 1.

#### UseEffects

Gets or sets whether to enable pitch-shifting effects.
By default, effects are enabled if Rate, Pitch or Speed are set before loading a media file.
If file is loaded at normal speed and you want to allow changing it later, this property forces initializing the effects module.
This property must be set before playback. Default = False.

#### VolumeBoost

Gets or sets a value that will be multiplied to the volume. Default = 1.

#### Event: MediaError

Occurs when the player throws an error.

#### Event: MediaFinished

Occurs when media playback is finished.

#### BassDevice class

You can get information about auto-loaded BASS plugins and supported formats using the [BassDevice](https://github.com/mysteryx93/MediaPlayerUI.NET/blob/master/Avalonia.Bass/BassDevice.cs) class.


## Using Custom Players

Look at [MediaPlayer.Wpf.Mpv](https://github.com/mysteryx93/MediaPlayerUI.NET/tree/master/Wpf.Mpv) or [MediaPlayer.Avalonia.Bass](https://github.com/mysteryx93/MediaPlayerUI.NET/tree/master/Avalonia.Bass). Integrating a new player is quite straightforward.

[VapourSynthViewer.NET](https://github.com/mysteryx93/VapourSynthViewer.NET) is an example of using the UI for previewing Vapoursynth video scripts.

## Licensing

This library is licensed under MIT.

### MPV

MPV.NET-lib is licensed under MIT.

The rest of libmpv is licensed under GPLv2 by default, which means that any work utilising this wrapper in conjunction with libmpv is subject to GPLv2, unless libmpv is compiled using LGPL.

In simple terms, once you use the "libmpv" files (DLL) you downloaded, your application must be licensed under GPLv2. [See here for more information.](https://github.com/mpv-player/mpv/blob/master/Copyright)

### NAudio

NAudio is licensed under MIT. SoundTouch is licensed under LGPL v2.1

### BASS

BASS is free for non-commercial use. If you are a non-commercial entity (eg. an individual) and you are not making any money from your product (through sales, advertising, etc) then you can use BASS in it for free. [Otherwise, one of the following licences will be required.](https://www.un4seen.com/)

ManagedBass is licensed under MIT.


### Author

Brought to you by [Etienne Charland aka Hanuman](https://www.spiritualselftransformation.com/). Made by a Lightworker in his spare time.
