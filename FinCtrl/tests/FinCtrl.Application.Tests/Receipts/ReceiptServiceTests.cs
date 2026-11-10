using FinCtrl.Application.Payments;
using FinCtrl.Application.Receipts;
using FinCtrl.Common.Application;
using FinCtrl.Domain;
using FinCtrl.Domain.Receipts;
using NSubstitute;

namespace FinCtrl.Application.Tests.Receipts;

public class ReceiptServiceTests
{
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IReceiptItemRepository _items = Substitute.For<IReceiptItemRepository>();
    private readonly ReceiptService _service;

    public ReceiptServiceTests()
    {
        _service = new ReceiptService(_payments, _items);
    }

    [Fact]
    public async Task GetByPaymentId_UnknownOrDeletedPayment_ThrowsNotFoundException()
    {
        _payments.Get(5).Returns((Payment?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByPaymentId(5));
        await _items.DidNotReceive().GetByPaymentId(Arg.Any<int>());
    }

    [Fact]
    public async Task GetByPaymentId_NoItems_ReturnsEmptyReceiptWithZeroSum()
    {
        _payments.Get(1).Returns(new Payment { PaymentId = 1 });
        _items.GetByPaymentId(1).Returns([]);

        var receipt = await _service.GetByPaymentId(1);

        Assert.Equal(1, receipt.PaymentId);
        Assert.Empty(receipt.Items);
        Assert.Equal(0m, receipt.TotalSum);
    }

    [Fact]
    public async Task GetByPaymentId_WithItems_KeepsRepositoryOrderAndSumsTotalPrice()
    {
        _payments.Get(1).Returns(new Payment { PaymentId = 1 });
        _items.GetByPaymentId(1).Returns(
        [
            new ReceiptItem { ReceiptItemId = 3, TotalPrice = 10.50m },
            new ReceiptItem { ReceiptItemId = 7, TotalPrice = 4.25m }
        ]);

        var receipt = await _service.GetByPaymentId(1);

        Assert.Equal([3, 7], receipt.Items.Select(i => i.ReceiptItemId));
        Assert.Equal(14.75m, receipt.TotalSum);
    }
}
