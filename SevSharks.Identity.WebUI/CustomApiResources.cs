using Duende.IdentityServer.Models;
using System.Collections.Generic;

namespace SevSharks.Identity.WebUI
{
    public static class CustomApiResources
    {
        public static class CustomScopes
        {
            public const string SignalrScopeName = "tbt_signalr_web";
            public const string OrderScopeName = "tbt_order";
        }

        public class SignalrScope : ApiScope
        {
            public SignalrScope()
            {
                Name = CustomScopes.SignalrScopeName;
                DisplayName = "Уведомления от ВместеНаТакси";
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
            }
        }

        public class OrderScope : ApiScope
        {
            public OrderScope()
            {
                Name = CustomScopes.OrderScopeName;
                DisplayName = "Заказы от ВместеНаТакси";
                Required = true;
            }
        }

        public class OrderApiResource : ApiResource
        {
            public OrderApiResource()
            {
                Name = CustomScopes.OrderScopeName;
                DisplayName = "Order Web Resource";
                Scopes = new List<string> { CustomScopes.OrderScopeName };
            }
        }
    }
}
