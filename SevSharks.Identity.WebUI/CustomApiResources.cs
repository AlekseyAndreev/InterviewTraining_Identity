using Duende.IdentityModel;
using Duende.IdentityServer.Models;
using System.Collections.Generic;

namespace SevSharks.Identity.WebUI
{
    public static class CustomApiResources
    {
        public static class CustomScopes
        {
            public const string SignalrScopeName = "interview_training_signalr_web";
            public const string InterviewScopeName = "interview_training_interview";
        }

        public class SignalrScope : ApiScope
        {
            public SignalrScope()
            {
                Name = CustomScopes.SignalrScopeName;
                DisplayName = "Уведомления от Интервью";
                Required = true;
            }
        }

        public class SignalRApiResource : ApiResource
        {
            public SignalRApiResource()
            {
                Name = CustomScopes.SignalrScopeName;
                DisplayName = "SignalR Web Resource";
                Scopes = new List<string> { CustomScopes.SignalrScopeName };
                UserClaims = new[] { JwtClaimTypes.Role };
            }
        }

        public class InterviewScope : ApiScope
        {
            public InterviewScope()
            {
                Name = CustomScopes.InterviewScopeName;
                DisplayName = "Интервью";
                Required = true;
            }
        }

        public class InterviewApiResource : ApiResource
        {
            public InterviewApiResource()
            {
                Name = CustomScopes.InterviewScopeName;
                DisplayName = "Interview Web Resource";
                Scopes = new List<string> { CustomScopes.InterviewScopeName };
                UserClaims = new[] { JwtClaimTypes.Role };
            }
        }
    }
}
