namespace Customers.Api.Contracts.Requests;

public class CustomerRequest
{
    public required string GitHubUsername { get; init; } = default!;

    public required string FullName { get; init; } = default!;

    public required string Email { get; init; } = default!;

    public required DateTime DateOfBirth { get; init; } = default!;
}
