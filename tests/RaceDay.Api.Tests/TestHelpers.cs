using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

/// <summary>Boots the real API in memory, swapping SQL Server for an isolated EF in-memory database.</summary>
public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "RaceDayTests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Registration:OrganiserCode", TestData.OrganiserCode);
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}

public static class TestData
{
    public const string OrganiserCode = "RACEDAY-ORG-2026";
    public const string Password = "Password123!";
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@test.com";
}

public static class ClientExtensions
{
    /// <summary>Registers a new user and logs them in. Each client has its own cookie jar (= its own session).</summary>
    public static async Task<(HttpClient client, string email)> RegisterAndLoginAsync(this RaceDayFactory factory, string role)
    {
        var client = factory.CreateClient();
        var email = TestData.UniqueEmail(role.ToLower());
        var reg = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test",
            lastName = role,
            email,
            password = TestData.Password,
            dateOfBirth = "1995-05-05",
            phone = "0821234567",
            role,
            organiserCode = role == "Organiser" ? TestData.OrganiserCode : null
        });
        reg.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestData.Password });
        login.EnsureSuccessStatusCode();
        return (client, email);
    }

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage res) =>
        await res.Content.ReadFromJsonAsync<JsonElement>();

    /// <summary>Creates an event (future date) as the given organiser client and returns its id.</summary>
    public static async Task<int> CreateEventAsync(this HttpClient organiser, string name = "City 10K")
    {
        var res = await organiser.PostAsJsonAsync("/api/events", new
        {
            name,
            description = "A fast city road race",
            eventDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
            location = "Cape Town",
            distanceKm = 10,
            eventType = "Run"
        });
        res.EnsureSuccessStatusCode();
        return (await res.ReadJsonAsync()).GetProperty("eventId").GetInt32();
    }

    public static async Task<int> CreateCategoryAsync(this HttpClient organiser, int eventId, string name = "Senior")
    {
        var res = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            name,
            categoryType = "Age",
            minAge = 20,
            maxAge = 39
        });
        res.EnsureSuccessStatusCode();
        return (await res.ReadJsonAsync()).GetProperty("categoryId").GetInt32();
    }
}
