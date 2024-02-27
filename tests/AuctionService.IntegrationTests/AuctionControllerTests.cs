
using System.Net;
using System.Net.Http.Json;
using AuctionService.Data;
using AuctionService.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests;

[Collection("Shared collection")]
public class AuctionControllerTests : IAsyncLifetime
{
    private readonly CustomWebAppFactory _factory;
    private readonly HttpClient _httpClient;
    private const string _gT_ID = "afbee524-5972-4075-8800-7d1f9d7b0a0c";

    // constructor to get access to the custom web app factory
    public AuctionControllerTests(CustomWebAppFactory factory)
    {
        _factory = factory;
        _httpClient = factory.CreateClient();
    }

    // Before each test
    public Task InitializeAsync() => Task.CompletedTask; // not gonna initialize anything in here, thus completedtask

    // After each test - Reinitialize db after
    public Task DisposeAsync()
    {
        // need to get access to the scope the test is running in to get hold of the database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        DbHelper.ReinitDbForTest(db);
        return Task.CompletedTask;
    }

    private static CreateAuctionDto GetAuctionForCreate()
    {
        return new CreateAuctionDto
        {
            Make = "test",
            Model = "testModel",
            ImageUrl = "test",
            Color = "test",
            Mileage = 10,
            Year = 10,
            ReservePrice = 10
        };
    }

    [Fact]
    public async Task GetAuctions_ShouldReturn3Auctions()
    {
        // arrange is taken care of in the setup of CustomWebAppFactory

        // act
        var response = await _httpClient.GetFromJsonAsync<List<AuctionDto>>("api/auctions");

        // assert
        Assert.Equal(3, response.Count);
    }

    [Fact]
    public async Task GetAuctionById_WithValidId_ShouldReturnAuction()
    {
        // act
        var response = await _httpClient.GetFromJsonAsync<AuctionDto>($"api/auctions/{_gT_ID}");

        // assert
        Assert.Equal("GT", response.Model);
    }

    [Fact]
    public async Task GetAuctionById_WithInvalidId_ShouldReturn404()
    {
        // act
        var response = await _httpClient.GetAsync($"api/auctions/{Guid.NewGuid()}");

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAuctionById_WithInvalidGuid_ShouldReturn400()
    {
        // act
        var response = await _httpClient.GetAsync("api/auctions/notaguid");

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAuction_WithNoAuth_ShouldReturn401()
    {
        // arrange
        var auction = new CreateAuctionDto{Make = "test"};

        // act
        var response = await _httpClient.PostAsJsonAsync("api/auctions", auction);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAuction_WithAuth_ShouldReturn201()
    {
        // arrange
        var auction = GetAuctionForCreate();
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // act
        var response = await _httpClient.PostAsJsonAsync("api/auctions", auction);

        // assert
        response.EnsureSuccessStatusCode(); // ensure a valid request
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdAuction = await response.Content.ReadFromJsonAsync<AuctionDto>();
        Assert.Equal("bob", createdAuction.Seller);
    }

    [Fact]
    public async Task CreateAuction_WithInvalidCreateAuctionDto_ShouldReturn400()
    {
        // arrange
        var auction = GetAuctionForCreate();
        auction.Make = null;
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // act
        var response = await _httpClient.PostAsJsonAsync("api/auctions", auction);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuction_WithValidUpdateDtoAndUser_ShouldReturn200()
    {
        // arrange
        var updateAuction = new UpdateAuctionDto { Make = "Updated"};
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // act
        var response = await _httpClient.PutAsJsonAsync($"api/auctions/{_gT_ID}", updateAuction);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuction_WithValidUpdateDtoAndInvalidUser_ShouldReturn403()
    {
        // arrange
        var updateAuction = new UpdateAuctionDto { Make = "Updated"};
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("notbob"));

        // act
        var response = await _httpClient.PutAsJsonAsync($"api/auctions/{_gT_ID}", updateAuction);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

}
