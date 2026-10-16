using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

public class EnrolmentAndResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;
    public EnrolmentAndResultTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecordedCorrectly()
    {
        var (org, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var (par, _) = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await org.CreateEventAsync();
        var catId = await org.CreateCategoryAsync(eventId);

        var res = await par.PostAsJsonAsync("/api/enrolments", new { eventId, categoryId = catId });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var created = await res.ReadJsonAsync();
        Assert.Equal(eventId, created.GetProperty("eventId").GetInt32());
        Assert.Equal(catId, created.GetProperty("categoryId").GetInt32());

        // participant sees it in their own list
        var mine = await (await par.GetAsync("/api/enrolments/my")).ReadJsonAsync();
        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(created.GetProperty("participantId").GetInt32(), mine[0].GetProperty("participantId").GetInt32());

        // organiser sees it for their event
        var forEvent = await (await org.GetAsync($"/api/events/{eventId}/enrolments")).ReadJsonAsync();
        Assert.Equal(1, forEvent.GetArrayLength());
        Assert.Equal(catId, forEvent[0].GetProperty("categoryId").GetInt32());
    }

    [Fact]
    public async Task Enrol_Twice_ForSameEvent_ReturnsConflict()
    {
        var (org, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var (par, _) = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await org.CreateEventAsync();
        var catId = await org.CreateCategoryAsync(eventId);

        await par.PostAsJsonAsync("/api/enrolments", new { eventId, categoryId = catId });
        var again = await par.PostAsJsonAsync("/api/enrolments", new { eventId, categoryId = catId });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithCategoryFromAnotherEvent_ReturnsBadRequest()
    {
        var (org, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var (par, _) = await _factory.RegisterAndLoginAsync("Participant");
        var eventA = await org.CreateEventAsync("A");
        var eventB = await org.CreateEventAsync("B");
        var catB = await org.CreateCategoryAsync(eventB);

        var res = await par.PostAsJsonAsync("/api/enrolments", new { eventId = eventA, categoryId = catB });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Enrol_InMissingEvent_ReturnsNotFound()
    {
        var (par, _) = await _factory.RegisterAndLoginAsync("Participant");
        var res = await par.PostAsJsonAsync("/api/enrolments", new { eventId = 987654, categoryId = 1 });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_AndUnauthenticatedCannotEnrol()
    {
        var (org, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var eventId = await org.CreateEventAsync();
        var catId = await org.CreateCategoryAsync(eventId);

        Assert.Equal(HttpStatusCode.Forbidden, (await org.PostAsJsonAsync("/api/enrolments", new { eventId, categoryId = catId })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().PostAsJsonAsync("/api/enrolments", new { eventId, categoryId = catId })).StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments_OrganiserCannotViewMyEnrolments()
    {
        var (org, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var (par, _) = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await org.CreateEventAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await par.GetAsync($"/api/events/{eventId}/enrolments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await org.GetAsync("/api/enrolments/my")).StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotViewEnrolments_OfEventTheyDoNotOwn()
    {
        var (owner, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var (other, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var eventId = await owner.CreateEventAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/events/{eventId}/enrolments")).StatusCode);
    }

    [Fact]
    public async Task Participant_CanWithdraw_OwnEnrolment()
    {
        var (org, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var (par, _) = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await org.CreateEventAsync();
        var catId = await org.CreateCategoryAsync(eventId);
        var enrolId = (await (await par.PostAsJsonAsync("/api/enrolments", new { eventId, categoryId = catId })).ReadJsonAsync()).GetProperty("enrolmentId").GetInt32();

        Assert.Equal(HttpStatusCode.NoContent, (await par.DeleteAsync($"/api/enrolments/{enrolId}")).StatusCode);
        Assert.Equal(0, (await (await par.GetAsync("/api/enrolments/my")).ReadJsonAsync()).GetArrayLength());
    }

    // ---------------- Results ----------------

    private async Task<(HttpClient org, HttpClient par, int eventId, int enrolId)> SetupEnrolmentAsync()
    {
        var (org, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var (par, _) = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await org.CreateEventAsync();
        var catId = await org.CreateCategoryAsync(eventId);
        var enrol = await (await par.PostAsJsonAsync("/api/enrolments", new { eventId, categoryId = catId })).ReadJsonAsync();
        return (org, par, eventId, enrol.GetProperty("enrolmentId").GetInt32());
    }

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantCanViewOwnResult()
    {
        var (org, par, eventId, enrolId) = await SetupEnrolmentAsync();

        var create = await org.PostAsJsonAsync("/api/results", new { enrolmentId = enrolId, finishTime = "00:48:12", position = 3 });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var mine = await (await par.GetAsync("/api/results/my")).ReadJsonAsync();
        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(3, mine[0].GetProperty("position").GetInt32());
        Assert.Equal("00:48:12", mine[0].GetProperty("finishTime").GetString());

        var all = await (await org.GetAsync($"/api/events/{eventId}/results")).ReadJsonAsync();
        Assert.Equal(1, all.GetArrayLength());
    }

    [Fact]
    public async Task Participant_CannotCaptureResults()
    {
        var (_, par, _, enrolId) = await SetupEnrolmentAsync();
        var res = await par.PostAsJsonAsync("/api/results", new { enrolmentId = enrolId, finishTime = "00:40:00", position = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureResult_ForEventTheyDoNotOwn()
    {
        var (_, _, _, enrolId) = await SetupEnrolmentAsync();
        var (stranger, _) = await _factory.RegisterAndLoginAsync("Organiser");
        var res = await stranger.PostAsJsonAsync("/api/results", new { enrolmentId = enrolId, finishTime = "00:40:00", position = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task DuplicateResult_ReturnsConflict_AndInvalidResult_ReturnsBadRequest()
    {
        var (org, _, _, enrolId) = await SetupEnrolmentAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await org.PostAsJsonAsync("/api/results", new { enrolmentId = enrolId, finishTime = "00:00:00", position = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await org.PostAsJsonAsync("/api/results", new { enrolmentId = enrolId, finishTime = "00:30:00", position = 0 })).StatusCode);

        Assert.Equal(HttpStatusCode.Created, (await org.PostAsJsonAsync("/api/results", new { enrolmentId = enrolId, finishTime = "00:30:00", position = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await org.PostAsJsonAsync("/api/results", new { enrolmentId = enrolId, finishTime = "00:31:00", position = 2 })).StatusCode);
    }

    [Fact]
    public async Task Participant_OnlySeesOwnResults()
    {
        var (org, par, _, enrolId) = await SetupEnrolmentAsync();
        await org.PostAsJsonAsync("/api/results", new { enrolmentId = enrolId, finishTime = "00:30:00", position = 1 });

        var (otherPar, _) = await _factory.RegisterAndLoginAsync("Participant");
        Assert.Equal(0, (await (await otherPar.GetAsync("/api/results/my")).ReadJsonAsync()).GetArrayLength());
        Assert.Equal(1, (await (await par.GetAsync("/api/results/my")).ReadJsonAsync()).GetArrayLength());
    }

    [Fact]
    public async Task Organiser_CanCorrectResult()
    {
        var (org, _, _, enrolId) = await SetupEnrolmentAsync();
        var created = await (await org.PostAsJsonAsync("/api/results", new { enrolmentId = enrolId, finishTime = "00:30:00", position = 5 })).ReadJsonAsync();
        var id = created.GetProperty("resultId").GetInt32();

        var res = await org.PutAsJsonAsync($"/api/results/{id}", new { finishTime = "00:29:00", position = 4 });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(4, (await res.ReadJsonAsync()).GetProperty("position").GetInt32());
    }

    [Fact]
    public async Task Results_Endpoints_RequireAuthentication()
    {
        var anon = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/results/my")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsJsonAsync("/api/results", new { enrolmentId = 1, finishTime = "00:30:00", position = 1 })).StatusCode);
    }
}
