namespace Celeste.Wpf.Controls;

/// <summary>
/// How far a <see cref="Card"/> lifts off the page.
/// </summary>
public enum CardElevation
{
    /// <summary>A bordered surface with no shadow. The default, and the right choice inside dense layouts.</summary>
    Flat,

    /// <summary>A subtle shadow. Use to lift a card above a sunken background.</summary>
    Low,

    /// <summary>A pronounced shadow. Reserve for transient surfaces such as dialogs and popovers.</summary>
    High,
}
