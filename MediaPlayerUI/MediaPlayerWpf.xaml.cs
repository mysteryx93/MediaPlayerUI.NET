using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Windows.Forms.Integration;
using EmergenceGuardian.MediaPlayerUI;
using System.ComponentModel;

namespace EmergenceGuardian.MediaPlayerUI {
	/// <summary>
	/// Interaction logic for WpfMPlayerControl.xaml
	/// </summary>
public partial class MediaPlayerWpf {
	public MediaPlayerWpf() {
		InitializeComponent();
		SetUI(this, MediaUI);
	}

	public static DependencyPropertyKey UIPropertyKey = DependencyProperty.RegisterAttachedReadOnly("UI", typeof(PlayerControls), typeof(MediaPlayerWpf), new PropertyMetadata(null));
	public static DependencyProperty UIProperty = UIPropertyKey.DependencyProperty;
	public PlayerControls UI { get => (PlayerControls)base.GetValue(UIProperty); private set => base.SetValue(UIPropertyKey, value); }
	[AttachedPropertyBrowsableForType(typeof(MediaPlayerWpf))]
	public static PlayerControls GetUI(UIElement element) {
		return (PlayerControls)element.GetValue(UIProperty);
	}
	public static void SetUI(UIElement element, PlayerControls value) {
		element.SetValue(UIPropertyKey, value);
	}

	public static DependencyProperty HostProperty = DependencyProperty.RegisterAttached("Host", typeof(PlayerBase), typeof(MediaPlayerWpf), new PropertyMetadata(null, OnHostChanged));
	public PlayerBase Host { get => (PlayerBase)base.GetValue(HostProperty); set => base.SetValue(HostProperty, value); }
	private static void OnHostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
		MediaPlayerWpf P = d as MediaPlayerWpf;
		if (e.OldValue != null)
			P.HostGrid.Children.Remove(e.OldValue as PlayerBase);
		if (e.NewValue != null) {
			P.HostGrid.Children.Add(e.NewValue as PlayerBase);
			GetUI(P).PlayerHost = e.NewValue as PlayerBase;
		}
	}
	[AttachedPropertyBrowsableForType(typeof(MediaPlayerWpf))]
	public static PlayerBase GetHost(UIElement element) {
		return (PlayerBase)element.GetValue(HostProperty);
	}
	public static void SetHost(UIElement element, PlayerBase value) {
		element.SetValue(HostProperty, value);
	}
}
}