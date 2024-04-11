using Bogus;
using Customers.Api.Contracts.Requests;
using Customers.Api.Database;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Customers.Api.Tests.Integration;
public class CustomerApiFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    public const string ValidUser = "validuser";
    public const string InvalidUser = "invaliduser";
    public const string ThrottledUser = "throttleduser";

    private static int _startDbPort = 5555;
    private int _dbPort;
    private IContainer _dbContainer = null!;
    private readonly GitHubApiServer _gitHubApiServer = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {        
        builder.ConfigureLogging(builder =>
        {
            builder.ClearProviders();
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();

            services.RemoveAll<IDbConnectionFactory>();
            services.AddSingleton<IDbConnectionFactory>(_ => 
                new NpgsqlConnectionFactory($"Server=localhost;Port={_dbPort};Database=mydb;User ID=course;Password=changeme;"));
            
            services.AddHttpClient("GitHub", httpClient =>
            {
                httpClient.BaseAddress = new Uri(_gitHubApiServer.Url);
                httpClient.DefaultRequestHeaders.Add(
                    HeaderNames.Accept, "application/vnd.github.v3+json");
                httpClient.DefaultRequestHeaders.Add(
                    HeaderNames.UserAgent, $"Course-{Environment.MachineName}");
            });

        });

        base.ConfigureWebHost(builder);        
    }    

    public async Task InitializeAsync()
    {
        _gitHubApiServer.Start();
        _gitHubApiServer.SetupUser(ValidUser, true);
        _gitHubApiServer.SetupThrottledUser(ThrottledUser);
        _dbPort = Interlocked.Increment(ref _startDbPort);
        _dbContainer = new ContainerBuilder()
            .WithImage("postgres:latest")
            .WithEnvironment("POSTGRES_USER", "course")
            .WithEnvironment("POSTGRES_PASSWORD", "changeme")
            .WithEnvironment("POSTGRES_DB", "mydb")
            .WithPortBinding(_dbPort, 5432)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();
        await _dbContainer.StartAsync();        
    }

    public new async Task DisposeAsync()
    {        
        await _dbContainer.StopAsync();
        _gitHubApiServer.Dispose();
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
        private static readonly Faker<CustomerRequest> _default = new Faker<CustomerRequest>()
            .RuleFor(x => x.FullName, faker => faker.Person.FullName)
            .RuleFor(x => x.Email, faker => faker.Person.Email)
            .RuleFor(x => x.GitHubUsername, ValidUser)
            .RuleFor(x => x.DateOfBirth, faker => faker.Person.DateOfBirth.Date);

        private static readonly Faker<CustomerRequest> _invalid = new Faker<CustomerRequest>()
            .RuleFor(x => x.FullName, faker => faker.Person.FullName)
            .RuleFor(x => x.Email, faker => faker.Person.Email)
            .RuleFor(x => x.GitHubUsername, InvalidUser)
            .RuleFor(x => x.DateOfBirth, faker => faker.Person.DateOfBirth.Date);
        public Faker<CustomerRequest> Default => _default;
        public Faker<CustomerRequest> InvalidGitHubUser => _invalid;

        public CustomerRequest Generate(Action<Faker<CustomerRequest>> configure)
        {
            var faker = new Faker<CustomerRequest>();
            configure(faker);
            return faker;
        }
    }
}


