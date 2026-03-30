using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OnyxArchiver.UI.Components;

/// <summary>
/// A specialized button designed for application toolbars. 
/// Features granular control over icon sizing and coloring to maintain visual consistency 
/// across the primary action bar.
/// </summary>
public partial class ToolbarButton : Button
{
    public ToolbarButton()
    {
        InitializeComponent();
    }

    #region Content Properties

    /// <summary>
    /// The label text for the toolbar item (often hidden or shown as a tooltip in compact modes).
    /// </summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(ToolbarButton), new PropertyMetadata("Item"));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// The primary icon for the action. Supports both bitmap and vector (DrawingImage) sources.
    /// </summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(ImageSource), typeof(ToolbarButton), new PropertyMetadata(null));

    public ImageSource Icon
    {
        get => (ImageSource)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    #endregion

    #region Visual Configuration

    /// <summary>
    /// Defines the color of the icon. 
    /// Best used when the Icon is a Geometry/Path with a TemplateBinding to this Brush.
    /// </summary>
    public static readonly DependencyProperty IconColorProperty =
        DependencyProperty.Register(nameof(IconColor), typeof(Brush), typeof(ToolbarButton), new PropertyMetadata(null));

    public Brush IconColor
    {
        get => (Brush)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    /// <summary>
    /// Uniform size (Width/Height) for the icon within the button layout.
    /// </summary>
    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(ToolbarButton), new PropertyMetadata(24.0));

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    /// <summary>
    /// Roundness of the button's background border.
    /// </summary>
    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(ToolbarButton), new PropertyMetadata(new CornerRadius(4)));

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    #endregion
}