using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Data;

// PropertyDescriptor AddValueChanged Alternative
// https://agsmith.wordpress.com/2008/04/07/propertydescriptor-addvaluechanged-alternative/
// License: public domain

namespace HanumanInstitute.MediaPlayer.Wpf;

/// <summary>
/// Tracks changes to a dependency property while avoiding memory leaks.
/// </summary>
public sealed class PropertyChangeNotifier : DependencyObject, IDisposable
{

    private readonly WeakReference _propertySource;

    /// <summary>
    /// Initializes a new instance of the PropertyChangeNotifier.
    /// </summary>
    /// <param name="propertySource">The control containing the property.</param>
    /// <param name="path">The name of the property to track.</param>
    public PropertyChangeNotifier(DependencyObject propertySource, string path)
        : this(propertySource, new PropertyPath(path))
    { }

    /// <summary>
    /// Initializes a new instance of the PropertyChangeNotifier.
    /// </summary>
    /// <param name="propertySource">The control containing the property.</param>
    /// <param name="property">The property to track.</param>
    public PropertyChangeNotifier(DependencyObject propertySource, DependencyProperty property)
        : this(propertySource, new PropertyPath(property))
    { }

    /// <summary>
    /// Initializes a new instance of the PropertyChangeNotifier.
    /// </summary>
    /// <param name="propertySource">The control containing the property.</param>
    /// <param name="property">The property to track.</param>
    public PropertyChangeNotifier(DependencyObject propertySource, PropertyPath property)
    {
        propertySource.CheckNotNull(nameof(propertySource));
        property.CheckNotNull(nameof(property));

        _propertySource = new WeakReference(propertySource);
        var binding = new Binding
        {
            Path = property,
            Mode = BindingMode.OneWay,
            Source = propertySource
        };
        BindingOperations.SetBinding(this, ValueProperty, binding);
    }

    /// <summary>
    /// Gets the property being tracked.
    /// </summary>
    public DependencyObject? PropertySource
    {
        get
        {
            try
            {
                // note, it is possible that accessing the target property
                // will result in an exception so i’ve wrapped this check
                // in a try catch
                return _propertySource.IsAlive
                    ? _propertySource.Target as DependencyObject
                    : null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Identifies the <see cref="Value"/> dependency property
    /// </summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value",
        typeof(object), typeof(PropertyChangeNotifier), new FrameworkPropertyMetadata(null, OnPropertyChanged));

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var notifier = (PropertyChangeNotifier)d.CheckNotNull(nameof(d));
        notifier.ValueChanged?.Invoke(notifier.PropertySource, EventArgs.Empty);
    }

    /// <summary>
    /// Returns/sets the value of the property
    /// </summary>
    /// <seealso cref="ValueProperty"/>
    [Description("Returns / sets the value of the property")]
    [Category("Behavior")]
    [Bindable(true)]
    [SuppressMessage("Naming", "CA1721:Property names should not match get methods", Justification = "Reviewed")]
    public object Value
    {
        get
        {
            return GetValue(PropertyChangeNotifier.ValueProperty);
        }
        set
        {
            SetValue(PropertyChangeNotifier.ValueProperty, value);
        }
    }

    /// <inheritdoc />
    public event EventHandler? ValueChanged;

    private bool _disposedValue;

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                BindingOperations.ClearBinding(this, ValueProperty);
            }
            _disposedValue = true;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
