using System.Windows;

namespace Celeste.Wpf.Controls;

/// <summary>
/// Attached properties that let a consumer tweak a Celeste-styled control without replacing its
/// template — corner radius, placeholder text, and the like.
/// </summary>
public static class ControlHelper
{
    /// <summary>Identifies the <c>CornerRadius</c> attached property.</summary>
    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.RegisterAttached(
            "CornerRadius",
            typeof(CornerRadius),
            typeof(ControlHelper),
            new FrameworkPropertyMetadata(new CornerRadius(6d), FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <c>PlaceholderText</c> attached property.</summary>
    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.RegisterAttached(
            "PlaceholderText",
            typeof(string),
            typeof(ControlHelper),
            new FrameworkPropertyMetadata(string.Empty));

    /// <summary>Identifies the <c>IconContent</c> attached property.</summary>
    public static readonly DependencyProperty IconContentProperty =
        DependencyProperty.RegisterAttached(
            "IconContent",
            typeof(object),
            typeof(ControlHelper),
            new FrameworkPropertyMetadata(null));

    /// <summary>Gets the corner radius applied by a Celeste control template.</summary>
    public static CornerRadius GetCornerRadius(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (CornerRadius)element.GetValue(CornerRadiusProperty);
    }

    /// <summary>Sets the corner radius applied by a Celeste control template.</summary>
    public static void SetCornerRadius(DependencyObject element, CornerRadius value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(CornerRadiusProperty, value);
    }

    /// <summary>Gets the hint shown while an input control is empty.</summary>
    public static string GetPlaceholderText(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (string)element.GetValue(PlaceholderTextProperty) ?? string.Empty;
    }

    /// <summary>Sets the hint shown while an input control is empty.</summary>
    public static void SetPlaceholderText(DependencyObject element, string value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>Gets the leading visual rendered before a control's content.</summary>
    public static object? GetIconContent(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(IconContentProperty);
    }

    /// <summary>Sets the leading visual rendered before a control's content.</summary>
    public static void SetIconContent(DependencyObject element, object? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(IconContentProperty, value);
    }
}
