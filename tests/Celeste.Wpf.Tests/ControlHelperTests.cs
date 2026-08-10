using System.Windows;
using System.Windows.Controls;
using Celeste.Wpf.Controls;
using Xunit;

namespace Celeste.Wpf.Tests;

/// <summary>
/// The attached properties are the supported way to adjust a Celeste control without replacing its
/// template, which makes their accessors public API worth pinning down.
/// </summary>
public class ControlHelperTests
{
    [Fact]
    public void ACornerRadiusRoundTrips()
    {
        StaTestHost.Run(() =>
        {
            var target = new Button();
            var radius = new CornerRadius(3, 4, 5, 6);

            ControlHelper.SetCornerRadius(target, radius);

            Assert.Equal(radius, ControlHelper.GetCornerRadius(target));
        });
    }

    [Fact]
    public void PlaceholderTextRoundTripsAndIsNeverNull()
    {
        StaTestHost.Run(() =>
        {
            var target = new TextBox();

            // Empty rather than null, so a template can bind to its length without a converter.
            Assert.Equal(string.Empty, ControlHelper.GetPlaceholderText(target));

            ControlHelper.SetPlaceholderText(target, "e.g. Northwind");
            Assert.Equal("e.g. Northwind", ControlHelper.GetPlaceholderText(target));
        });
    }

    [Fact]
    public void IconContentRoundTrips()
    {
        StaTestHost.Run(() =>
        {
            var target = new Button();
            var icon = new Border();

            Assert.Null(ControlHelper.GetIconContent(target));

            ControlHelper.SetIconContent(target, icon);
            Assert.Same(icon, ControlHelper.GetIconContent(target));
        });
    }

    /// <summary>
    /// Attached-property accessors are called with whatever the caller has, including nothing. Saying
    /// which argument was null beats a NullReferenceException from inside GetValue.
    /// </summary>
    [Fact]
    public void EveryAccessorRejectsANullElement()
    {
        Assert.Throws<ArgumentNullException>(() => ControlHelper.GetCornerRadius(null!));
        Assert.Throws<ArgumentNullException>(() => ControlHelper.SetCornerRadius(null!, default));
        Assert.Throws<ArgumentNullException>(() => ControlHelper.GetPlaceholderText(null!));
        Assert.Throws<ArgumentNullException>(() => ControlHelper.SetPlaceholderText(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => ControlHelper.GetIconContent(null!));
        Assert.Throws<ArgumentNullException>(() => ControlHelper.SetIconContent(null!, "x"));
    }
}
