using Bogus;
using Customers.Api.Contracts.Requests;
using Customers.Api.Contracts.Responses;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Customers.Api.Tests.Integration.CustomerControllerTests;
public class GetAllCustomersTests : IClassFixture<CustomerApiFactory>
{
    private readonly HttpClient _httpClient;
    private readonly Faker<CustomerRequest> _validCustomerGenerator;
    public GetAllCustomersTests(CustomerApiFactory appFactory)
    {
        _httpClient = appFactory.CreateClient();
        _validCustomerGenerator = appFactory.Generators.RequestGenerators.Default;
    }

    [Fact]
    public async Task GetAll_ReturnsAllCustomers_WhenCustomersExist()
    {
        // Arrange
        var request = _validCustomerGenerator.Generate();
        var createdResponse = await _httpClient.PostAsJsonAsync($"customers", request);
        var createdCustomer = await createdResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Act
        var response = await _httpClient.GetAsync("customers");
        var customersResponse = await response.Content.ReadFromJsonAsync<GetAllCustomersResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        customersResponse.Should().NotBeNull();
        customersResponse!.Customers.Single().Should().BeEquivalentTo(createdCustomer);

        // Cleanup
        await _httpClient.DeleteAsync($"customers/{createdCustomer!.Id}");
    }


    [Fact]
    public async Task GetAll_ReturnsEmptyResult_WhenNoCustomersExist()
    {
        // Act
        var response = await _httpClient.GetAsync("customers");
        var customersResponse = await response.Content.ReadFromJsonAsync<GetAllCustomersResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        customersResponse.Should().NotBeNull();
        customersResponse!.Customers.Should().BeEmpty();
    }    
}
