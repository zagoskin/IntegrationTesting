using Bogus;
using Customers.Api.Contracts.Requests;
using Customers.Api.Contracts.Responses;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;

namespace Customers.Api.Tests.Integration.CustomerControllerTests;
public class DeleteCustomerTests : IClassFixture<CustomerApiFactory>
{
    private readonly HttpClient _httpClient;
    private readonly Faker<CustomerRequest> _validCustomerGenerator;
    public DeleteCustomerTests(CustomerApiFactory appFactory)
    {
        _httpClient = appFactory.CreateClient();
        _validCustomerGenerator = appFactory.Generators.RequestGenerators.Default;
    }

    [Fact]
    public async Task Delete_ReturnsOkAndDeletesCustomer_WhenCustomerExists()
    {
        // Arrange
        var request = _validCustomerGenerator.Generate();
        var createdResponse = await _httpClient.PostAsJsonAsync($"customers", request);

        // Act
        var response = await _httpClient.DeleteAsync(createdResponse.Headers.Location!.LocalPath);        
        var getResponse = await _httpClient.GetAsync(createdResponse.Headers.Location!.LocalPath);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }


    [Fact]
    public async Task Delete_ReturnsNotFound_WhenCustomerDoesNotExist()
    {
        // Act
        var response = await _httpClient.DeleteAsync($"customers/{Guid.NewGuid()}");
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Not Found");
        problemDetails.Status.Should().Be((int)HttpStatusCode.NotFound);
    }
}
