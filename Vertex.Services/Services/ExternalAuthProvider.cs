using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using Vertex.Services.Models;

namespace Vertex.Services.Services
{
    public interface IExternalAuthProvider
    {
        Task<ExternalUserInfo> VerifyTokenAsync(string provider, string token);
    }

    public class ExternalAuthProvider : IExternalAuthProvider
    {
        private readonly ExternalAuthSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;

        public ExternalAuthProvider(IOptions<ExternalAuthSettings> settings, IHttpClientFactory httpClientFactory)
        {
            _settings = settings.Value;
            _httpClientFactory = httpClientFactory;
        }

        public Task<ExternalUserInfo> VerifyTokenAsync(string provider, string token)
        {
            return provider.ToLowerInvariant() switch
            {
                "google" => VerifyGoogleTokenAsync(token),
                "github" => VerifyGitHubTokenAsync(token),
                _ => throw new ArgumentException($"Unsupported auth provider: {provider}")
            };
        }

        private async Task<ExternalUserInfo> VerifyGoogleTokenAsync(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _settings.Google.ClientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                return new ExternalUserInfo(
                    ExternalId: payload.Subject,
                    Email: payload.Email,
                    Name: payload.Name ?? payload.Email.Split('@')[0],
                    AvatarUrl: payload.Picture
                );
            }
            catch (InvalidJwtException)
            {
                throw new UnauthorizedAccessException("Invalid Google token.");
            }
        }

        private async Task<ExternalUserInfo> VerifyGitHubTokenAsync(string tokenOrCode)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Vertex-App");

            string accessToken = tokenOrCode;

            // GitHub OAuth access tokens start with 'gho_'
            // If the token doesn't match this prefix, treat it as an authorization code and exchange it.
            if (!tokenOrCode.StartsWith("gho_") && !tokenOrCode.StartsWith("ghp_") && !tokenOrCode.StartsWith("github_"))
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = _settings.GitHub.ClientId,
                    ["client_secret"] = _settings.GitHub.ClientSecret,
                    ["code"] = tokenOrCode
                });

                var tokenResponse = await client.SendAsync(request);
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    throw new UnauthorizedAccessException("Failed to exchange GitHub authorization code.");
                }

                var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                using var tokenDoc = JsonDocument.Parse(tokenJson);
                if (tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenProp))
                {
                    accessToken = accessTokenProp.GetString() ?? throw new UnauthorizedAccessException("Access token not found in GitHub response.");
                }
                else if (tokenDoc.RootElement.TryGetProperty("error_description", out var errorProp))
                {
                    throw new UnauthorizedAccessException($"GitHub OAuth error: {errorProp.GetString()}");
                }
                else
                {
                    throw new UnauthorizedAccessException("Failed to exchange GitHub authorization code.");
                }
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Get user profile
            var response = await client.GetAsync("https://api.github.com/user");
            if (!response.IsSuccessStatusCode)
            {
                throw new UnauthorizedAccessException("Invalid GitHub token.");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var externalId = root.GetProperty("id").GetInt64().ToString();
            var name = root.TryGetProperty("name", out var nameProp) && nameProp.ValueKind != JsonValueKind.Null
                ? nameProp.GetString()
                : root.GetProperty("login").GetString();
            var avatarUrl = root.TryGetProperty("avatar_url", out var avatarProp) && avatarProp.ValueKind != JsonValueKind.Null
                ? avatarProp.GetString()
                : null;

            // Get primary email (may be private)
            var email = root.TryGetProperty("email", out var emailProp) && emailProp.ValueKind != JsonValueKind.Null
                ? emailProp.GetString()
                : null;

            if (string.IsNullOrEmpty(email))
            {
                // Fetch from /user/emails API
                var emailResponse = await client.GetAsync("https://api.github.com/user/emails");
                if (emailResponse.IsSuccessStatusCode)
                {
                    var emailJson = await emailResponse.Content.ReadAsStringAsync();
                    using var emailDoc = JsonDocument.Parse(emailJson);
                    foreach (var emailEntry in emailDoc.RootElement.EnumerateArray())
                    {
                        if (emailEntry.TryGetProperty("primary", out var primary) && primary.GetBoolean())
                        {
                            email = emailEntry.GetProperty("email").GetString();
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(email))
                {
                    throw new UnauthorizedAccessException("Unable to retrieve email from GitHub. Please make sure your email is public or grant email permission.");
                }
            }

            return new ExternalUserInfo(
                ExternalId: externalId,
                Email: email!,
                Name: name ?? "GitHub User",
                AvatarUrl: avatarUrl
            );
        }
    }
}
