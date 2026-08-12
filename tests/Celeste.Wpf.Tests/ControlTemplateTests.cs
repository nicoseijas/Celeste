using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Celeste.Wpf.Controls;
using Celeste.Wpf.Theming;
using Xunit;

namespace Celeste.Wpf.Tests;

/// <summary>
/// Applies every Celeste-styled control's template for real. A malformed template — a missing part,
/// a trigger that targets a name the template does not define, a binding whose path cannot be
/// resolved — throws here rather than in a consumer's window.
/// </summary>
public class ControlTemplateTests
{
    /// <summary>A keyed style rather than a type, so it needs its own row here.</summary>
    private const string SegmentStyleKey = "Celeste.ToggleButton.Segment";

    public static TheoryData<string> ControlNames => new()
    {
        nameof(Button),
        nameof(ToggleButton),
        SegmentStyleKey,
        nameof(TextBox),
        nameof(PasswordBox),
        nameof(CheckBox),
        nameof(RadioButton),
        nameof(ComboBox),
        nameof(ListBox),
        nameof(TabControl),
        nameof(Slider),
        nameof(ProgressBar),
        nameof(ScrollBar),
        nameof(Card),
        nameof(Badge),
        nameof(ToggleSwitch),
        nameof(ProgressRing),
        nameof(ImageView),
        nameof(Avatar),
        nameof(NavigationHost),
        nameof(Sidebar),
        nameof(SidebarHost),
        nameof(SidebarItem),
        nameof(SidebarGroupHeader),
        nameof(SidebarSeparator),
    };

    [Theory]
    [MemberData(nameof(ControlNames))]
    public void TemplateAppliesInLightTheme(string controlName)
    {
        StaTestHost.Run(() => AssertTemplateApplies(Create(controlName)), ApplicationTheme.Light);
    }

    [Theory]
    [MemberData(nameof(ControlNames))]
    public void TemplateAppliesInDarkTheme(string controlName)
    {
        StaTestHost.Run(() => AssertTemplateApplies(Create(controlName)), ApplicationTheme.Dark);
    }

    [Fact]
    public void ComboBoxRendersItsItems()
    {
        StaTestHost.Run(() =>
        {
            var combo = new ComboBox();
            combo.Items.Add(new ComboBoxItem { Content = "first" });
            combo.Items.Add(new ComboBoxItem { Content = "second" });
            combo.SelectedIndex = 0;

            AssertTemplateApplies(combo);
            Assert.Equal(2, combo.Items.Count);
        });
    }

    [Fact]
    public void CardCollapsesTheHeaderAreaWhenNoHeaderOrDescriptionIsSet()
    {
        StaTestHost.Run(() =>
        {
            var withHeader = new Card { Header = "Billing", Content = "body" };
            var bare = new Card { Content = "body" };

            AssertTemplateApplies(withHeader);
            AssertTemplateApplies(bare);

            Assert.True(withHeader.DesiredSize.Height > bare.DesiredSize.Height);
        });
    }

    [Fact]
    public void ButtonVariantsAllResolveToDistinctBackgrounds()
    {
        StaTestHost.Run(() =>
        {
            Color primary = BackgroundOf(Styled("Celeste.Button.Primary"));
            Color destructive = BackgroundOf(Styled("Celeste.Button.Destructive"));

            Assert.NotEqual(primary, destructive);
        });
    }

    /// <summary>
    /// A size modifier changes metrics and nothing else. Basing one on a variant instead of on the
    /// default recolors every button that only asked to be bigger.
    /// </summary>
    [Fact]
    public void SizeStylesChangeTheMetricsAndNotTheVariant()
    {
        StaTestHost.Run(() =>
        {
            var small = Styled("Celeste.Button.Small");
            var medium = new Button { Content = "x" };
            var large = Styled("Celeste.Button.Large");

            Color defaultVariant = BackgroundOf(medium);
            Assert.Equal(defaultVariant, BackgroundOf(small));
            Assert.Equal(defaultVariant, BackgroundOf(large));

            // DesiredSize, not ActualHeight: the test host stretches the button to fill its parent.
            Assert.True(small.DesiredSize.Height < medium.DesiredSize.Height, "Small measured no smaller than the default.");
            Assert.True(medium.DesiredSize.Height < large.DesiredSize.Height, "Large measured no larger than the default.");
        });
    }

    /// <summary>
    /// A condition inside a template's own triggers is evaluated against the templated parent, and a
    /// control has no templated parent of its own. <see cref="RelativeSourceMode.TemplatedParent"/>
    /// there resolves to nothing, so the condition is never true — silently, with no binding error.
    /// </summary>
    [Fact]
    public void NoTemplateTriggerConditionBindsToItsTemplatedParent()
    {
        StaTestHost.Run(() =>
        {
            List<string> offenders = ControlTemplates()
                .SelectMany(
                    template => ConditionBindings(template)
                        .Where(binding => binding.RelativeSource?.Mode == RelativeSourceMode.TemplatedParent)
                        .Select(binding => $"{template.TargetType.Name}.{binding.Path?.Path}"))
                .ToList();

            Assert.True(
                offenders.Count == 0,
                $"Use RelativeSource Self instead: {string.Join(", ", offenders)}");
        });
    }

