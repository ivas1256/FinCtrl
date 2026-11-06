namespace FinCtrl.Domain.Tests;

public class TransferTests
{
    [Theory]
    [InlineData(10, 20)]
    [InlineData(20, 10)]
    public void GetPairPaymentId_OneSide_ReturnsOtherSide(int paymentId, int expectedPairId)
    {
        var transfer = new Transfer { OutPaymentId = 10, InPaymentId = 20 };

        Assert.Equal(expectedPairId, transfer.GetPairPaymentId(paymentId));
    }

    [Fact]
    public void GetPairPaymentId_NoInSide_OutSideGetsNull()
    {
        var transfer = new Transfer { OutPaymentId = 10, InPaymentId = null };

        Assert.Null(transfer.GetPairPaymentId(10));
    }
}
