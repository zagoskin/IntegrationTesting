using Bogus;
using Customers.WebApp.Models;
using FluentAssertions;
using Microsoft.Playwright;

namespace Customers.WebApp.Tests.Integration.Pages;

[Collection(nameof(SharedTestCollection))]
public class DeleteCustomerTests
{
    private readonly SharedTestContext _testContext;
    private readonly Faker<Customer> _validCustomerGenerator;

    public DeleteCustomerTests(SharedTestContext testContext)
    {
        _testContext = testContext;
        _validCustomerGenerator = testContext.Generators.RequestGenerators.Default;
    }

    [Fact]
    public async Task Delete_ShouldDeleteCustomer_WhenCustomerExists()
    {
        // Arrange
        const string DeleteText = "Delete";
        var page = await _testContext.Browser.NewPageAsync(new()
        {
            BaseURL = SharedTestContext.AppUrl
        });
        Customer customer = await CreateCustomerAsync(page);
        var linkElement = page.Locator("article>p>a").First;
        var link = await linkElement.GetAttributeAsync("href");
        await page.GotoAsync(link!);

        // Act
        page.Dialog += (_, dialog) => dialog.AcceptAsync();
        await page.Locator("button")
            .Filter(new LocatorFilterOptions { HasTextString = DeleteText })
            .ClickAsync();

        // Assert
        await page.GotoAsync(link!);
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
