using System.Windows;
using System.Windows.Automation.Peers;

namespace Celeste.Wpf.Controls;

/// <summary>
/// The automation peer for the Celeste controls whose accessible identity is a control type and a
/// name, and nothing more.
/// </summary>
/// <remarks>
/// <para>
/// A lookless <see cref="System.Windows.Controls.Control"/> with no peer of its own is reported to a
/// screen reader as a bare <see cref="AutomationControlType.Custom"/> element with no name, which is
/// indistinguishable from a decorative rectangle. Every control here has a real answer to "what is
/// this", so this peer gives it one.
/// </para>
/// <para>
/// Internal, and deliberately not one class per control: these differ only in the control type they
/// report and where they look for a name when the application has not set one.
/// <see cref="SidebarAutomationPeer"/> is public and separate because a list has selection semantics
/// to preserve, which is a different kind of problem.
/// </para>
/// </remarks>
internal sealed class CelesteAutomationPeer : FrameworkElementAutomationPeer
{
    private readonly AutomationControlType _controlType;
    private readonly Func<string?>? _nameFallback;

    /// <summary>Initializes a new instance of the <see cref="CelesteAutomationPeer"/> class.</summary>
    /// <param name="owner">The control this peer reports on.</param>
    /// <param name="controlType">What the control is, as far as assistive technology is concerned.</param>
    /// <param name="nameFallback">
    /// Where to find a name when the application has not set <c>AutomationProperties.Name</c>. Leave
    /// it out for a control that has nothing sensible to fall back to: an unnamed image is announced
    /// as an image, which is the correct answer for a decorative one.
    /// </param>
    public CelesteAutomationPeer(
        FrameworkElement owner,
        AutomationControlType controlType,
        Func<string?>? nameFallback = null)
        : base(owner)
    {
        _controlType = controlType;
        _nameFallback = nameFallback;
    }

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => _controlType;

    /// <inheritdoc />
    protected override string GetClassNameCore() => Owner.GetType().Name;

    /// <inheritdoc />
    protected override string GetNameCore()
    {
        // Whatever the application set wins: it knows what this control means in its own screen.
        string name = base.GetNameCore();

        return string.IsNullOrEmpty(name)
            ? _nameFallback?.Invoke() ?? string.Empty
            : name;
    }
}
