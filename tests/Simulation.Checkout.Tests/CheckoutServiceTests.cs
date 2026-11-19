using FluentAssertions;
using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Domain.Exceptions;

namespace Simulation.Checkout.Tests;

public sealed class CheckoutServiceTests
{
    // TASK 1.1 — Empty Order

    [Fact]
    public async Task CheckoutAsync_WhenOrderIsEmpty_ShouldThrowEmptyOrderException()
    {
        var (sut, _, _, _) = CreateSut();
        var order = CreateOrder();

        Func<Task> act = () => sut.CheckoutAsync(order);

        await act.Should().ThrowAsync<EmptyOrderException>();
    }

    // TASK 1.2 — Invalid Quantity

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CheckoutAsync_WhenQuantityIsNotPositive_ShouldThrowInvalidQuantityException(
        int quantity)
    {
        const string productName = "Keyboard";

        var (sut, _, _, _) = CreateSut();
        var order = CreateOrder(CreateItem(500m, quantity, productName));

        Func<Task> act = () => sut.CheckoutAsync(order);

        var exception = await act.Should()
            .ThrowAsync<InvalidQuantityException>();

        exception.Which.Message.Should().Contain(productName);
    }

    // TASK 1.3 — Negative Price

    [Fact]
    public async Task CheckoutAsync_WhenPriceIsNegative_ShouldThrowInvalidPriceException()
    {
        var (sut, _, _, _) = CreateSut();
        var order = CreateOrder(CreateItem(-0.01m, 1));

        Func<Task> act = () => sut.CheckoutAsync(order);

        await act.Should().ThrowAsync<InvalidPriceException>();
    }

    // TASK 1.4 — Zero Price

    [Fact]
    public async Task CheckoutAsync_WhenPriceIsZero_ShouldSucceed()
    {
        var (sut, _, _, _) = CreateSut();
        var order = CreateOrder(CreateItem(0m, 1));

        var receipt = await sut.CheckoutAsync(order);

        receipt.Subtotal.Should().Be(0m);
        receipt.Discount.Should().Be(0m);
        receipt.Total.Should().Be(0m);
    }

    // TASK 1.5 — Discount Boundaries

    public static TheoryData<int, int> DiscountCases => new()
    {
        { 99999, 0 },
        { 100000, 10000 },
        { 150000, 10000 }
    };

    [Theory]
    [MemberData(nameof(DiscountCases))]
    public void CalculateDiscount_WhenSubtotalChanges_ShouldApplyCorrectDiscount(
        int subtotalInCents,
        int expectedDiscountInCents)
    {
        var subtotal = subtotalInCents / 100m;
        var expectedDiscount = expectedDiscountInCents / 100m;

        var discount = CheckoutService.CalculateDiscount(subtotal);

        discount.Should().Be(expectedDiscount);
    }

    // TASK 1.6 — Examples A and C

    public static TheoryData<Order, decimal, decimal, decimal> CheckoutExamples => new()
    {
        {
            CreateOrder(
                CreateItem(150m, 2),
                CreateItem(400m, 1)),
            700m, 0m, 700m
        },
        {
            CreateOrder(CreateItem(500m, 3)),
            1500m, 100m, 1400m
        }
    };

    [Theory]
    [MemberData(nameof(CheckoutExamples))]
    public async Task CheckoutAsync_WhenProcessingExamples_ShouldCalculateCorrectTotals(
        Order order,
        decimal expectedSubtotal,
        decimal expectedDiscount,
        decimal expectedTotal)
    {
        var (sut, _, _, _) = CreateSut();

        var receipt = await sut.CheckoutAsync(order);

        receipt.Subtotal.Should().Be(expectedSubtotal);
        receipt.Discount.Should().Be(expectedDiscount);
        receipt.Total.Should().Be(expectedTotal);
    }

    // TASK 2.1 — Successful Checkout With Mocks

    [Fact]
    public async Task CheckoutAsync_WhenOrderIsValid_ShouldSaveAndSendConfirmationOnce()
    {
        var (sut, repository, _, notification) = CreateSut();
        var order = CreateOrder(CreateItem(500m, 2));

        var receipt = await sut.CheckoutAsync(order);

        receipt.Total.Should().Be(900m);

        repository.Verify(
            x => x.SaveAsync(
                It.Is<Order>(saved => saved.Id == order.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);

        notification.Verify(
            x => x.SendOrderConfirmationAsync(
                It.Is<CheckoutReceipt>(r => r.OrderId == order.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // TASK 2.2 — Insufficient Inventory

    [Fact]
    public async Task CheckoutAsync_WhenItemIsOutOfStock_ShouldNotSaveOrNotify()
    {
        var (sut, repository, inventory, notification) = CreateSut();
        var order = CreateOrder(CreateItem(500m, 1));

        inventory
            .Setup(x => x.IsAvailableAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Func<Task> act = () => sut.CheckoutAsync(order);

        await act.Should()
            .ThrowAsync<InsufficientInventoryException>();

        repository.Verify(
            x => x.SaveAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        notification.Verify(
            x => x.SendOrderConfirmationAsync(
                It.IsAny<CheckoutReceipt>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // TASK 2.3 — Validation Failure

    [Fact]
    public async Task CheckoutAsync_WhenValidationFails_ShouldNotCallDependencies()
    {
        var (sut, repository, inventory, notification) = CreateSut();
        var order = CreateOrder(CreateItem(500m, 0));

        Func<Task> act = () => sut.CheckoutAsync(order);

        await act.Should().ThrowAsync<InvalidQuantityException>();

        inventory.Verify(
            x => x.IsAvailableAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        repository.Verify(
            x => x.SaveAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        notification.Verify(
            x => x.SendOrderConfirmationAsync(
                It.IsAny<CheckoutReceipt>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Shared Test Helpers

    private static Order CreateOrder(params OrderItem[] items)
    {
        return new Order
        {
            CustomerName = "Ahmed",
            CustomerEmail = "ahmed@example.com",
            Items = items
        };
    }

    private static OrderItem CreateItem(
        decimal price,
        int quantity,
        string productName = "Keyboard")
    {
        return new OrderItem
        {
            ProductId = Guid.NewGuid(),
            ProductName = productName,
            UnitPrice = price,
            Quantity = quantity
        };
    }

    private static (
        CheckoutService Sut,
        Mock<IOrderRepository> Repository,
        Mock<IInventoryService> Inventory,
        Mock<INotificationService> Notification
    ) CreateSut()
    {
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

        return (sut, repository, inventory, notification);
    }
}
