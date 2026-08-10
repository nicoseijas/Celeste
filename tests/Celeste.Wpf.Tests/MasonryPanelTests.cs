using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Celeste.Wpf.Controls;
using Xunit;

namespace Celeste.Wpf.Tests;

/// <summary>
/// The placement rules the panel owns. Widths here are chosen to divide evenly, so an assertion that
/// fails is a placement bug rather than layout rounding.
/// </summary>
public class MasonryPanelTests
{
    private const double MinColumnWidth = 200;
    private const double ColumnSpacing = 20;
    private const double RowSpacing = 10;

    /// <summary>A column of <see cref="MinColumnWidth"/> plus the gap that precedes it.</summary>
    private const double ColumnPitch = MinColumnWidth + ColumnSpacing;

    [Theory]
    [InlineData(MinColumnWidth, 1)]
    [InlineData(ColumnPitch + MinColumnWidth, 2)]
    [InlineData((ColumnPitch * 2) + MinColumnWidth, 3)]
    [InlineData((ColumnPitch * 3) + MinColumnWidth, 4)]
    public void TheColumnCountFollowsTheWidth(double width, int expectedColumns)
    {
        StaTestHost.Run(() =>
        {
            MasonryPanel panel = Panel(Tiles(100, expectedColumns + 1));

            Realize(panel, width);

            // One tile per column, plus one that has to wrap under the first: the number of distinct
            // horizontal offsets is the number of columns.
            Assert.Equal(expectedColumns, DistinctColumnOffsets(panel));
        });
    }

    [Fact]
    public void AFixedColumnCountIgnoresTheWidth()
    {
        StaTestHost.Run(() =>
        {
            MasonryPanel panel = Panel(Tiles(100, 4));
            panel.Columns = 2;

            Realize(panel, 900);

            Assert.Equal(2, DistinctColumnOffsets(panel));
        });
    }

    /// <summary>
    /// The whole point of the layout: a short tile does not leave the column beside a tall one empty.
    /// </summary>
    [Fact]
    public void EachTileGoesIntoTheColumnThatIsShortest()
    {
        StaTestHost.Run(() =>
        {
            Border tall = Tile(200);
            Border short1 = Tile(40);
            Border short2 = Tile(40);

            MasonryPanel panel = Panel([tall, short1, short2]);
            panel.Columns = 2;

            Realize(panel, ColumnPitch + MinColumnWidth);

            // Third tile lands under the second, not under the first, because column two is 40 tall
            // against column one's 200.
            Assert.Equal(0, Offset(tall).X, 3);
            Assert.Equal(0, Offset(tall).Y, 3);
            Assert.Equal(ColumnPitch, Offset(short1).X, 3);
            Assert.Equal(0, Offset(short1).Y, 3);
            Assert.Equal(ColumnPitch, Offset(short2).X, 3);
            Assert.Equal(40 + RowSpacing, Offset(short2).Y, 3);
        });
    }

    /// <summary>Equal heights keep declaration order, so a uniform grid still reads left to right.</summary>
    [Fact]
    public void TilesOfEqualHeightFillLeftToRight()
    {
        StaTestHost.Run(() =>
        {
            Border[] tiles = Tiles(100, 3);
            MasonryPanel panel = Panel(tiles);

            Realize(panel, (ColumnPitch * 2) + MinColumnWidth);

            Assert.Equal(0, Offset(tiles[0]).X, 3);
            Assert.Equal(ColumnPitch, Offset(tiles[1]).X, 3);
            Assert.Equal(ColumnPitch * 2, Offset(tiles[2]).X, 3);
        });
    }

    [Fact]
    public void TheHeightIsTheTallestColumnWithNoTrailingGap()
    {
        StaTestHost.Run(() =>
        {
            // Column one takes the first tile and stops at 100. Column two is shorter, so it takes
            // both of the others: 60 + 10 + 100. The gap after the last tile is not part of it.
            MasonryPanel panel = Panel([Tile(100), Tile(60), Tile(100)]);
            panel.Columns = 2;

            panel.Measure(new Size(ColumnPitch + MinColumnWidth, double.PositiveInfinity));

            Assert.Equal(170, panel.DesiredSize.Height, 3);
        });
    }

