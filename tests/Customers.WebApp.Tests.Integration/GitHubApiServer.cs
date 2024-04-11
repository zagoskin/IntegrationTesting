using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Customers.WebApp.Tests.Integration;
public class GitHubApiServer : IDisposable
{    
    private WireMockServer _server = null!;
    public string Url => _server.Url!;
    public void Start()
    {
        _server = WireMockServer.Start(port: 9850);
    }

    public void SetupUser(string username, bool isValid)
    {
        _server.Given(Request.Create()
            .WithPath($"/users/{username}").UsingGet())
            .RespondWith(Response.Create()
                .WithBody(isValid ? GetSuccessBody(username) : GetNotFoundBody())
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithStatusCode(isValid ? 200 : 404));
    }

    public void SetupThrottledUser(string username)
    {
        _server.Given(Request.Create()
            .WithPath($"/users/{username}").UsingGet())
            .RespondWith(Response.Create()
                .WithBody(GetRateLimitBody())
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithStatusCode(403));
    }

    private static string GetRateLimitBody()
    {
        return """
            {
                "message": "API rate limit exceeded for 127.0.0.1",
                "documentation_url": "https://docs.github.com/rest/overview/resources-in-the-rest-api#rate-limiting"
            }
            """;
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }

    private string GetNotFoundBody()
    {
        return """
            {
                "message": "Not Found",
                "documentation_url": "https://docs.github.com/rest/users/users#get-a-user"
            }
            """;
    }

    private string GetSuccessBody(string username)
    {
        return $$"""
            {
                "login": {{username}},
                "id": 26393765,
                "node_id": "MDQ6VXNlcjI2MzkzNzY1",
                "avatar_url": "https://avatars.githubusercontent.com/u/26393765?v=4",
                "gravatar_id": "",
                "url": "https://api.github.com/users/{{username}}",
                "html_url": "https://github.com/{{username}}",
                "followers_url": "https://api.github.com/users/{{username}}/followers",
                "following_url": "https://api.github.com/users/{{username}}/following{/other_user}",
                "gists_url": "https://api.github.com/users/{{username}}/gists{/gist_id}",
                "starred_url": "https://api.github.com/users/{{username}}/starred{/owner}{/repo}",
                "subscriptions_url": "https://api.github.com/users/{{username}}/subscriptions",
                "organizations_url": "https://api.github.com/users/{{username}}/orgs",
                "repos_url": "https://api.github.com/users/{{username}}/repos",
                "events_url": "https://api.github.com/users/{{username}}/events{/privacy}",
                "received_events_url": "https://api.github.com/users/{{username}}/received_events",
                "type": "User",
                "site_admin": false,
                "name": "Generic User",
                "company": null,
                "blog": "",
                "location": null,
                "email": null,
                "hireable": null,
                "bio": null,
                "twitter_username": null,
                "public_repos": 6,
                "public_gists": 0,
                "followers": 0,
                "following": 1,
                "created_at": "2017-03-13T22:09:24Z",
                "updated_at": "2024-03-27T15:30:26Z"
            }
            """;
    }
}
