using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Celeste.Wpf.Controls;

/// <summary>
/// An indeterminate spinner for work whose duration is unknown.
/// </summary>
/// <remarks>
/// The animation only runs while <see cref="IsActive"/> is <see langword="true"/>, so an idle ring
/// costs nothing. Use <see cref="ProgressBar"/> instead when you can report real progress.
/// </remarks>
public class ProgressRing : Control
{
    /// <summary>Identifies the <see cref="IsActive"/> dependency property.</summary>
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(ProgressRing),
            new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="StrokeThickness"/> dependency property.</summary>
    public static readonly DependencyProperty StrokeThicknessProperty =
        DependencyProperty.Register(
            nameof(StrokeThickness),
            typeof(double),
            typeof(ProgressRing),
            new PropertyMetadata(3d));

    static ProgressRing() =>
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ProgressRing),
            new FrameworkPropertyMetadata(typeof(ProgressRing)));

    /// <summary>Gets or sets whether the ring is spinning. Defaults to <see langword="true"/>.</summary>
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>Gets or sets the width of the ring's stroke, in device-independent pixels.</summary>
    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    /// <inheritdoc />
    /// <remarks>
    /// A progress bar, because that is what it is: work in progress whose amount is unknown. Give it
    /// an <c>AutomationProperties.Name</c> saying what is being waited for — "spinner" tells a screen
    /// reader user nothing they can act on.
    /// </remarks>
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new CelesteAutomationPeer(this, AutomationControlType.ProgressBar);
}
