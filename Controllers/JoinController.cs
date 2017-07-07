using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using SamaSamaLAN.Core;

namespace SamaSamaLAN.Controllers
{
    /// <summary>
    /// Controller for the /join path
    /// </summary>
    public static class JoinController
    {
        public static string JoinPath = "/join";
        public static string SignupPath = "/signup";
        public static string EnterGamePath = "/entergame";

        /// <summary>
        /// Configuration of app
        /// </summary>
        public static void Configure(IAppBuilder app)
        {
            app.Map(JoinPath, config =>
            {
                config.Run(context =>
                {
                    return HandleJoin(context);
                });
            });

            app.Map(SignupPath, config =>
            {
                config.Run(context =>
                {
                    return HandleSignup(context);
                });
            });

            app.Map(EnterGamePath, config =>
            {
                config.Run(context =>
                {
                    return HandleEnterGame(context);
                });
            });
        }

        /// <summary>
        /// Handles entering the game
        /// </summary>
        public static Task HandleEnterGame(IOwinContext context)
        {
            context.Response.ContentType = "text/html";
            StringBuilder response = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(context.Request.Query["gamenumber"]) && Utils.GetUser(context))
            {
                int gameNumber;
                GameInstance game;
                if (int.TryParse(context.Request.Query["gamenumber"], out gameNumber) && GameInstance.Instances.TryGetValue(gameNumber, out game))
                {
                    var userName = context.Request.User.Identity.Name;

                    Player player;
                    if (Player.Players.ContainsKey(userName))
                    {
                        player = Player.Players[userName];
                        player.Join(game);
                    }
                    else
                    {
                        player = new Player(userName, game);
                    }

                    context.Response.Redirect(PlayController.PlayPath);
                    return context.Response.WriteAsync(response.ToString());
                }
            }

            // Invalid entry or no user found
            context.Response.Redirect(SignupPath);
            return context.Response.WriteAsync(string.Empty);
        }

        /// <summary>
        /// Lobby for players to join games
        /// </summary>
        public static Task HandleJoin(IOwinContext context)
        {
            context.Response.ContentType = "text/html";
            StringBuilder response = new StringBuilder();

            if (!Utils.GetUser(context))
            {
                GenerateSignupForm(response);
                return context.Response.WriteAsync(response.ToString());
            }

            // Authenticated
            var userName = context.Request.User.Identity.Name;
            response.Append("Currently 'authenticated' as: <u><i>" + userName + "</u></i>");

            // If player exists in a game
            if (Player.Players.ContainsKey(userName))
            {
                var player = Player.Players[userName];
                if (player.Game != null && player.Game.State != GameState.Ending && player.Game.State != GameState.SettingUp && player.IsAlive)
                {
                    response.Append("<br>You are currently in a game.<br>");
                    response.Append("<br><a href='" + PlayController.PlayPath + "'>Return to game</a>");
                    return context.Response.WriteAsync(response.ToString());
                }
            }

            response.Append("<br><h2><u>Current Games:</u></h2>");
            foreach (var game in GameInstance.Instances)
            {
                var gameNumber = game.Key;
                var gameState = game.Value.State;

                if (gameState == GameState.SettingUp)
                {
                    response.Append("<a href='" + EnterGamePath + "?gamenumber=" + gameNumber + "'>Join Game #" + gameNumber + "</a/><br>");
                }
            }

            GenerateSignupForm(response, "Change Name");
            Utils.SetRefresh(10, response);

            return context.Response.WriteAsync(response.ToString());
        }

        /// <summary>
        /// Handles signing up
        /// </summary>
        public static Task HandleSignup(IOwinContext context)
        {
            context.Response.ContentType = "text/html";
            StringBuilder response = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(context.Request.Query["username"]))
            {
                var newUserName = context.Request.Query["username"];
                bool nameTaken = false;

                foreach (var playerName in Player.Players.Keys)
                {
                    if (playerName.Equals(newUserName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        nameTaken = true;
                        break;
                    }
                }

                if (nameTaken)
                {
                    response.Append("Username <b><u>" + newUserName + "</b></u> has been taken.<br>");
                }
                else
                {
                    Utils.SetUser(context, newUserName);
                    context.Response.Redirect(JoinPath);
                    return context.Response.WriteAsync(string.Empty);
                }
            }

            GenerateSignupForm(response);
            return context.Response.WriteAsync(response.ToString());
        }

        /// <summary>
        /// Generates the sign up form
        /// </summary>
        private static void GenerateSignupForm(StringBuilder response, string signupField = "SIGN UP!")
        {
            response.Append("<br><form action='" + SignupPath + "' method='get'>Username:<br><input type='text' name='username'><br><input type='submit' value='" + signupField + "'></form>");
        }

    }
}