    [Fact]
    public void ACollapsedTileTakesNoSpaceAndLeavesNoGap()
    {
        StaTestHost.Run(() =>
        {
            Border first = Tile(100);
            Border hidden = Tile(100);
            hidden.Visibility = Visibility.Collapsed;
            Border last = Tile(100);

            MasonryPanel panel = Panel([first, hidden, last]);
            panel.Columns = 1;

            Realize(panel, MinColumnWidth);

            // Not 100 + gap + 100 + gap + 100: the collapsed tile is not in the column at all.
            Assert.Equal(100 + RowSpacing, Offset(last).Y, 3);
            Assert.Equal(210, panel.DesiredSize.Height, 3);
        });
    }

    [Fact]
    public void NarrowingThePanelReflowsIt()
    {
        StaTestHost.Run(() =>
        {
            Border[] tiles = Tiles(100, 3);
            MasonryPanel panel = Panel(tiles);

            Border root = Realize(panel, (ColumnPitch * 2) + MinColumnWidth);
            Assert.Equal(3, DistinctColumnOffsets(panel));

            Resize(root, ColumnPitch + MinColumnWidth);

            // Three columns' worth of tiles in two columns: the third one moves under the first.
            Assert.Equal(2, DistinctColumnOffsets(panel));
            Assert.Equal(0, Offset(tiles[2]).X, 3);
            Assert.Equal(100 + RowSpacing, Offset(tiles[2]).Y, 3);
        });
    }

    /// <summary>
    /// With no width to divide there is nothing to derive a column count from, so the panel falls
    /// back to one column of <see cref="MasonryPanel.MinColumnWidth"/> rather than measuring to zero.
    /// </summary>
    [Fact]
    public void AnUnconstrainedWidthFallsBackToOneColumn()
    {
        StaTestHost.Run(() =>
        {
            MasonryPanel panel = Panel(Tiles(100, 2));

            panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(MinColumnWidth, panel.DesiredSize.Width, 3);
            Assert.Equal(210, panel.DesiredSize.Height, 3);
        });
    }

    /// <summary>
    /// A fixed column count the width cannot pay for: the gaps alone exceed it. The panel gives every
    /// column nothing rather than a negative width, which is the difference between a squashed layout
    /// and an exception.
    /// </summary>
    [Fact]
    public void MoreColumnsThanFitDoNotProduceANegativeWidth()
    {
        StaTestHost.Run(() =>
        {
            MasonryPanel panel = Panel(Tiles(50, 3));
            panel.Columns = 10;

            Realize(panel, 100);

            Assert.Equal(100, panel.DesiredSize.Width, 3);
            Assert.All(panel.Children.Cast<UIElement>(), child => Assert.Equal(0, child.RenderSize.Width, 3));
        });
    }

    private static MasonryPanel Panel(IEnumerable<UIElement> children)
    {
        var panel = new MasonryPanel
        {
            MinColumnWidth = MinColumnWidth,
            ColumnSpacing = ColumnSpacing,
            RowSpacing = RowSpacing,
        };

        foreach (UIElement child in children)
        {
            panel.Children.Add(child);
        }

        return panel;
    }

    /// <summary>
    /// A tile with a height and no width: it stretches to the column, so its arranged offset is the
    /// column's own and the assertions do not have to account for centering.
    /// </summary>
    private static Border Tile(double height) => new() { Height = height };

    private static Border[] Tiles(double height, int count) =>
        [.. Enumerable.Range(0, count).Select(_ => Tile(height))];

    private static Border Realize(MasonryPanel panel, double width)
    {
        var root = new Border { Child = panel };

        Resize(root, width);
        return root;
    }

    private static void Resize(Border root, double width)
    {
        root.Width = width;
        root.Measure(new Size(width, double.PositiveInfinity));
        root.Arrange(new Rect(0, 0, width, root.DesiredSize.Height));
        root.UpdateLayout();
    }

    private static Vector Offset(UIElement child) => VisualTreeHelper.GetOffset(child);

    private static int DistinctColumnOffsets(MasonryPanel panel) =>
        panel.Children
            .Cast<UIElement>()
            .Where(child => child.Visibility != Visibility.Collapsed)
            .Select(child => Math.Round(Offset(child).X, 3))
            .Distinct()
            .Count();
}
