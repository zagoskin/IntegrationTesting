using Bogus;
using Customers.WebApp.Models;
using FluentAssertions;

namespace Customers.WebApp.Tests.Integration.Pages;
[Collection(nameof(SharedTestCollection))]
public class UpdateCustomerTests
{
    private readonly SharedTestContext _testContext;
    private readonly Faker<Customer> _validCustomerGenerator;
    public UpdateCustomerTests(SharedTestContext testContext)
    {
        _testContext = testContext;
        _validCustomerGenerator = testContext.Generators.RequestGenerators.Default;
    }

    [Fact]
    public async Task Update_ShouldUpdateCustomer_WhenDataIsValid()
    {
        // Arrange
        var page = await _testContext.Browser.NewPageAsync(new()
        {
            BaseURL = SharedTestContext.AppUrl
        });
        _ = await CreateCustomerAsync(page);
        var createdCustomerLinkElement = page.Locator("article>p>a").First;
        var customerId = (await createdCustomerLinkElement.GetAttributeAsync("href"))!.Split('/').Last();
        var updateCustomer = _validCustomerGenerator.Generate();

        // Act
        await UpdateCustomer(page, customerId, updateCustomer);

        // Assert
        var linkElement = page.Locator("article>p>a").First;
        var link = await linkElement.GetAttributeAsync("href");
        link.Should().NotBeNull().And.NotBeEmpty();

        await page.GotoAsync(link!);
        (await page.Locator("p[id=fullname-field]").InnerTextAsync())
            .Should().Be(updateCustomer.FullName);

        (await page.Locator("p[id=email-field]").InnerTextAsync())
            .Should().Be(updateCustomer.Email);

        (await page.Locator("p[id=github-username-field]").InnerTextAsync())
            .Should().Be(updateCustomer.GitHubUsername);

        (await page.Locator("p[id=dob-field]").InnerTextAsync())
            .Should().Be(updateCustomer.DateOfBirth.ToString("dd/MM/yyyy"));

        await page.CloseAsync();
    }

    [Fact]
    public async Task Update_ShouldShowError_WhenEmailIsInvalid()
    {
        // Arrange
        const string InvalidEmail = "notanemail";
        var page = await _testContext.Browser.NewPageAsync(new()
        {
            BaseURL = SharedTestContext.AppUrl
        });
        _ = await CreateCustomerAsync(page);
        var createdCustomerLinkElement = page.Locator("article>p>a").First;
        var customerId = (await createdCustomerLinkElement.GetAttributeAsync("href"))!.Split('/').Last();
        var updateCustomer = _validCustomerGenerator.Generate();
        updateCustomer.Email = InvalidEmail;

        // Act
        await UpdateCustomer(page, customerId, updateCustomer, submit: false);

        // Assert
        var element = page.Locator("li.validation-message").First;
        var text = await element.InnerTextAsync();
        text.Should().Be("Invalid email format");

        await page.CloseAsync();
    }

    private static async Task UpdateCustomer(Microsoft.Playwright.IPage page, string customerId, Customer updateCustomer, bool submit = true)
    {
        await page.GotoAsync($"/update-customer/{customerId}");
        await page.FillAsync("input[id=fullname]", updateCustomer.FullName);
        await page.FillAsync("input[id=email]", updateCustomer.Email);
        await page.FillAsync("input[id=github-username]", updateCustomer.GitHubUsername);
        await page.FillAsync("input[id=dob]", updateCustomer.DateOfBirth.ToString("yyyy-MM-dd"));

        if (!submit)
        {
            return;
        }

        await page.ClickAsync("button[type=submit]");
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
