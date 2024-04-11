using Bogus;
using Customers.WebApp.Models;
using FluentAssertions;

namespace Customers.WebApp.Tests.Integration.Pages;

[Collection(nameof(SharedTestCollection))]
public class AddCustomerTests
{
    private readonly SharedTestContext _testContext;
    private readonly Faker<Customer> _validCustomerGenerator;
    public AddCustomerTests(SharedTestContext testContext)
    {
        _testContext = testContext;
        _validCustomerGenerator = testContext.Generators.RequestGenerators.Default;
    }

    [Fact]
    public async Task Create_ShouldCreateCustomer_WhenCustomerIsValid()
    {
        // Arrange
        var page = await _testContext.Browser.NewPageAsync(new()
        {
            BaseURL = SharedTestContext.AppUrl
        });
        await page.GotoAsync("add-customer");
        var customer = _validCustomerGenerator.Generate();

        // Act
        await page.FillAsync("input[id=fullname]", customer.FullName);
        await page.FillAsync("input[id=email]", customer.Email);
        await page.FillAsync("input[id=github-username]", customer.GitHubUsername);
        await page.FillAsync("input[id=dob]", customer.DateOfBirth.ToString("yyyy-MM-dd"));

        await page.ClickAsync("button[type=submit]");

        // Assert
        var linkElement = page.Locator("article>p>a").First;
        var link = await linkElement.GetAttributeAsync("href");
        link.Should().NotBeNull().And.NotBeEmpty();

        await page.GotoAsync(link!);
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
    public async Task Create_ShouldShowError_WhenCustomerEmailIsInValid()
    {
        // Arrange
        const string InvalidEmail = "notanemail";
        var page = await _testContext.Browser.NewPageAsync(new()
        {
            BaseURL = SharedTestContext.AppUrl
        });
        await page.GotoAsync("add-customer");
        var customer = _validCustomerGenerator.Generate();

        // Act
        await page.FillAsync("input[id=email]", InvalidEmail);
        await page.FillAsync("input[id=fullname]", customer.FullName);
        //await page.FillAsync("input[id=github-username]", customer.GitHubUsername);
        //await page.FillAsync("input[id=dob]", customer.DateOfBirth.ToString("yyyy-MM-dd"));        

        // Assert
        var element = page.Locator("li.validation-message").First;
        var text = await element.InnerTextAsync();
        
        text.Should().Be("Invalid email format");

        await page.CloseAsync();
    }
}
