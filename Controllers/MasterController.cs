using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using SamaSamaLAN.Core;

namespace SamaSamaLAN.Controllers
{
    /// <summary>
    /// Route for master controller
    /// </summary>
    public static class MasterController
    {
        public const string StopGamePath = "/stopgame";
        public const string CreateGamePath = "/creategame";
        public const string StartGamePath = "/startgame";
        public const string MasterPath = "/master";

        /// <summary>
        /// Configuration of app
        /// </summary>
        public static void Configure(IAppBuilder app)
        {
            app.Map(MasterPath, config =>
            {
                config.Run(context =>
                {
                    return HandleMaster(context);
                });
            });

            app.Map(StartGamePath, config =>
            {
                config.Run(context =>
                {
                    return HandleStartGame(context);
                });
            });

            app.Map(StopGamePath, config =>
            {
                config.Run(context =>
                {
                    return HandleStopGame(context);
                });
            });

            app.Map(CreateGamePath, config =>
            {
                config.Run(context =>
                {
                    return HandleCreateGame(context);
                });
            });
        }

        /// <summary>
        /// Creates a game
        /// </summary>
        public static Task HandleCreateGame(IOwinContext context)
        {
            var createdGame = new GameInstance();

            context.Response.Redirect(MasterPath);
            return context.Response.WriteAsync(string.Empty);
        }

        /// <summary>
        /// Handles the start of the game
        /// </summary>
        public static Task HandleStartGame(IOwinContext context)
        {
            context.Response.ContentType = "text/html";
            var response = new StringBuilder();

            int gameNumber;
            GameInstance game;
            if (int.TryParse(context.Request.Query["gamenumber"], out gameNumber) && GameInstance.Instances.TryGetValue(gameNumber, out game))
            {
                var word1 = context.Request.Query["word1"];
                var word2 = context.Request.Query["word2"];
                if (!string.IsNullOrWhiteSpace(word1) && !string.IsNullOrWhiteSpace(word2))
                {
                    // Start the game!
                    game.Votes.Clear();
                    game.State = GameState.Playing;
                    game.MajorityWord = word1;
                    game.MinorityWord = word2;

                    foreach (var player in game.Players)
                    {
                        player.GuessesLeft = 2;
                        player.IsAlive = true;
                        player.Word = game.MajorityWord;
                    }

                    game.MinorityCount = 1;
                    for (int i = 5; i <= game.Players.Count; i = i + 3)
                    {
                        game.MinorityCount++;
                    }

                    while (game.Players.Count(p => p.Word == game.MinorityWord) < game.MinorityCount)
                    {
                        var randomNumber = new Random().Next(0, game.Players.Count);
                        game.Players[randomNumber].Word = game.MinorityWord;
                    }

                    context.Response.Redirect(MasterPath + "?gamenumber=" + gameNumber);
                    return context.Response.WriteAsync(string.Empty);
                }

                response.Append("<br><u><b>Players</u></b>");
                foreach (var player in game.Players)
                {
                    response.Append("<br>" + player.Name);
                }

                response.Append("<br><form action='" + StartGamePath + "' method='get'>");
                response.Append("Majority Word:<br><input type='text' name='word1'><br>");
                response.Append("Minority Word:<br><input type='text' name='word2'><br>");
                response.Append("<input type='hidden' value='" + gameNumber + "' name='gamenumber'/>");
                response.Append("<input type='submit' value='START GAME!'></form>");

                Utils.SetRefresh(10, response);
                return context.Response.WriteAsync(response.ToString());
            }

            context.Response.Redirect(MasterPath);
            return context.Response.WriteAsync(string.Empty);
        }

        /// <summary>
        /// Stop a game
        /// </summary>
        public static Task HandleStopGame(IOwinContext context)
        {
            context.Response.ContentType = "text/html";
            var response = new StringBuilder();

            int gameNumber;
            GameInstance game;
            if (int.TryParse(context.Request.Query["gamenumber"], out gameNumber) && GameInstance.Instances.TryGetValue(gameNumber, out game))
            {
                foreach (var player in game.Players)
                {
                    player.IsAlive = false;
                    player.Game = null;
                }

                game.Players.Clear();
                GameInstance.Instances.Remove(game.GameNumber);
            }

            context.Response.Redirect(MasterPath);
            return context.Response.WriteAsync(string.Empty);
        }

        /// <summary>
        /// The master controller
        /// </summary>
        public static Task HandleMaster(IOwinContext context)
        {
            context.Response.ContentType = "text/html";
            var response = new StringBuilder();

            int gameNumber;
            GameInstance game;
            if (int.TryParse(context.Request.Query["gamenumber"], out gameNumber) && GameInstance.Instances.TryGetValue(gameNumber, out game))
            {
                // Render game-specific stuff
                switch (game.State)
                {
                    case GameState.Ending:
                        response.Append("<br><a href='" + StopGamePath + "?gamenumber=" + game.GameNumber + "'>End Game</a>");
                        break;

                    case GameState.SettingUp:
                        response.Append("<br><u><b>Players</u></b>");
                        foreach (var player in game.Players)
                        {
                            response.Append("<br>" + player.Name);
                        }

                        response.Append("<br><br><a href='" + StartGamePath + "?gamenumber=" + game.GameNumber + "'>Start Game</a>");
                        break;

                    case GameState.Playing:
                        response.Append("<br><u><b>Players</u></b>");
                        foreach (var player in game.Players)
                        {
                            response.Append("<br>" + player.Name + " - Word: " + player.Word + " - Is Alive: " + player.IsAlive);
                        }

                        response.Append("<br><br><a href='" + VotingController.StartVotingPath + "?gamenumber=" + game.GameNumber + "'>Start Voting</a>");
                        break;

                    case GameState.Voting:
                        response.Append("<br><u><b>Votes</u></b>");
                        foreach (var player in game.Players)
                        {
                            if (game.Votes.ContainsKey(player))
                            {
                                response.Append("<br>" + player.Name + " votes for " + game.Votes[player].Name);
                            }
                            else
                            {
                                response.Append("<br>" + player.Name + " hasn't voted yet.");
                            }
                        }

                        response.Append("<br><br><a href='" + VotingController.FinishVotingPath + "?gamenumber=" + game.GameNumber + "'>Finish Voting</a>");
                        break;

                }

                Utils.SetRefresh(10, response);
                response.Append("<br><br><a href='" + StopGamePath + "?gamenumber=" + game.GameNumber + "'>TERMINATE GAME</a>");
                response.Append("<br><br><a href='" + MasterPath + "'>RETURN</a>");

                return context.Response.WriteAsync(response.ToString());
            }

            // Show list of games
            foreach (var gameInList in GameInstance.Instances.Values)
            {
                response.Append("<br><a href='" + MasterPath + "?gamenumber=" + gameInList.GameNumber + "'>GAME #" + gameInList.GameNumber + "</a>");
                response.Append(" - STATE: " + gameInList.State.ToString());
            }

            response.Append("<br><br><a href='" + CreateGamePath + "'>CREATE GAME</a>");
            Utils.SetRefresh(10, response);
            return context.Response.WriteAsync(response.ToString());
        }
    }
}
