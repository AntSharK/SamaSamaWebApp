using System.Collections.Generic;

namespace SamaSamaLAN.Core
{
    /// <summary>
    /// A player of the game
    /// </summary>
    public class Player
    {
        public static Dictionary<string, Player> Players = new Dictionary<string, Player>();

        public string Word = string.Empty;
        public bool IsAlive;
        public string Name;
        public int GuessesLeft;
        public GameInstance Game;

        public Player(string name, GameInstance game)
        {
            this.Name = name;
            Players[name] = this;
            this.Join(game);
        }

        /// <summary>
        /// Joins a game
        /// </summary>
        /// <param name="game">The instance of the game to join</param>
        public void Join(GameInstance game)
        {
            if (this.Game != null)
            {
                game.Players.Remove(this);
            }

            this.Game = game;
            this.IsAlive = true;
            this.GuessesLeft = 2;
            game.Players.Add(this);
        }

        public void Leave()
        {
            this.Game.Players.Remove(this);
            this.Game = null;
            this.IsAlive = false;
        }
    }
}
