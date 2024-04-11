using Bogus;
using Customers.WebApp.Models;
using FluentAssertions;
using Microsoft.Playwright;

namespace Customers.WebApp.Tests.Integration.Pages;

[Collection(nameof(SharedTestCollection))]
public class GetAllCustomersTests
{
    private readonly SharedTestContext _testContext;
    private readonly Faker<Customer> _validCustomerGenerator;

    public GetAllCustomersTests(SharedTestContext testContext)
    {
        _testContext = testContext;
        _validCustomerGenerator = testContext.Generators.RequestGenerators.Default;
    }

    [Fact]
    public async Task GetAll_ShouldContainCustomer_WhenCustomerExists()
    {
        // Arrange
        const string GetAllCustomersUrl = "customers";
        var page = await _testContext.Browser.NewPageAsync(new()
        {
            BaseURL = SharedTestContext.AppUrl
        });
        Customer customer = await CreateCustomerAsync(page);

        // Act
        await page.GotoAsync(GetAllCustomersUrl);

        // Assert
        (await page.Locator("table>tbody>tr>td")
            .Filter(new LocatorFilterOptions { HasTextString = customer.FullName })
            .First
            .InnerTextAsync())
            .Should().NotBeNullOrEmpty()
            .And.Be(customer.FullName);

        (await page.Locator("table>tbody>tr>td")
            .Filter(new LocatorFilterOptions { HasTextString = customer.Email })
            .First
            .InnerTextAsync())
            .Should().NotBeNullOrEmpty()
            .And.Be(customer.Email);

        (await page.Locator("table>tbody>tr>td")
            .Filter(new LocatorFilterOptions { HasTextString = customer.GitHubUsername })
            .First
            .InnerTextAsync())
            .Should().NotBeNullOrEmpty()
            .And.Be(customer.GitHubUsername);

        (await page.Locator("table>tbody>tr>td")
            .Filter(new LocatorFilterOptions { HasTextString = customer.DateOfBirth.ToString("dd/MM/yyyy") })
            .First
            .InnerTextAsync())
            .Should().NotBeNullOrEmpty()
            .And.Be(customer.DateOfBirth.ToString("dd/MM/yyyy"));

        await page.CloseAsync();
    }

    private async Task<Customer> CreateCustomerAsync(Microsoft.Playwright.IPage page)
    {
        await page.GotoAsync("add-customer");
        var customer = _validCustomerGenerator.Generate();
        await page.FillAsync("input[id=fullname]", customer.FullName);
        await page.FillAsync("input[id=email]", customer.Email);
        await page.FillAsync("input[id=github-username]", customer.GitHubUsername);
        await page.FillAsync("input[id=dob]", customer.DateOfBirth.ToString("yyyy-MM-dd"));

        await page.ClickAsync("button[type=submit]");
        return customer;
    }
}
