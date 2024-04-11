using Bogus;
using Customers.WebApp.Models;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using Microsoft.Playwright;

namespace Customers.WebApp.Tests.Integration;
public class SharedTestContext : IAsyncLifetime
{
    private static readonly string _dockerComposeFile = Path.Combine(Directory.GetCurrentDirectory(), "../../../docker-compose.integration.yml");
    public const string ValidUser = "validuser";
    public const string InvalidUser = "invaliduser";
    public const string ThrottledUser = "throttleduser";
    public const string AppUrl = "https://localhost:7780";

    private IPlaywright _playwright = null!;

    public IBrowser Browser { get; private set; } = null!;

    private readonly GitHubApiServer _gitHubApiServer = new();
    private readonly ICompositeService _dockerService = new Builder()
        .UseContainer()
        .UseCompose()
        .FromFile(_dockerComposeFile)
        .RemoveOrphans()
        .WaitForHttp("test-app", AppUrl)
        .Build();            

    public async Task InitializeAsync()
    {
        _gitHubApiServer.Start();
        _gitHubApiServer.SetupUser(ValidUser, true);
        _gitHubApiServer.SetupUser(InvalidUser, false);
        _gitHubApiServer.SetupThrottledUser(ThrottledUser);
        
        _dockerService.Start();

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
            SlowMo = 1000
        });
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _dockerService.Dispose();
        _gitHubApiServer.Dispose();

        await Browser.DisposeAsync();
        _playwright.Dispose();
    }

    public CustomerApiGenerators Generators => CustomerApiGenerators.Instance;

    public class CustomerApiGenerators
    {
        private static readonly CustomerApiGenerators _instance = new();
        public static CustomerApiGenerators Instance => _instance;
        private static readonly CustomerRequestGenerators _requestGenerators = new();
        public CustomerRequestGenerators RequestGenerators => _requestGenerators;
    }

    public class CustomerRequestGenerators
    {
        private static readonly Faker<Customer> _default = new Faker<Customer>()
            .RuleFor(x => x.FullName, faker => faker.Person.FullName)
            .RuleFor(x => x.Email, faker => faker.Person.Email)
            .RuleFor(x => x.GitHubUsername, ValidUser)
            .RuleFor(x => x.DateOfBirth, faker => DateOnly.FromDateTime(faker.Person.DateOfBirth));

        private static readonly Faker<Customer> _invalid = new Faker<Customer>()
            .RuleFor(x => x.FullName, faker => faker.Person.FullName)
            .RuleFor(x => x.Email, faker => faker.Person.Email)
            .RuleFor(x => x.GitHubUsername, InvalidUser)
            .RuleFor(x => x.DateOfBirth, faker => DateOnly.FromDateTime(faker.Person.DateOfBirth));
        public Faker<Customer> Default => _default;
        public Faker<Customer> InvalidGitHubUser => _invalid;

        public Customer Generate(Action<Faker<Customer>> configure)
        {
            var faker = new Faker<Customer>();
            configure(faker);
            return faker;
        }
    }
}
