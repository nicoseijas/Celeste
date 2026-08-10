using System.Windows;
using System.Windows.Controls;

namespace Celeste.Wpf.Controls;

/// <summary>
/// Lays children out in columns of equal width and unequal height: each one goes into whichever
/// column is currently shortest, so tiles of different heights leave no gaps at the bottom.
/// </summary>
/// <remarks>
/// <para>
/// The number of columns comes from the panel's own width unless <see cref="Columns"/> is set: the
/// panel fits as many <see cref="MinColumnWidth"/> columns as it can and shares the remaining space
/// between them, so a window resize reflows instead of clipping.
/// </para>
/// <para>
/// <b>Shortest-column placement changes reading order.</b> Child three can end up beside child one
/// rather than under it. That is the point of the layout, and it is the reason not to use it for
/// content that has to be read in sequence.
/// </para>
/// <para>
/// Children are measured with an unconstrained height, so each one has to be able to decide its own:
/// an <see cref="ImageView"/> does that from its aspect ratio, and a <see cref="Card"/> from its
/// content. A child that stretches to fill instead measures to nothing and disappears.
/// </para>
/// <para>
/// Nothing here virtualizes. Every child is measured and arranged on every pass, which is fine for
/// the tens of tiles a gallery view shows and wrong for thousands.
/// </para>
/// </remarks>
/// <example>
/// <code language="xml">
/// &lt;celeste:MasonryPanel MinColumnWidth="220" ColumnSpacing="12" RowSpacing="12"&gt;
///     &lt;celeste:ImageView Source="one.jpg" /&gt;
///     &lt;celeste:ImageView Source="two.jpg" /&gt;
/// &lt;/celeste:MasonryPanel&gt;
/// </code>
/// </example>
public class MasonryPanel : Panel
{
    /// <summary>Identifies the <see cref="Columns"/> dependency property.</summary>
    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.Register(
            nameof(Columns),
            typeof(int),
            typeof(MasonryPanel),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure),
            value => (int)value >= 0);

    /// <summary>Identifies the <see cref="MinColumnWidth"/> dependency property.</summary>
    public static readonly DependencyProperty MinColumnWidthProperty =
        DependencyProperty.Register(
            nameof(MinColumnWidth),
            typeof(double),
            typeof(MasonryPanel),
            new FrameworkPropertyMetadata(DefaultMinColumnWidth, FrameworkPropertyMetadataOptions.AffectsMeasure),
            IsPositiveAndFinite);

    /// <summary>Identifies the <see cref="ColumnSpacing"/> dependency property.</summary>
    public static readonly DependencyProperty ColumnSpacingProperty =
        DependencyProperty.Register(
            nameof(ColumnSpacing),
            typeof(double),
            typeof(MasonryPanel),
            new FrameworkPropertyMetadata(DefaultSpacing, FrameworkPropertyMetadataOptions.AffectsMeasure),
            IsNonNegativeAndFinite);

    /// <summary>Identifies the <see cref="RowSpacing"/> dependency property.</summary>
    public static readonly DependencyProperty RowSpacingProperty =
        DependencyProperty.Register(
            nameof(RowSpacing),
            typeof(double),
            typeof(MasonryPanel),
            new FrameworkPropertyMetadata(DefaultSpacing, FrameworkPropertyMetadataOptions.AffectsMeasure),
            IsNonNegativeAndFinite);

    private const double DefaultMinColumnWidth = 220d;

    /// <summary>Matches <c>Celeste.Space.Md</c>. A panel is not templated, so it cannot read tokens.</summary>
    private const double DefaultSpacing = 12d;

    /// <summary>
    /// Gets or sets a fixed number of columns, or 0 — the default — to derive it from the panel's
    /// width and <see cref="MinColumnWidth"/>.
    /// </summary>
    public int Columns
    {
        get => (int)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>
    /// Gets or sets the narrowest a column may get before the panel drops one. Ignored while
    /// <see cref="Columns"/> is set. Defaults to 220.
    /// </summary>
    public double MinColumnWidth
    {
        get => (double)GetValue(MinColumnWidthProperty);
        set => SetValue(MinColumnWidthProperty, value);
    }

    /// <summary>Gets or sets the gap between columns. Defaults to 12.</summary>
    public double ColumnSpacing
    {
        get => (double)GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    /// <summary>Gets or sets the gap between tiles within a column. Defaults to 12.</summary>
    public double RowSpacing
    {
        get => (double)GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        int columns = ColumnCount(availableSize.Width);
        double columnWidth = ColumnWidth(availableSize.Width, columns);
        double[] filled = MeasureIntoColumns(columns, columnWidth);

        double width = double.IsInfinity(availableSize.Width) || availableSize.Width <= 0
            ? (columnWidth * columns) + (ColumnSpacing * (columns - 1))
            : availableSize.Width;

        return new Size(width, Tallest(filled));
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        int columns = ColumnCount(finalSize.Width);
        double columnWidth = ColumnWidth(finalSize.Width, columns);
        double[] filled = new double[columns];

        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                child.Arrange(new Rect(0, 0, 0, 0));
                continue;
            }

            // The final width can differ from the one measured against — a scroll bar appearing is
            // the usual reason — and the heights the placement uses have to match the width the
            // tiles actually get. Re-measuring at an unchanged constraint costs nothing.
            child.Measure(new Size(columnWidth, double.PositiveInfinity));

            int column = ShortestColumn(filled);
            double top = filled[column];
            double height = child.DesiredSize.Height;

            child.Arrange(new Rect(
                column * (columnWidth + ColumnSpacing),
                top,
                columnWidth,
                height));

            filled[column] = top + height + RowSpacing;
        }

        return finalSize;
    }

    private static bool IsPositiveAndFinite(object value)
    {
        double candidate = (double)value;
        return candidate > 0 && !double.IsInfinity(candidate);
    }

    private static bool IsNonNegativeAndFinite(object value)
    {
        double candidate = (double)value;
        return candidate >= 0 && !double.IsNaN(candidate) && !double.IsInfinity(candidate);
    }

    /// <summary>Leftmost of the shortest columns, so tiles of equal height stay in reading order.</summary>
    private static int ShortestColumn(double[] filled)
    {
        int shortest = 0;

        for (int column = 1; column < filled.Length; column++)
        {
            if (filled[column] < filled[shortest])
            {
                shortest = column;
            }
        }

        return shortest;
    }

    /// <summary>
    /// The height the panel needs. Every column carries a trailing <see cref="RowSpacing"/> from its
    /// last tile, which is not part of the content.
    /// </summary>
    private double Tallest(double[] filled)
    {
        double tallest = 0;

        foreach (double height in filled)
        {
            if (height > tallest)
            {
                tallest = height;
            }
        }

        return tallest > 0 ? tallest - RowSpacing : 0;
    }

    private double[] MeasureIntoColumns(int columns, double columnWidth)
    {
        double[] filled = new double[columns];
        var constraint = new Size(columnWidth, double.PositiveInfinity);

        foreach (UIElement child in InternalChildren)
        {
            child.Measure(constraint);

            // A collapsed child is measured — WPF expects every child to be — but takes no column
            // and leaves no gap behind it.
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            int column = ShortestColumn(filled);
            filled[column] += child.DesiredSize.Height + RowSpacing;
        }

        return filled;
    }

    private int ColumnCount(double width)
    {
        int requested = Columns;
        if (requested > 0)
        {
            return requested;
        }

        if (double.IsInfinity(width) || width <= 0)
        {
            return 1;
        }

        // The gaps sit between columns, so the last column needs no spacing of its own.
        double spacing = ColumnSpacing;
        int fits = (int)Math.Floor((width + spacing) / (MinColumnWidth + spacing));
        return Math.Max(1, fits);
    }

    private double ColumnWidth(double width, int columns)
    {
        if (double.IsInfinity(width) || width <= 0)
        {
            return MinColumnWidth;
        }

        double content = width - (ColumnSpacing * (columns - 1));
        return content > 0 ? content / columns : 0;
    }
}
