namespace Customers.WebApp.Tests.Integration;

[CollectionDefinition(nameof(SharedTestCollection))]
public class SharedTestCollection : ICollectionFixture<SharedTestContext>
{
}
