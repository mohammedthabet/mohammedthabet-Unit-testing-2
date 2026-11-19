using AutoFixture;
using Simulation.Checkout.Domain;

namespace Simulation.Checkout.Tests.TestData;

public static class OrderTestData
{
    public static Order CreateValidOrder()
    {
        var fixture = new Fixture();

        var item = fixture.Build<OrderItem>()
            .With(x => x.UnitPrice, 10m)
            .With(x => x.Quantity, 2)
            .Create();

        return fixture.Build<Order>()
            .With(x => x.Items, new[] { item })
            .Create();
    }
}