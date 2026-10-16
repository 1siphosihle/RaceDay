using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;
    public AuthTests(RaceDayFactory factory) => _factory = factory;

    private static object Reg(string email, string role = "Participant", string? code = null, string password = "Password123!") => new
    {
        firstName = "Jane", lastName = "Runner", email, password,
        dateOfBirth = "1990-01-01", phone = "0820000000", role, organiserCode = code
    };

    [Fact]
    public async Task Register_Participant_ReturnsCreated_AndNeverReturnsPassword()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register", Reg(TestData.UniqueEmail("p")));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Participant", body);
    }

    [Fact]
    public async Task Register_Organiser_WithCorrectCode_Succeeds()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register", Reg(TestData.UniqueEmail("o"), "Organiser", TestData.OrganiserCode));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithoutCodeOrWithWrongCode_IsForbidden()
    {
        var client = _factory.CreateClient();
        var none = await client.PostAsJsonAsync("/api/auth/register", Reg(TestData.UniqueEmail("o"), "Organiser"));
        var wrong = await client.PostAsJsonAsync("/api/auth/register", Reg(TestData.UniqueEmail("o"), "Organiser", "nope"));
        Assert.Equal(HttpStatusCode.Forbidden, none.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, wrong.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var email = TestData.UniqueEmail("dup");
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", Reg(email))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", Reg(email))).StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register", Reg(TestData.UniqueEmail("x"), "Admin"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register", Reg(TestData.UniqueEmail("x"), password: "short"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk_AndSessionAllowsProfileAccess()
    {
        var (client, email) = await _factory.RegisterAndLoginAsync("Participant");
        var profile = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        Assert.Equal(email, (await profile.ReadJsonAsync()).GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var email = TestData.UniqueEmail("p");
        await client.PostAsJsonAsync("/api/auth/register", Reg(email));
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUser_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@test.com", password = "Password123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsSession()
    {
        var (client, _) = await _factory.RegisterAndLoginAsync("Participant");
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/profile")).StatusCode);
    }

    [Fact]
    public async Task Profile_CanBeUpdated_ByOwner()
    {
        var (client, _) = await _factory.RegisterAndLoginAsync("Participant");
        var res = await client.PutAsJsonAsync("/api/profile", new
        {
            firstName = "Updated", lastName = "Name", dateOfBirth = "1991-02-02", phone = "0831112222"
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Updated", (await res.ReadJsonAsync()).GetProperty("firstName").GetString());
    }

    [Fact]
    public async Task Passwords_AreStoredHashed()
    {
        var hasher = new RaceDay.Api.Services.PasswordHasher();
        var hash = hasher.Hash("Password123!");
        Assert.DoesNotContain("Password123!", hash);
        Assert.True(hasher.Verify("Password123!", hash));
        Assert.False(hasher.Verify("password123!", hash));
        Assert.NotEqual(hash, hasher.Hash("Password123!")); // random salt
    }
}
