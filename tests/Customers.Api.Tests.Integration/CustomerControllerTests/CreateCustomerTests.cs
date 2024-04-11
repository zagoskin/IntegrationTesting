using Bogus;
using Customers.Api.Contracts.Requests;
using Customers.Api.Contracts.Responses;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;

namespace Customers.Api.Tests.Integration.CustomerControllerTests;

//[Collection(CustomerApiTestCollection.CollectionName)]
public class CreateCustomerTests : IClassFixture<CustomerApiFactory>    
{
    private readonly HttpClient _httpClient;
    private readonly Faker<CustomerRequest> _validCustomerGenerator;
    private readonly Faker<CustomerRequest> _invalidCustomerGenerator;
    public CreateCustomerTests(CustomerApiFactory appFactory)
    {
        _httpClient = appFactory.CreateClient();
        _validCustomerGenerator = appFactory.Generators.RequestGenerators.Default;
        _invalidCustomerGenerator = appFactory.Generators.RequestGenerators.InvalidGitHubUser;
    }

    [Fact]
    public async Task Create_CreatesCustomer_WhenCustomerIsValid()
    {
        // Arrange
        var request = _validCustomerGenerator.Generate();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"customers", request);
        var createdCustomer = await response.Content.ReadFromJsonAsync<CustomerResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.LocalPath.Should().Be($"/customers/{createdCustomer!.Id}");
        createdCustomer.Should().NotBeNull();
        createdCustomer.Should().BeEquivalentTo(request);
    }

    [Fact]
    public async Task Create_ReturnsValidationError_WhenEmailIsInvalid()
    {
        // Arrange
        const string InvalidEmail = "notanemail";
        var request = _validCustomerGenerator.Clone()
            .RuleFor(c => c.Email, InvalidEmail)
            .Generate();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"customers", request);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        problem.Should().NotBeNull();
        problem!.Title.Should().Be("One or more validation errors occurred.");
        problem.Errors.Should().ContainKey(nameof(CustomerRequest.Email));
        problem.Errors[nameof(CustomerRequest.Email)][0].Should().Be($"{InvalidEmail} is not a valid email address");
    }

    [Fact]
    public async Task Create_ReturnsValidationError_WhenGitHubUserIsInvalid()
    {
        // Arrange        
        var request = _invalidCustomerGenerator.Generate();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"customers", request);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        problem.Should().NotBeNull();
        problem!.Title.Should().Be("One or more validation errors occurred.");
        problem.Errors.Should().ContainKey(nameof(CustomerRequest.GitHubUsername));
        problem.Errors[nameof(CustomerRequest.GitHubUsername)][0].Should().Be($"There is no GitHub user with username {request.GitHubUsername}");
    }

    [Fact]
    public async Task Create_ReturnsInternalServerError_WhenGitHubIsThrottled()
    {
        // Arrange
        var request = _validCustomerGenerator.Clone()
            .RuleFor(x => x.GitHubUsername, CustomerApiFactory.ThrottledUser)
            .Generate();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"customers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}
