using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using SamaSamaLAN.Core;

namespace SamaSamaLAN.Controllers
{
    /// <summary>
    /// Controller for voting
    /// </summary>
    public static class VotingController
    {
        public const string StartVotingPath = "/startvoting";
        public const string FinishVotingPath = "/finishvoting";
        public const string PlayerVotingPath = "/voting";
        public const string GuessPath = "/guess";

        /// <summary>
        /// Configuration of app
        /// </summary>
        public static void Configure(IAppBuilder app)
        {
            app.Map(GuessPath, config =>
            {
                config.Run(context =>
                {
                    return HandleGuess(context);
                });
            });

            app.Map(PlayerVotingPath, config =>
            {
                config.Run(context =>
                {
                    return HandlePlayerVoting(context);
                });
            });

            app.Map(FinishVotingPath, config =>
            {
                config.Run(context =>
                {
                    return HandleFinishVoting(context);
                });
            });

            app.Map(StartVotingPath, config =>
            {
                config.Run(context =>
                {
                    return HandleStartVoting(context);
                });
            });
        }

        /// <summary>
        /// Handle the end of voting
        /// </summary>
        public static Task HandleFinishVoting(IOwinContext context)
        {
            context.Response.ContentType = "text/html";
            var response = new StringBuilder();

            int gameNumber;
            GameInstance game;
            if (int.TryParse(context.Request.Query["gamenumber"], out gameNumber) && GameInstance.Instances.TryGetValue(gameNumber, out game))
            {
                // Tabulate votes
                Dictionary<Player, int> votes = new Dictionary<Player, int>();
                foreach (var player in game.Players)
                {
                    votes[player] = 0;
                }
                foreach (var playerVotes in game.Votes)
                {
                    votes[playerVotes.Value]++;
                }
                int maxVotes = 0;
                foreach (var voteCount in votes)
                {
                    if (voteCount.Value > maxVotes)
                    {
                        maxVotes = voteCount.Value;
                    }
                }

                foreach (var playerVotes in votes)
                {
                    if (playerVotes.Value == maxVotes)
                    {
                        playerVotes.Key.IsAlive = false;
                    }
                }

                // Figure out if anyone won
                int majorCount = 0;
                int minorCount = 0;
                foreach (var player in game.Players)
                {
                    if (player.IsAlive)
                    {
                        if (player.Word == game.MajorityWord)
                        {
                            majorCount++;
                        }
                        else if (player.Word == game.MinorityWord)
                        {
                            minorCount++;
                        }
                    }
                }

                if (minorCount >= majorCount)
                {
                    game.State = GameState.Ending;
                    var messageBuilder = new StringBuilder();
                    messageBuilder.Append("GAME OVER! The minority team won.");
                    messageBuilder.Append("<br><br><u><b>WINNING TEAM</u></b>");
                    foreach (var player in game.Players)
                    {
                        if (player.Word == game.MinorityWord)
                        {
                            messageBuilder.Append("<br>" + player.Name);
                        }
                    }

                    game.EndMessage = messageBuilder.ToString();
                }
                else if (minorCount == 0)
                {
                    game.State = GameState.Ending;
                    var messageBuilder = new StringBuilder();
                    messageBuilder.Append("GAME OVER! The majority team won.");
                    messageBuilder.Append("<br><br><u><b>WINNING TEAM</u></b>");
                    foreach (var player in game.Players)
                    {
                        if (player.Word == game.MajorityWord)
                        {
                            messageBuilder.Append("<br>" + player.Name);
                        }
                    }

                    game.EndMessage = messageBuilder.ToString();
                }

                context.Response.Redirect(MasterController.MasterPath + "?gamenumber=" + gameNumber);
                return context.Response.WriteAsync(string.Empty);
            }

            context.Response.Redirect(MasterController.MasterPath);
            return context.Response.WriteAsync(string.Empty);
        }

        /// <summary>
        /// Handles path to start voting process
        /// </summary>
        public static Task HandleStartVoting(IOwinContext context)
        {
            context.Response.ContentType = "text/html";
            var response = new StringBuilder();

            int gameNumber;
            GameInstance game;
            if (int.TryParse(context.Request.Query["gamenumber"], out gameNumber) && GameInstance.Instances.TryGetValue(gameNumber, out game))
            {
                game.State = GameState.Voting;
                game.Votes.Clear();

                context.Response.Redirect(MasterController.MasterPath + "?gamenumber=" + gameNumber);
                return context.Response.WriteAsync(string.Empty);
            }

            context.Response.Redirect(MasterController.MasterPath);
            return context.Response.WriteAsync(string.Empty);
        }

