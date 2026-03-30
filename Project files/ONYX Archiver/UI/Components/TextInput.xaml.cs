using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OnyxArchiver.UI.Components;

/// <summary>
/// A multi-functional text input component supporting icons, placeholders, 
/// and a secure password mode with visibility toggling.
/// </summary>
public partial class TextInput : UserControl
{
    public TextInput()
    {
        InitializeComponent();

        // Triggers the assigned command when the user clicks on the control area.
        // Useful for custom focus handling or MVVM-based interaction.
        this.PreviewMouseLeftButtonDown += (s, e) => {
            if (Command != null && Command.CanExecute(CommandParameter))
                Command.Execute(CommandParameter);
        };
    }

    #region Commands

    /// <summary>
    /// Command to execute on user interaction with the component.
    /// </summary>
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(TextInput), new PropertyMetadata(null));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>
    /// Parameter passed to the Command during execution.
    /// </summary>
    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(TextInput), new PropertyMetadata(null));

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    #endregion

    #region Styling & Content

    /// <summary>
    /// Graphical icon displayed on the left side of the input field.
    /// </summary>
    public static readonly DependencyProperty LeftIconProperty =
        DependencyProperty.Register(nameof(LeftIcon), typeof(ImageSource), typeof(TextInput));

    public ImageSource LeftIcon
    {
        get => (ImageSource)GetValue(LeftIconProperty);
        set => SetValue(LeftIconProperty, value);
    }

    /// <summary>
    /// Hint text displayed when the input is empty.
    /// </summary>
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(TextInput));

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>
    /// The actual text content of the input. Supports two-way binding by default for MVVM.
    /// </summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(TextInput),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    #endregion

    #region Password Logic

    /// <summary>
    /// Determines if the field should behave as a password input (masking characters).
    /// </summary>
    public static readonly DependencyProperty IsPasswordModeProperty =
        DependencyProperty.Register(nameof(IsPasswordMode), typeof(bool), typeof(TextInput), new PropertyMetadata(false));

    public bool IsPasswordMode
    {
        get => (bool)GetValue(IsPasswordModeProperty);
        set => SetValue(IsPasswordModeProperty, value);
    }

    /// <summary>
    /// Toggles between masked characters (PasswordBox) and plain text (TextBox).
    /// </summary>
    public static readonly DependencyProperty IsPasswordVisibleProperty =
        DependencyProperty.Register(nameof(IsPasswordVisible), typeof(bool), typeof(TextInput), new PropertyMetadata(false));

    public bool IsPasswordVisible
    {
        get => (bool)GetValue(IsPasswordVisibleProperty);
        set => SetValue(IsPasswordVisibleProperty, value);
    }

    #endregion

    /// <summary>
    /// Static callback to handle global changes to the Text property.
    /// </summary>
    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextInput control)
        {
            control.UpdatePlaceholder();
        }
    }

    /// <summary>
    /// Updates the visibility of the placeholder based on current text content.
    /// </summary>
    private void UpdatePlaceholder()
    {
        if (placeholder != null)
        {
            placeholder.Visibility = string.IsNullOrEmpty(this.Text) ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Event handler to sync the internal PasswordBox content with the public Text property.
    /// </summary>
    private void passwordInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (this.Text != passwordInput.Password)
        {
            this.Text = passwordInput.Password;
        }
        UpdatePlaceholder();
    }

    /// <summary>
    /// Event handler to sync the internal TextBox content with the public Text property 
    /// and keep PasswordBox in sync when in password mode.
    /// </summary>
    private void textInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (this.Text != textInput.Text)
        {
            this.Text = textInput.Text;
        }

        if (IsPasswordMode && passwordInput.Password != textInput.Text)
        {
            passwordInput.Password = textInput.Text;
        }
        UpdatePlaceholder();
    }

    /// <summary>
    /// Core logic for toggling password visibility. 
    /// Restores focus and cursor position when switching to plain text.
    /// </summary>
    private void togglePasswordVisibility_Click(object sender, RoutedEventArgs e)
    {
        IsPasswordVisible = !IsPasswordVisible;

        if (IsPasswordVisible)
        {
            // Transfer data to TextBox, focus it, and move caret to the end.
            textInput.Text = passwordInput.Password;
            textInput.Focus();
            textInput.SelectionStart = textInput.Text.Length;
        }
        else
        {
            // Transfer data back to PasswordBox and restore focus.
            passwordInput.Password = textInput.Text;
            passwordInput.Focus();
        }
    }
}