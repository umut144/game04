namespace Cardgame.Core.Tests.Board;

using Cardgame.Core.Board;
using Xunit;

public sealed class BoardGeometryTests
{
    [Fact]
    public void ASlotOpposesTheSlotWithTheSameNumber()
    {
        for (int slot = 1; slot <= 6; slot++)
        {
            Assert.Equal(slot, BoardGeometry.OpposingSlot(slot));
        }
    }

    [Fact]
    public void NeighboursAreTheAdjacentSlotsOnTheSameSide()
    {
        Assert.Equal(new[] { 2 }, BoardGeometry.NeighboursOf(1));
        Assert.Equal(new[] { 2, 4 }, BoardGeometry.NeighboursOf(3));
        Assert.Equal(new[] { 5 }, BoardGeometry.NeighboursOf(6));
    }

    [Theory]
    [InlineData(1, TotemPosition.A)]
    [InlineData(2, TotemPosition.A)]
    [InlineData(3, TotemPosition.B)]
    [InlineData(4, TotemPosition.B)]
    [InlineData(5, TotemPosition.C)]
    [InlineData(6, TotemPosition.C)]
    public void EachColumnHasOneTotemPlaceBehindIt(int column, TotemPosition expected)
    {
        Assert.Equal(expected, BoardGeometry.TotemPositionBehind(column));
        var pair = BoardGeometry.ColumnsOf(expected);
        Assert.Contains(column, new[] { pair.FirstColumn, pair.SecondColumn });
    }

    [Fact]
    public void ThereIsNoSlotOrColumnOutsideOneToSix()
    {
        Assert.False(BoardGeometry.IsValidSlot(0));
        Assert.False(BoardGeometry.IsValidSlot(7));
        Assert.Throws<ArgumentOutOfRangeException>(() => BoardGeometry.OpposingSlot(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => BoardGeometry.NeighboursOf(7));
        Assert.Throws<ArgumentOutOfRangeException>(() => BoardGeometry.TotemPositionBehind(7));
    }
}