        /// <summary>
        /// Handle guessing of words
        /// </summary>
        public static Task HandleGuess(IOwinContext context)
        {
            context.Response.ContentType = "text/html";
            var response = new StringBuilder();

            if (Utils.GetUser(context))
            {
                var userName = context.Request.User.Identity.Name;
                if (Player.Players.ContainsKey(userName))
                {
                    var player = Player.Players[userName];
                    if (player.IsAlive && player.Game != null)
                    {
                        if (!string.IsNullOrWhiteSpace(context.Request.Query["guess"]))
                        {
                            var guess = context.Request.Query["guess"];
                            player.GuessesLeft--;

                            if (player.Word == player.Game.MajorityWord)
                            {
                                if (guess.Equals(player.Game.MinorityWord, StringComparison.OrdinalIgnoreCase))
                                {
                                    player.Game.State = GameState.Ending;
                                    player.Game.EndMessage = "Game Over - " + player.Name + " guessed the other team's word.<br>";
                                    context.Response.Redirect(PlayController.PathEnd);
                                    return context.Response.WriteAsync(string.Empty);
                                }
                            }
                            else
                            {
                                if (guess.Equals(player.Game.MajorityWord, StringComparison.OrdinalIgnoreCase))
                                {
                                    player.Game.State = GameState.Ending;
                                    player.Game.EndMessage = "Game Over - " + player.Name + " guessed the other team's word.<br>";
                                    context.Response.Redirect(PlayController.PathEnd);
                                    return context.Response.WriteAsync(string.Empty);
                                }
                            }
                        }
                    }

                    response.AppendLine("<a href='"+ PlayerVotingPath + "'>RETURN.</a>");
                    response.AppendLine("<br>Guesses left: " + player.GuessesLeft);

                    if (player.GuessesLeft > 0)
                    {
                        response.AppendLine("<form action='" + GuessPath + "' method='get'>Guess opposing word:<br><input type='text' name='guess'><br><input type='submit' value='GUESS!'></form>");
                    }

                    return context.Response.WriteAsync(response.ToString());
                }
            }

            context.Response.Redirect(JoinController.JoinPath);
            return context.Response.WriteAsync(string.Empty);
        }

        /// <summary>
        /// Handles player voting
        /// </summary>
        public static Task HandlePlayerVoting(IOwinContext context)
        {
            context.Response.ContentType = "text/html";
            var response = new StringBuilder();

            if (Utils.GetUser(context))
            {
                var userName = context.Request.User.Identity.Name;
                if (Player.Players.ContainsKey(userName))
                {
                    var player = Player.Players[userName];
                    if (player.IsAlive && player.Game != null && player.Game.State == GameState.Voting)
                    {
                        response.Append("<br><b><u>VOTE!</b></u>");
                        foreach (var playerToVote in player.Game.Players)
                        {
                            if (playerToVote.IsAlive && playerToVote.Name != userName)
                            {
                                response.Append("<br><a href='" + PlayerVotingPath + "?vote=" + playerToVote.Name + "'>VOTE FOR " + playerToVote.Name + "</a>");
                            }
                        }

                        if (!string.IsNullOrEmpty(context.Request.Query["vote"]))
                        {
                            var playerVotedForName = context.Request.Query["vote"];
                            var playerVotedFor = player.Game.Players.Find(p => p.Name == playerVotedForName);
                            if (playerVotedFor != null)
                            {
                                player.Game.Votes[player] = playerVotedFor;
                            }
                        }

                        if (player.Game.Votes.ContainsKey(player))
                        {
                            response.Append("<br>You voted for: " + player.Game.Votes[player].Name);
                        }

                        response.AppendLine("<br><br><a href='/guess'>Guess the other team's word and automatically win!</a>");

                        Utils.SetRefresh(8, response);
                        return context.Response.WriteAsync(response.ToString());
                    }
                    else
                    {
                        context.Response.Redirect(PlayController.PlayPath);
                        return context.Response.WriteAsync(string.Empty);
                    }
                }
            }

            context.Response.Redirect(JoinController.JoinPath);
            return context.Response.WriteAsync(string.Empty);
        }
    }
}
