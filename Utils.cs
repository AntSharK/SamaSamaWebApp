using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace SamaSamaLAN
{
    /// <summary>
    /// Utility classes
    /// </summary>
    public static class Utils
    {
        private const string userCookieName = "supersecretusercookie";

        /// <summary>
        /// Appends a line to auto refresh
        /// </summary>
        public static void SetRefresh(int refreshInterval, StringBuilder sb)
        {
            sb.Append("<meta http-equiv='refresh' content='" + refreshInterval + "'><br>This page autorefreshes because AJAX is hard...");
        }

        /// <summary>
        /// Sets the user in the context
        /// </summary>
        public static void SetUser(IOwinContext context, string userName)
        {
            var user = new GenericIdentity(userName);
            context.Request.User = new GenericPrincipal(user, new string[] { });

            context.Response.Cookies.Append(userCookieName, userName);
        }

        /// <summary>
        /// Gets the user from the context
        /// </summary>
        public static bool GetUser(IOwinContext context)
        {
            if (context.Request.User != null)
            {
                return true;
            }

            var userName = context.Request.Cookies[userCookieName];
            if (!string.IsNullOrEmpty(userName))
            {
                var user = new GenericIdentity(userName);
                context.Request.User = new GenericPrincipal(user, new string[] { });
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a message for dead players
        /// </summary>
        public static string GetDeadMessage()
        {
            var random = new Random().Next(0, 10);
            switch (random)
            {
                default:
                    return "Nobody likes you.";
            }
        }
    }
}
