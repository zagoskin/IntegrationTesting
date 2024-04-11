using Microsoft.AspNetCore.Mvc.Testing;

namespace Customers.Api.Tests.Integration;

[CollectionDefinition(CollectionName)]
public class CustomerApiTestCollection : ICollectionFixture<WebApplicationFactory<IApiMarker>>
{
    public const string CollectionName = "CustomerApi Collection";
}
