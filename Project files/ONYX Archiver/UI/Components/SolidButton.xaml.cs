using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OnyxArchiver.UI.Components;

/// <summary>
/// A custom styled button supporting vector/raster icons on both sides and configurable corner radius.
/// This control is designed to be used with a custom ControlTemplate in XAML.
/// </summary>
public partial class SolidButton : Button
{
    public SolidButton()
    {
        // Note: InitializeComponent is usually called if there is a corresponding XAML file.
        // If this is a purely code-behind custom control, you might use DefaultStyleKeyProperty instead.
        InitializeComponent();
    }

    /// <summary>
    /// The text content displayed inside the button. 
    /// Separate from the standard 'Content' property to allow complex templating.
    /// </summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(SolidButton), new PropertyMetadata("Button"));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Defines the roundness of the button corners.
    /// </summary>
    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(SolidButton), new PropertyMetadata(new CornerRadius(0)));

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    /// <summary>
    /// Optional icon displayed to the left of the text.
    /// </summary>
    public static readonly DependencyProperty LeftIconProperty =
        DependencyProperty.Register(nameof(LeftIcon), typeof(ImageSource), typeof(SolidButton), new PropertyMetadata(null));

    public ImageSource LeftIcon
    {
        get => (ImageSource)GetValue(LeftIconProperty);
        set => SetValue(LeftIconProperty, value);
    }

    /// <summary>
    /// Optional icon displayed to the right of the text.
    /// </summary>
    public static readonly DependencyProperty RightIconProperty =
        DependencyProperty.Register(nameof(RightIcon), typeof(ImageSource), typeof(SolidButton), new PropertyMetadata(null));

    public ImageSource RightIcon
    {
        get => (ImageSource)GetValue(RightIconProperty);
        set => SetValue(RightIconProperty, value);
    }

    /// <summary>
    /// Allows tinting or coloring the icons, especially useful for Path-based geometries or OpacityMasks.
    /// </summary>
    public static readonly DependencyProperty IconColorProperty =
        DependencyProperty.Register(nameof(IconColor), typeof(Brush), typeof(SolidButton), new PropertyMetadata(null));

    public Brush IconColor
    {
        get => (Brush)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }
}