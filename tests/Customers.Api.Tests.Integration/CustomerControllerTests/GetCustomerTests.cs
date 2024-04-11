using Bogus;
using Customers.Api.Contracts.Requests;
using Customers.Api.Contracts.Responses;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;

namespace Customers.Api.Tests.Integration.CustomerControllerTests;

//[Collection(CustomerApiTestCollection.CollectionName)]
public class GetCustomerTests : IClassFixture<CustomerApiFactory>
{
    private readonly HttpClient _httpClient;
    private readonly Faker<CustomerRequest> _validCustomerGenerator;
    public GetCustomerTests(CustomerApiFactory appFactory)
    {
        _httpClient = appFactory.CreateClient();
        _validCustomerGenerator = appFactory.Generators.RequestGenerators.Default;
    }

    [Fact]
    public async Task Get_ShouldReturnCustomer_WhenCustomerExists()
    {
        // Arrange
        var request = _validCustomerGenerator.Generate();
        var createdResponse = await _httpClient.PostAsJsonAsync($"customers", request);

        // Act
        var response = await _httpClient.GetAsync(createdResponse.Headers.Location!.LocalPath);        
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        customer.Should().NotBeNull();
        customer.Should().BeEquivalentTo(request);
    }


    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenCustomerDoesNotExists()
    {
        // Act
        var response = await _httpClient.GetAsync($"customers/{Guid.NewGuid()}");        
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Not Found");
        problemDetails.Status.Should().Be((int)HttpStatusCode.NotFound);        
    }
}
