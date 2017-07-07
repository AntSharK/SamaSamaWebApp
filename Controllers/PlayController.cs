using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using SamaSamaLAN.Core;

namespace SamaSamaLAN.Controllers
{
    /// <summary>
    /// Controller for playing the game
    /// </summary>
    public static class PlayController
    {
        public static string PlayPath = "/play";
        public static string PathEnd = "/end";
        public static string PathLeave = "/leave";

        /// <summary>
        /// Configuration of app
        /// </summary>
        public static void Configure(IAppBuilder app)
        {
            app.Map(PlayPath, config =>
            {
                config.Run(context =>
                {
                    return HandlePlay(context);
                });
            });

            app.Map(PathEnd, config =>
            {
                config.Run(context =>
                {
                    return HandleEnd(context);
                });
            });

            app.Map(PathLeave, config =>
            {
                config.Run(context =>
                {
                    return HandleLeave(context);
                });
            });
        }

        /// <summary>
        /// Leave current game
        /// </summary>
        public static Task HandleLeave(IOwinContext context)
        {
            context.Response.ContentType = "text/html";
            var response = new StringBuilder();

            if (Utils.GetUser(context))
            {
                var userName = context.Request.User.Identity.Name;
                if (Player.Players.ContainsKey(userName))
                {
                    var player = Player.Players[userName];
                    if (player.Game != null && player.Game.State == GameState.SettingUp)
                    {
                        player.Leave();
                    }
                }
            }

            context.Response.Redirect(JoinController.JoinPath);
            return context.Response.WriteAsync(string.Empty);
        }

        /// <summary>
        /// Handle the end of the game
        /// </summary>
        public static Task HandleEnd(IOwinContext context)
        {
            context.Response.ContentType = "text/html";
            var response = new StringBuilder();

            if (Utils.GetUser(context))
            {
                var userName = context.Request.User.Identity.Name;
                if (Player.Players.ContainsKey(userName))
                {
                    var player = Player.Players[userName];
                    if (player.Game != null && player.Game.State == GameState.Ending)
                    {
                        response.Append(player.Game.EndMessage);
                        response.Append("<br><br><u><b>Minority Word: " + player.Game.MinorityWord + "</u></b>");
                        foreach (var playerToDisplay in player.Game.Players.Where(p => p.Word == player.Game.MinorityWord))
                        {
                            response.Append("<br>" + playerToDisplay.Name + " - STATUS: " + (playerToDisplay.IsAlive ? "ALIVE" : "DEAD"));
                        }

                        response.Append("<br><br><u><b>Majority Word: " + player.Game.MajorityWord + "</u></b>");
                        foreach (var playerToDisplay in player.Game.Players.Where(p => p.Word == player.Game.MajorityWord))
                        {
                            response.Append("<br>" + playerToDisplay.Name + " - STATUS: " + (playerToDisplay.IsAlive ? "ALIVE" : "DEAD"));
                        }

                        response.Append("<br><br><a href='" + JoinController.JoinPath + "'>JOIN AGAIN!</a>");
                        return context.Response.WriteAsync(response.ToString());
                    }
                }
            }
            
            response.Append("<br><a href='/play'>Return to game</a>");
            return context.Response.WriteAsync(response.ToString());
        }

        /// <summary>
        /// Handles playing
        /// </summary>
        public static Task HandlePlay(IOwinContext context)
        {
            context.Response.ContentType = "text/html";
            var response = new StringBuilder();

            if (Utils.GetUser(context))
            {
                var userName = context.Request.User.Identity.Name;
                if (Player.Players.ContainsKey(userName))
                {
                    var player = Player.Players[userName];
                    response.Append("You are: <b>" + player.Name + "</b>");
                    
                    if (player.Game != null)
                    {
                        if (!player.IsAlive && player.Game.State != GameState.SettingUp)
                        {
                            response.Append("<br>You are dead." + Utils.GetDeadMessage());
                        }

                        switch (player.Game.State)
                        {
                            case GameState.SettingUp:
                                response.Append("<br><br>Waiting for game to start...");
                                response.Append("<br><a href='" + PathLeave + "'>Leave</a>");
                                break;
                            case GameState.Ending:
                                context.Response.Redirect(PlayController.PathEnd);
                                break;
                            case GameState.Voting:
                                context.Response.Redirect(VotingController.PlayerVotingPath);
                                break;
                            case GameState.Playing:
                                response.Append("<br><h1>Your word is: <u>" + player.Word + "</u></h1>");
                                response.Append("<br><u>Players still playing</u><br>");
                                foreach (var gamePlayer in player.Game.Players.Where(p => p.IsAlive))
                                {
                                    response.Append(gamePlayer.Name + "</br>");
                                }
                                break;
                        }

                        Instructions(response, player.Game.MinorityCount);
                        Utils.SetRefresh(8, response);
                        return context.Response.WriteAsync(response.ToString());
                    }
                }
            }

            context.Response.Redirect(JoinController.JoinPath);
            return context.Response.WriteAsync(string.Empty);
        }

        /// <summary>
        /// Sets the instructions
        /// </summary>
        private static void Instructions(StringBuilder response, int minorityCount)
        {
            response.AppendLine("<br><br><u>Instructions</u>");
            response.AppendLine("<br>You have been given a word. Everyone in the game has been given the same word, except for " + minorityCount + " players.");
            response.AppendLine("<br>Every round, you have to state something true about your word. You cannot repeat what has already been said.");
            response.AppendLine("<br>At the end of every round, everyone votes to eliminate players they think are on the opposing team.");
            response.AppendLine("<br>The player with the most number of votes is eliminated. In a tie, they all players with the most number of votes all eliminated.");
            response.AppendLine("<br><u>Victory condition:</u> The team that starts out with the minority word wins once they have the same number of surviving players as the majority.");
            response.AppendLine("<br><u>Victory condition:</u> The team that starts out with the majority word wins by eliminating the other team.");
            response.AppendLine("<br><u>Victory condition:</u> During voting, you can guess the opposing team's word. Guessing correctly instantly wins you the game.<br><br>");
        }
    }
}
