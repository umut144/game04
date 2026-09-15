namespace Cardgame.Core.Tests.Board;

using Cardgame.Core.Board;
using Cardgame.Core.Model;
using Xunit;

public sealed class BoardSideTests
{
    [Fact]
    public void ASlotCanBeOccupiedAndVacated()
    {
        var side = new BoardSide();
        var card = new CardInstanceId(3);

        Assert.False(side.IsOccupied(6));
        side.Occupy(6, card);

        Assert.True(side.IsOccupied(6));
        Assert.Equal(card, side.OccupantOf(6));
        Assert.Equal(card, side.UnitSlots[5]);
        Assert.Equal(card, side.Vacate(6));
        Assert.False(side.IsOccupied(6));
    }

    [Fact]
    public void AnOccupiedSlotCannotBeOccupiedAgainAndAnEmptyOneNotVacated()
    {
        var side = new BoardSide();
        side.Occupy(1, new CardInstanceId(1));

        Assert.Throws<InvalidOperationException>(() => side.Occupy(1, new CardInstanceId(2)));
        Assert.Throws<InvalidOperationException>(() => side.Vacate(2));
        Assert.Equal(new CardInstanceId(1), side.OccupantOf(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-1)]
    public void SlotsRunFromOneToSix(int slot)
    {
        var side = new BoardSide();

        Assert.Throws<ArgumentOutOfRangeException>(() => side.IsOccupied(slot));
        Assert.Throws<ArgumentOutOfRangeException>(() => side.Occupy(slot, new CardInstanceId(1)));
    }
}
