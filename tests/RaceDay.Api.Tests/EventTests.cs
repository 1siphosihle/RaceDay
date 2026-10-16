using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

public class EventTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;
    public EventTests(RaceDayFactory factory) => _factory = factory;

    private static object EventBody(string name = "Updated Event") => new
    {
        name, description = "desc", eventDate = DateTime.UtcNow.AddDays(10).ToString("yyyy-MM-dd"),
        location = "Durban", distanceKm = 21.1, eventType = "Cycle"
    };

    [Fact]
    public async Task Unauthenticated_Request_ToEvents_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/events")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreate_Update_AndDeleteEvent()
    {
        var (org, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var id = await org.CreateEventAsync();

        var update = await org.PutAsJsonAsync($"/api/events/{id}", EventBody());
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var json = await update.ReadJsonAsync();
        Assert.Equal("Updated Event", json.GetProperty("name").GetString());
        Assert.Equal("Cycle", json.GetProperty("eventType").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await org.DeleteAsync($"/api/events/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await org.GetAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CreateEvent_WithInvalidData_ReturnsBadRequest()
    {
        var (org, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var res = await org.PostAsJsonAsync("/api/events", new { name = "", description = "d", eventDate = "2030-01-01", location = "x", distanceKm = 0, eventType = "Run" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CreateEvent_WithInvalidType_ReturnsBadRequest()
    {
        var (org, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var res = await org.PostAsJsonAsync("/api/events", new { name = "x", description = "d", eventDate = "2030-01-01", location = "x", distanceKm = 5, eventType = "Swim" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Both_Roles_CanViewEvents()
    {
        var (org, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var (par, _) = await _factory.RegisterAndLoginAsync("Participant");
        var id = await org.CreateEventAsync("Viewable Event");

        Assert.Equal(HttpStatusCode.OK, (await org.GetAsync($"/api/events/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await par.GetAsync($"/api/events/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await par.GetAsync("/api/events")).StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreate_Update_OrDeleteEvents()
    {
        var (org, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var (par, _) = await _factory.RegisterAndLoginAsync("Participant");
        var id = await org.CreateEventAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await par.PostAsJsonAsync("/api/events", EventBody())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await par.PutAsJsonAsync($"/api/events/{id}", EventBody())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await par.DeleteAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotModify_AnotherOrganisersEvent()
    {
        var (owner, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var (other, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var id = await owner.CreateEventAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"/api/events/{id}", EventBody())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task UpdatingMissingEvent_ReturnsNotFound()
    {
        var (org, _) = await _factory.RegisterAndLoginAsync("Organiser");
        Assert.Equal(HttpStatusCode.NotFound, (await org.PutAsJsonAsync("/api/events/999999", EventBody())).StatusCode);
    }

    [Fact]
    public async Task Organiser_CanManageCategories_AndBothRolesCanViewThem()
    {
        var (org, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var (par, _) = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await org.CreateEventAsync();
        var catId = await org.CreateCategoryAsync(eventId);

        var list = await par.GetAsync($"/api/events/{eventId}/categories");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(1, (await list.ReadJsonAsync()).GetArrayLength());

        var upd = await org.PutAsJsonAsync($"/api/categories/{catId}", new { name = "Under 20", categoryType = "Age", minAge = 0, maxAge = 19 });
        Assert.Equal(HttpStatusCode.OK, upd.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await org.DeleteAsync($"/api/categories/{catId}")).StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateCategory()
    {
        var (org, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var (par, _) = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await org.CreateEventAsync();
        var res = await par.PostAsJsonAsync($"/api/events/{eventId}/categories", new { name = "Hack", categoryType = "Distance", distanceKm = 5 });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Category_WithInvalidData_ReturnsBadRequest()
    {
        var (org, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var eventId = await org.CreateEventAsync();
        var res = await org.PostAsJsonAsync($"/api/events/{eventId}/categories", new { name = "10km", categoryType = "Distance" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
