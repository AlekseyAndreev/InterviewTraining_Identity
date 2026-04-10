using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace SevSharks.Identity.WebUI.ExternalAuthentication.VkConnection
{
    /// <summary>
    /// VkAuthenticationHandler
    /// </summary>
    public class VkAuthenticationHandler : OAuthHandler<VkAuthenticationOptions>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public VkAuthenticationHandler(IOptionsMonitor<VkAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
        { }

        /// <summary>
        /// CreateTicketAsync
        /// </summary>
        protected override async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
        {
            var address = QueryHelpers.AddQueryString(Options.UserInformationEndpoint, "access_token", tokens.AccessToken);

            address = QueryHelpers.AddQueryString(address, "v", Options.ApiVersion);

            if (Options.Fields.Count != 0)
            {
                address = QueryHelpers.AddQueryString(address, "fields", string.Join(",", Options.Fields));
            }

            var response = await Backchannel.GetAsync(address, Context.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("An error occurred while retrieving the user profile: the remote server " +
                                "returned a {Status} response with the following payload: {Headers} {Body}.",
                                response.StatusCode, // Status
                                response.Headers.ToString(), // Headers:
                                await response.Content.ReadAsStringAsync()); //Body:

                throw new HttpRequestException("An error occurred while retrieving the user profile.");
            }

            var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
            var user = (JObject)payload["response"][0];
            var tokensResponse = tokens.Response;
            foreach (var scope in Options.Scope)
            {
                var scopeProp = tokensResponse.RootElement.GetProperty(scope);
                var propertyText = scopeProp.GetString();
                if (!string.IsNullOrEmpty(propertyText))
                {
                    user.Add(scope, propertyText);
                }
            }
            var userJsonString = user.ToString();
            using JsonDocument document = JsonDocument.Parse(userJsonString);
            JsonElement root = document.RootElement;

            var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, root);

            context.RunClaimActions();

            await Options.Events.CreatingTicket(context);

            return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);
        }

        /*
    /// <summary>
    /// 
    /// </summary>
    protected override Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
    {
        var oauthHandler = this;
        return Task.FromResult(HandleRequestResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), "test")));
        var query = oauthHandler.Request.Query;
        var properties = new AuthenticationProperties
        {
            Items =
            {
                {"LoginProvider", Options.ClaimsIssuer}
            }
        };
        var redirectValue = "/Account/ExternalLoginCallback";
        if (query.ContainsKey(SiteReturnAdditional))
        {
            redirectValue = query[SiteReturnAdditional];
            //properties = oauthHandler.Options.StateDataFormat.Unprotect();
        }
        properties.Items.Add(RedirectName, redirectValue);
        var error = query["error"];
        if (!StringValues.IsNullOrEmpty(error))
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(error);
            var stringValues3 = query["error_description"];
            if (!StringValues.IsNullOrEmpty(stringValues3))
            {
                stringBuilder.Append(";Description=").Append(stringValues3);
            }

            var stringValues4 = query["error_uri"];
            if (!StringValues.IsNullOrEmpty(stringValues4))
            {
                stringBuilder.Append(";Uri=").Append(stringValues4);
            }

            return HandleRequestResult.Fail(stringBuilder.ToString(), properties);
        }
        var code = query["code"];
        if (StringValues.IsNullOrEmpty(code))
        {
            return HandleRequestResult.Fail("Code was not found.", properties);
        }

        var tokens = await GetTokenByAuthCodeAsync(code);
        if (tokens.Error != null)
        {
            return HandleRequestResult.Fail(tokens.Error, properties);
        }

        if (string.IsNullOrEmpty(tokens.AccessToken))
        {
            return HandleRequestResult.Fail("Failed to retrieve access token.", properties);
        }

        var identity = new ClaimsIdentity(oauthHandler.ClaimsIssuer);
        if (oauthHandler.Options.SaveTokens)
        {
            var authenticationTokenList = new List<AuthenticationToken>
            {
                new AuthenticationToken
                {
                    Name = "access_token",
                    Value = tokens.AccessToken
                }
            };
            if (!string.IsNullOrEmpty(tokens.RefreshToken))
            {
                authenticationTokenList.Add(new AuthenticationToken
                {
                    Name = "refresh_token",
                    Value = tokens.RefreshToken
                });
            }

            if (!string.IsNullOrEmpty(tokens.TokenType))
            {
                authenticationTokenList.Add(new AuthenticationToken
                {
                    Name = "token_type",
                    Value = tokens.TokenType
                });
            }

            if (!string.IsNullOrEmpty(tokens.ExpiresIn) && int.TryParse(tokens.ExpiresIn, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                var dateTimeOffset = oauthHandler.Clock.UtcNow + TimeSpan.FromSeconds(result);
                authenticationTokenList.Add(new AuthenticationToken
                {
                    Name = "expires_at",
                    Value = dateTimeOffset.ToString("o", CultureInfo.InvariantCulture)
                });
            }
            properties.StoreTokens(authenticationTokenList);
        }
        var ticketAsync = await oauthHandler.CreateTicketAsync(identity, properties, tokens);
        return ticketAsync == null ?
            HandleRequestResult.Fail("Failed to retrieve user information from remote server.", properties) :
            HandleRequestResult.Success(ticketAsync);
    }
        */
        /*
        private async Task<OAuthTokenResponse> GetTokenByAuthCodeAsync(string authCode)
        {
            if (string.IsNullOrEmpty(authCode))
            {
                throw new ArgumentNullException(nameof(authCode));
            }

            var timestamp = GetTimeStamp();
            var scopes = FormatScope(Options.Scope);
            var state = GetState();
            var signMessage = $"{scopes}{timestamp}{Options.ClientId}{state}";
            var clientSecret = SignString(signMessage).Result;

            var requestParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", Options.ClientId),
                new KeyValuePair<string, string>("code", authCode),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("state", state),
                new KeyValuePair<string, string>("scope", scopes),
                new KeyValuePair<string, string>("timestamp", timestamp),
                new KeyValuePair<string, string>("token_type", "Bearer"),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("redirect_uri", Options.CallbackPath)
            };

            var requestContent = new FormUrlEncodedContent(requestParameters);

            try
            {
                using (var response = await Backchannel.PostAsync(Options.TokenEndpoint, requestContent, Context.RequestAborted))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"ЕСИА вернул неуспешный код code: '{response.StatusCode}'.");
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    return OAuthTokenResponse.Success(JObject.Parse(content));
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }*/
    }
}