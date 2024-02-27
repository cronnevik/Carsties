using AuctionService.Entities;

namespace AuctionService.UnitTests;

public class AuctionEntityTests
{
    [Fact]
    public void HasReservedPrice_ReservedPriceGtZero_True()
    {
        // arrange (create the test obj)
        var auction = new Auction{Id = Guid.NewGuid(), ReservePrice = 10};

        // act (execute the method under test)
        var result = auction.HasReservedPrice();

        // assert
        Assert.True(result);
    }

    [Fact]
    public void HasReservedPrice_ReservedPriceIsZero_False()
    {
        var auction = new Auction{Id = Guid.NewGuid(), ReservePrice = 0};

        var result = auction.HasReservedPrice();

        Assert.False(result);
    }
}