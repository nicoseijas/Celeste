namespace Celeste.Wpf.Controls;

/// <summary>
/// The diameters an <see cref="Avatar"/> comes in. Setting
/// <see cref="System.Windows.FrameworkElement.Width"/> and
/// <see cref="System.Windows.FrameworkElement.Height"/> overrides all of them.
/// </summary>
public enum AvatarSize
{
    /// <summary>24 device-independent pixels. Fits a table row or a list item.</summary>
    Small,

    /// <summary>32 pixels. The default, and the size a toolbar or a comment expects.</summary>
    Medium,

    /// <summary>44 pixels. For a profile header, where the avatar is the subject.</summary>
    Large,
}
