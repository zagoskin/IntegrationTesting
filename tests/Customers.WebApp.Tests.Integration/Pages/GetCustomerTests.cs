using Bogus;
using Customers.WebApp.Models;
using FluentAssertions;

namespace Customers.WebApp.Tests.Integration.Pages;


[Collection(nameof(SharedTestCollection))]
public class GetCustomerTests
{
    private readonly SharedTestContext _testContext;
    private readonly Faker<Customer> _validCustomerGenerator;

    public GetCustomerTests(SharedTestContext testContext)
    {
        _testContext = testContext;
        _validCustomerGenerator = testContext.Generators.RequestGenerators.Default;
    }

    [Fact]
    public async Task Get_ShouldReturnCustomer_WhenCustomerExists()
    {
        // Arrange
        var page = await _testContext.Browser.NewPageAsync(new()
        {
            BaseURL = SharedTestContext.AppUrl
        });
        Customer customer = await CreateCustomerAsync(page);

        // Act
        var linkElement = page.Locator("article>p>a").First;
        var link = await linkElement.GetAttributeAsync("href");
        await page.GotoAsync(link!);

        // Assert
        (await page.Locator("p[id=fullname-field]").InnerTextAsync())
            .Should().Be(customer.FullName);

        (await page.Locator("p[id=email-field]").InnerTextAsync())
            .Should().Be(customer.Email);

        (await page.Locator("p[id=github-username-field]").InnerTextAsync())
            .Should().Be(customer.GitHubUsername);

        (await page.Locator("p[id=dob-field]").InnerTextAsync())
            .Should().Be(customer.DateOfBirth.ToString("dd/MM/yyyy"));

        await page.CloseAsync();
    }

    [Fact]
    public async Task Get_ShouldReturnNotFoundPage_WhenCustomerDoesNotExists()
    {
        // Arrange
        var page = await _testContext.Browser.NewPageAsync(new()
        {
            BaseURL = SharedTestContext.AppUrl
        });
        var customerUrl = $"{SharedTestContext.AppUrl}/customer/{Guid.NewGuid()}";

        // Act
        await page.GotoAsync(customerUrl);

        // Assert
        (await page.Locator("article>p").InnerTextAsync())
            .Should().Be("No customer found with this id");

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