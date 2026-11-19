using Bogus;

namespace Simulation.Checkout.Tests.TestData;

public sealed record FakeCustomer(string Name, string Email);

public static class CustomerTestData
{
    public static FakeCustomer Create(int seed = 12345)
    {
        var faker = new Faker("en")
        {
            Random = new Randomizer(seed)
        };

        return new FakeCustomer(
            faker.Name.FullName(),
            faker.Internet.Email());
    }
}