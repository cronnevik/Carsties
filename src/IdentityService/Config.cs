using Duende.IdentityServer.Models;

namespace IdentityService;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new ApiScope("auctionApp", "Auction app full access"),
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            // For Postman
            new Client
            {
                // Two tokens - Id token and Access token
                // This config is just for development - less secure
                ClientId = "postman",
                ClientName = "Postman",
                AllowedScopes = { "openid", "profile", "auctionApp" },
                RedirectUris = {"https://www.getpostman.com/oauth2/callback"}, // is required, but not needed for this application
                ClientSecrets = new[] { new Secret("NotASecret".Sha256()) },
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword
            },

            // For the nextjs app
            new Client
            {
                ClientId = "nextApp",
                ClientName = "nextApp",
                ClientSecrets = new[] { new Secret("NotASecret".Sha256()) },
                AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                RequirePkce = false ,// for mobile apps
                RedirectUris = {"http://localhost:3000/api/auth/callback/id-server"},
                AllowOfflineAccess = true, // for refresh token functionality
                AllowedScopes = { "openid", "profile", "auctionApp" },
                AccessTokenLifetime = 3600*24*30,  // default is 3600 seconds (1h). We extend to a month for dev purpose
                AlwaysIncludeUserClaimsInIdToken = true
            }
        };

}
