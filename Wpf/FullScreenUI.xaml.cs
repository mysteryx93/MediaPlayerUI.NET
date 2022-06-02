using System;
using System.Windows;
using System.Windows.Controls;

namespace HanumanInstitute.MediaPlayer.Wpf;

/// <summary>
/// Interaction logic for FullScreenUI.xaml
/// </summary>
public partial class FullScreenUI : Window
{
    /// <summary>
    /// Gets whether the full-screen window is closing.
    /// </summary>
    public bool IsClosing { get; private set; }

    /// <summary>
    /// Initializes a new instance of the FullScreenUI class.
    /// </summary>
    public FullScreenUI()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Returns the grid control that will contain the media player.
    /// </summary>
    public Grid ContentGrid => MainGrid;

    /// <summary>
    /// Closes the window. Ensures that Close is called only once.
    /// </summary>
    public void CloseOnce()
    {
        if (!IsClosing)
        {
            IsClosing = true;
            Close();
        }
    }

    private void UI_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        IsClosing = true;
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (IsLoaded)
        {
            CloseOnce();
        }
    }
}
