
using FluentAssertions;
using NetArchTest.Rules;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Infrastructure;

namespace Simulation.Checkout.Tests;

public sealed class ArchitectureTests
{
    // TASK 5.1 — Domain Dependencies

    [Fact]
    public void Domain_ShouldNotDependOnApplicationOrInfrastructure()
    {
        var domainTypes = Types.InAssembly(typeof(Order).Assembly);

        var applicationResult = domainTypes
            .That()
            .ResideInNamespaceMatching(
                "^Simulation\\.Checkout\\.Domain(\\.Exceptions)?$")
            .ShouldNot()
            .HaveDependencyOn("Simulation.Checkout.Application")
            .GetResult();

        var infrastructureResult = domainTypes
            .That()
            .ResideInNamespaceMatching(
                "^Simulation\\.Checkout\\.Domain(\\.Exceptions)?$")
            .ShouldNot()
            .HaveDependencyOn("Simulation.Checkout.Infrastructure")
            .GetResult();

        applicationResult.IsSuccessful.Should().BeTrue();
        infrastructureResult.IsSuccessful.Should().BeTrue();
    }

    // TASK 5.2 — Application Dependencies

    [Fact]
    public void Application_ShouldNotDependOnInfrastructure()
    {
        var result = Types.InAssembly(typeof(IOrderRepository).Assembly)
            .That()
            .ResideInNamespaceMatching(
                "^Simulation\\.Checkout\\.Application(\\.Abstractions)?$")
            .ShouldNot()
            .HaveDependencyOn("Simulation.Checkout.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    // TASK 5.3 — Interface Naming Convention

    [Fact]
    public void Interfaces_ShouldStartWithI()
    {
        var domainResult = Types.InAssembly(typeof(Order).Assembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        var applicationResult = Types.InAssembly(typeof(IOrderRepository).Assembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        var infrastructureResult = Types.InAssembly(typeof(InMemoryOrderRepository).Assembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        domainResult.IsSuccessful.Should().BeTrue();
        applicationResult.IsSuccessful.Should().BeTrue();
        infrastructureResult.IsSuccessful.Should().BeTrue();
    }

    // TASK 5.4 — Repository Naming Convention

    [Fact]
    public void OrderRepositoryImplementations_ShouldEndWithRepository()
    {
        var result = Types.InAssembly(typeof(InMemoryOrderRepository).Assembly)
            .That()
            .ImplementInterface(typeof(IOrderRepository))
            .Should()
            .HaveNameEndingWith("Repository")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