    private static IEnumerable<ControlTemplate> ControlTemplates() =>
        StylesIn(Application.Current.Resources)
            .SelectMany(style => style.Setters.OfType<Setter>())
            .Select(setter => setter.Value)
            .OfType<ControlTemplate>();

    private static IEnumerable<Style> StylesIn(ResourceDictionary dictionary)
    {
        foreach (ResourceDictionary merged in dictionary.MergedDictionaries)
        {
            foreach (Style style in StylesIn(merged))
            {
                yield return style;
            }
        }

        foreach (object? value in dictionary.Values)
        {
            if (value is Style style)
            {
                yield return style;
            }
        }
    }

    private static IEnumerable<Binding> ConditionBindings(ControlTemplate template)
    {
        foreach (TriggerBase trigger in template.Triggers)
        {
            switch (trigger)
            {
                case DataTrigger data when data.Binding is Binding binding:
                    yield return binding;
                    break;

                case MultiDataTrigger multi:
                    foreach (Condition condition in multi.Conditions)
                    {
                        if (condition.Binding is Binding binding)
                        {
                            yield return binding;
                        }
                    }

                    break;
            }
        }
    }

    private static Button Styled(string styleKey) => new()
    {
        Content = "x",
        Style = (Style)Application.Current.FindResource(styleKey),
    };

    private static Color BackgroundOf(Button button)
    {
        AssertTemplateApplies(button);
        return ((SolidColorBrush)button.Background).Color;
    }

    private static FrameworkElement Create(string controlName) => controlName switch
    {
        nameof(Button) => new Button { Content = "Label" },
        nameof(ToggleButton) => new ToggleButton { Content = "Label" },
        SegmentStyleKey => new RadioButton
        {
            Content = "Day",
            Style = (Style)Application.Current.FindResource(SegmentStyleKey),
        },
        nameof(TextBox) => new TextBox { Text = "value" },
        nameof(PasswordBox) => new PasswordBox(),
        nameof(CheckBox) => new CheckBox { Content = "Label" },
        nameof(RadioButton) => new RadioButton { Content = "Label" },
        nameof(ComboBox) => new ComboBox(),
        nameof(ListBox) => new ListBox(),
        nameof(TabControl) => new TabControl { Items = { new TabItem { Header = "One", Content = "body" } } },
        nameof(Slider) => new Slider { Value = 30 },
        nameof(ProgressBar) => new ProgressBar { Value = 30 },
        nameof(ScrollBar) => new ScrollBar(),
        nameof(Card) => new Card { Header = "Header", Content = "body" },
        nameof(Badge) => new Badge { Content = "New" },
        nameof(ToggleSwitch) => new ToggleSwitch { Content = "Label" },
        nameof(ProgressRing) => new ProgressRing(),

        // An aspect ratio rather than a source: the template has to measure without a picture, which
        // is the state every tile starts in.
        nameof(ImageView) => new ImageView { AspectRatio = 1.5 },
        nameof(Avatar) => new Avatar { Initials = "NS" },
        nameof(NavigationHost) => new NavigationHost { Content = "body" },
        nameof(Sidebar) => new Sidebar
        {
            Header = "Acme",
            Footer = "Settings",
            Items = { new SidebarItem { Content = "Overview" } },
        },
        nameof(SidebarHost) => new SidebarHost
        {
            Pane = new Sidebar { Items = { new SidebarItem { Content = "Overview" } } },
            Content = "body",
        },
        nameof(SidebarItem) => new SidebarItem { Content = "Overview", Badge = "3" },
        nameof(SidebarGroupHeader) => new SidebarGroupHeader { Content = "Workspace" },
        nameof(SidebarSeparator) => new SidebarSeparator(),
        _ => throw new ArgumentOutOfRangeException(nameof(controlName), controlName, "Unknown control."),
    };

    private static void AssertTemplateApplies(FrameworkElement element)
    {
        // A Window gives the element a PresentationSource-free but complete-enough tree for layout.
        var host = new Border { Child = element };
        host.Measure(new Size(600, 400));
        host.Arrange(new Rect(0, 0, 600, 400));
        host.UpdateLayout();

        Assert.True(element.ActualWidth > 0, $"{element.GetType().Name} measured to zero width.");
        Assert.True(element.ActualHeight > 0, $"{element.GetType().Name} measured to zero height.");
        Assert.True(VisualTreeHelper.GetChildrenCount(element) > 0, $"{element.GetType().Name} produced no visual children.");
    }
}
