using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    public static TheoryData<string> ControlNames => new()
    {
        nameof(Button),
        nameof(ToggleButton),
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
            Color primary = BackgroundOf("Celeste.Button.Primary");
            Color destructive = BackgroundOf("Celeste.Button.Destructive");

            Assert.NotEqual(primary, destructive);
        });

        static Color BackgroundOf(string styleKey)
        {
            var button = new Button
            {
                Content = "x",
                Style = (Style)Application.Current.FindResource(styleKey),
            };

            AssertTemplateApplies(button);
            return ((SolidColorBrush)button.Background).Color;
        }
    }

    private static FrameworkElement Create(string controlName) => controlName switch
    {
        nameof(Button) => new Button { Content = "Label" },
        nameof(ToggleButton) => new ToggleButton { Content = "Label" },
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
