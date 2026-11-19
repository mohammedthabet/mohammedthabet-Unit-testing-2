using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using VerifyXunit;

namespace Simulation.Checkout.Tests;

public sealed class ReceiptSnapshotTests

{
    [Fact]
    public Task CheckoutAsync_WhenExampleC_ShouldMatchReceiptSnapshot()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CustomerName = "Ahmed Mohamed",
            CustomerEmail = "ahmed@example.com",
            Items =
            [
                new OrderItem
                {
                    ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    ProductName = "Keyboard",
                    UnitPrice = 500m,
                    Quantity = 3
                }
            ]
        };

        var repository = new Mock<IOrderRepository>();
        var inventory = new Mock<IInventoryService>();
        var notification = new Mock<INotificationService>();

        inventory
            .Setup(x => x.IsAvailableAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new CheckoutService(
            repository.Object,
            inventory.Object,
            notification.Object);

        // Act
        var receiptTask = sut.CheckoutAsync(order);

        // Assert
        return Verify(receiptTask)
            .ScrubMember("CreatedAt");
    }
}