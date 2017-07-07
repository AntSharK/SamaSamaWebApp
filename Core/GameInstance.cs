using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SamaSamaLAN.Core
{
    /// <summary>
    /// An instance of the game
    /// </summary>
    public class GameInstance
    {
        public static Dictionary<int, GameInstance> Instances = new Dictionary<int, GameInstance>();
        private static int currentGameNumber = 0;

        public string EndMessage = "Game hasn't ended";
        public List<Player> Players = new List<Player>();
        public GameState State = GameState.SettingUp;
        public string MajorityWord;
        public string MinorityWord;
        public Dictionary<Player, Player> Votes = new Dictionary<Player, Player>();
        public int MinorityCount = 0;
        public int GameNumber;

        public GameInstance()
        {
            currentGameNumber++;
            Instances[currentGameNumber] = this;
            this.GameNumber = currentGameNumber;
        }
    }
}
