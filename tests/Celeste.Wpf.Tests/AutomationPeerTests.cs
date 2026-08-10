using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Celeste.Wpf.Controls;
using Xunit;

namespace Celeste.Wpf.Tests;

/// <summary>
/// Every custom control has to tell assistive technology what it is. A lookless control with no peer
/// of its own is reported as an unnamed custom element, which is indistinguishable from a decorative
/// rectangle — so this asserts a real control type for each of them, and a name where the control
/// has one to give.
/// </summary>
public class AutomationPeerTests
{
    public static TheoryData<string, AutomationControlType> ControlTypes => new()
    {
        { nameof(Card), AutomationControlType.Group },
        { nameof(Badge), AutomationControlType.Text },
        { nameof(ProgressRing), AutomationControlType.ProgressBar },
        { nameof(ImageView), AutomationControlType.Image },
        { nameof(Avatar), AutomationControlType.Image },
        { nameof(NavigationHost), AutomationControlType.Group },
        { nameof(SidebarHost), AutomationControlType.Group },
        { nameof(Sidebar), AutomationControlType.List },
        { nameof(ToggleSwitch), AutomationControlType.Button },
    };

    [Theory]
    [MemberData(nameof(ControlTypes))]
    public void EveryCustomControlReportsWhatItIs(string controlName, AutomationControlType expected)
    {
        StaTestHost.Run(() =>
        {
            AutomationPeer peer = PeerFor(Create(controlName));

            Assert.Equal(expected, peer.GetAutomationControlType());
            Assert.Equal(controlName, peer.GetClassName());
        });
    }

    /// <summary>
    /// A card's header and a badge's content are already on screen as text; an avatar's initials are
    /// the only thing it shows. Each is the best name available when the application has not set one.
    /// </summary>
    [Theory]
    [InlineData(nameof(Card), "Billing")]
    [InlineData(nameof(Badge), "New")]
    [InlineData(nameof(Avatar), "NS")]
    public void AControlWithSomethingToSayFallsBackToIt(string controlName, string expected)
    {
        StaTestHost.Run(() => Assert.Equal(expected, PeerFor(Create(controlName)).GetName()));
    }

    /// <summary>
    /// What the application sets always wins: it knows what the control means on its own screen, and
    /// for a picture it is the only source of an accessible name there is.
    /// </summary>
    [Fact]
    public void AnApplicationSuppliedNameWinsOverTheFallback()
    {
        StaTestHost.Run(() =>
        {
            var avatar = new Avatar { Initials = "NS" };
            AutomationProperties.SetName(avatar, "Nicolás Seijas");

            Assert.Equal("Nicolás Seijas", PeerFor(avatar).GetName());
        });
    }

    /// <summary>
    /// A decorative picture is better unnamed than named after its file: a screen reader announcing
    /// "tile-1.png" is noise, and an empty name is the signal that alternative text is missing.
    /// </summary>
    [Fact]
    public void APictureIsNotNamedAfterItsSource()
    {
        StaTestHost.Run(() =>
        {
            var view = new ImageView { Source = new Uri("https://example.invalid/cover.jpg") };

            Assert.Empty(PeerFor(view).GetName());
        });
    }

    private static AutomationPeer PeerFor(FrameworkElement element)
    {
        // Realized first: a peer reads properties off a control that has been through layout.
        var root = new Border { Child = element };
        root.Measure(new Size(400, 300));
        root.Arrange(new Rect(0, 0, 400, 300));
        root.UpdateLayout();

        AutomationPeer? peer = UIElementAutomationPeer.CreatePeerForElement(element);

        Assert.NotNull(peer);
        return peer;
    }

    private static FrameworkElement Create(string controlName) => controlName switch
    {
        nameof(Card) => new Card { Header = "Billing", Content = "body" },
        nameof(Badge) => new Badge { Content = "New" },
        nameof(ProgressRing) => new ProgressRing(),
        nameof(ImageView) => new ImageView { AspectRatio = 1.5 },
        nameof(Avatar) => new Avatar { Initials = "NS" },
        nameof(NavigationHost) => new NavigationHost { Content = "body" },
        nameof(SidebarHost) => new SidebarHost { Pane = new Sidebar(), Content = "body" },
        nameof(Sidebar) => new Sidebar { Items = { new SidebarItem { Content = "Overview" } } },
        nameof(ToggleSwitch) => new ToggleSwitch { Content = "Public" },
        _ => throw new ArgumentOutOfRangeException(nameof(controlName), controlName, "Unknown control."),
    };
}
