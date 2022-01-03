# MediaPlayer.NET

A .NET media player UI to use with any media player, for WPF and Avalonia

Currently supports MPV and NAudio for WPF, and BASS for Avalonia

## Features

* Full customizable UI with seek bar and volume control
* Fullscreen support with UI displaying on hover (WPF)
* Supports mouse and keyboard shortcuts
* Can be customized to work with any media player
* Designed with MVVM and databinding in mind

## TODO

- Integrate MPV video player in Avalonia (OpenGL integration, full-screen UI support)
- Avalonia style isn't completed (volume bar / speed bar)
- Contributions are welcomed to add new styles and color themes for both WPF and Avalonia!

![Screenshot](https://github.com/mysteryx93/MediaPlayerUI.NET/blob/master/Screenshot.png)

## Using MPV Player in WPF (MediaPlayer.Wpf.Mpv)

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

## Using NAudio Player in WPF (MediaPlayer.Wpf.NAudio)

Audio-only. Supports altering Pitch, Rate and Speed. Set UseEffects="True" or set Pitch/Rate/Speed at design-time.

1. Add [MediaPlayer.Wpf.NAudio](https://www.nuget.org/packages/MediaPlayer.Wpf.NAudio/) to your project.

2. Drop this code into your page.
```csharp
xmlns:media="https://github.com/mysteryx93/MediaPlayerUI.NET"
...
<media:MediaPlayer x:Name="Player">
    <media:NAudioPlayerHost Source="MyAudio.mp3" />
</media:MediaPlayer>
```

## Using BASS Player in Avalonia (MediaPlayer.Avalonia.Bass)

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

Copy `bass.dll` and `bass_fx.dll` into the project folder and add this to your project file. BASS will require different DLLs for x64 and x86.

```xml
<ItemGroup>
    <None Update="bass.dll">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="bass_fx.dll">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

Alternatively, you can add something like this to your project file

```xml
<Target Name="CopyCustomContent" AfterTargets="AfterBuild">
  <ItemGroup Condition=" '$(Platform)' != 'x86' ">
    <BassFiles Include="..\DLL\Windows-x64\bass*.dll" />
    <BassFiles Include="..\DLL\Linux-x64\libbass*.so" />
    <BassFiles Include="..\DLL\MacOS-x64\libbass*.dylib" />
  </ItemGroup>
  <ItemGroup Condition=" '$(Platform)' == 'x86' ">
    <BassFiles Include="..\DLL\Windows-x86\bass*.dll" />
    <BassFiles Include="..\DLL\Linux-x86\libbass*.so" />
  </ItemGroup>
  <Copy SourceFiles="@(BassFiles)" DestinationFolder="$(OutDir)" SkipUnchangedFiles="true" />
</Target>
<Target Name="CopyCustomContentOnPublish" AfterTargets="Publish">
  <ItemGroup Condition=" '$(Platform)' != 'x86' ">
    <BassFiles Include="..\DLL\Windows-x64\bass*.dll" />
    <BassFiles Include="..\DLL\Linux-x64\libbass*.so" />
    <BassFiles Include="..\DLL\MacOS-x64\libbass*.dylib" />
  </ItemGroup>
  <ItemGroup Condition=" '$(Platform)' == 'x86' ">
    <BassFiles Include="..\DLL\Windows-x86\bass*.dll" />
    <BassFiles Include="..\DLL\Linux-x86\libbass*.so" />
  </ItemGroup>
  <Copy SourceFiles="@(BassFiles)" DestinationFolder="$(OutDir)" />
</Target>
```

You can get information about auto-loaded BASS plugins and supported formats using the [BassDevice](https://github.com/mysteryx93/MediaPlayerUI.NET/blob/master/Avalonia.Bass/BassDevice.cs) class.

## Usage with other media players

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


**Author: Etienne Charland**
