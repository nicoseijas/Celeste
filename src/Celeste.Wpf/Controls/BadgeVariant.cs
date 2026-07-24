namespace Celeste.Wpf.Controls;

/// <summary>
/// The semantic color of a <see cref="Badge"/>.
/// </summary>
public enum BadgeVariant
{
    /// <summary>Low-emphasis gray. The default.</summary>
    Neutral,

    /// <summary>The brand color. Use for the one badge that matters most on screen.</summary>
    Primary,

    /// <summary>Green. A completed or healthy state.</summary>
    Success,

    /// <summary>Amber. A state that needs attention but is not an error.</summary>
    Warning,

    /// <summary>Red. A failure or a blocking state.</summary>
    Danger,

    /// <summary>Transparent with a visible border. Use where a filled badge would be too loud.</summary>
    Outline,
}
