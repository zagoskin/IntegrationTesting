using Bogus;
using Customers.Api.Contracts.Requests;
using Customers.Api.Contracts.Responses;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;

namespace Customers.Api.Tests.Integration.CustomerControllerTests;
public class UpdateCustomerTests : IClassFixture<CustomerApiFactory>
{
    private readonly HttpClient _httpClient;
    private readonly Faker<CustomerRequest> _validCustomerGenerator;
    private readonly Faker<CustomerRequest> _invalidCustomerGenerator;
    public UpdateCustomerTests(CustomerApiFactory appFactory)
    {
        _httpClient = appFactory.CreateClient();
        _validCustomerGenerator = appFactory.Generators.RequestGenerators.Default;
        _invalidCustomerGenerator = appFactory.Generators.RequestGenerators.InvalidGitHubUser;
    }

    [Fact]
    public async Task Update_UpdatesCustomer_WhenDataIsValid()
    {
        // Arrange
        var request = _validCustomerGenerator.Generate();        
        var response = await _httpClient.PostAsJsonAsync($"customers", request);
        var createdCustomer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        var updateRequest = _validCustomerGenerator.Generate();

        // Act
        var updateResponse = await _httpClient.PutAsJsonAsync($"customers/{createdCustomer!.Id}", updateRequest);        
        var updatedCustomer = await updateResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedCustomer.Should().NotBeNull();
        updatedCustomer.Should().BeEquivalentTo(updateRequest);
    }

    [Fact]
    public async Task Update_ReturnsValidationError_WhenEmailIsInvalid()
    {
        // Arrange
        const string InvalidEmail = "notanemail";
        var request = _validCustomerGenerator.Generate();
        var response = await _httpClient.PostAsJsonAsync($"customers", request);
        var createdCustomer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        var updateRequest = _validCustomerGenerator.Clone()
            .RuleFor(c => c.Email, InvalidEmail)
            .Generate();        

        // Act
        var updateResponse = await _httpClient.PutAsJsonAsync($"customers/{createdCustomer!.Id}", updateRequest);
        var problem = await updateResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        updateResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        problem.Should().NotBeNull();
        problem!.Title.Should().Be("One or more validation errors occurred.");
        problem.Errors.Should().ContainKey(nameof(CustomerRequest.Email));
        problem.Errors[nameof(CustomerRequest.Email)][0].Should().Be($"{InvalidEmail} is not a valid email address");
    }

    [Fact]
    public async Task Update_ReturnsValidationError_WhenGitHubCustomerDoesNotExist()
    {
        // Arrange
        var request = _validCustomerGenerator.Generate();
        var response = await _httpClient.PostAsJsonAsync($"customers", request);
        var createdCustomer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        var updateRequest = _invalidCustomerGenerator.Generate();
        

        // Act
        var updateResponse = await _httpClient.PutAsJsonAsync($"customers/{createdCustomer!.Id}", updateRequest);
        var problem = await updateResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        updateResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        problem.Should().NotBeNull();
        problem!.Title.Should().Be("One or more validation errors occurred.");

        problem.Errors.Should().ContainKey(nameof(CustomerRequest.GitHubUsername));
        problem.Errors[nameof(CustomerRequest.GitHubUsername)][0].Should().Be($"There is no GitHub user with username {updateRequest.GitHubUsername}");
    }
}
