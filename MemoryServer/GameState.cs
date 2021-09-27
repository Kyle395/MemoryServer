using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryServer
{
    class PlayerState
    {
        public bool ready = false;
        public bool connected = true;
        public int score = 0;
    }
    class GameState
    {
        public bool begun = false;        
        public List<string> playerOrder = new List<string>();
        public Dictionary<string, PlayerState> players = new Dictionary<string, PlayerState>();
        public List<string> winners = new List<string>();
        public List<int> board;
        public int activePlayer = -1;

        public GameState()
        {
            initBoard();
        }

        public void initBoard()
        {
            int[] vals = new int[16];
            for (int i = 0; i < vals.Length; i++)
            {
                vals[i] = -1;
            }
            this.board = new List<int>(vals);
        }
        public string Encode()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(" " + begun);
            sb.Append(" " + players.Count);
            foreach (var p in playerOrder)
            {
                sb.Append(" " + p);
                sb.Append(" " + players[p].connected);
                sb.Append(" " + players[p].ready);
                sb.Append(" " + players[p].score);
            }
            sb.Append(" " + board.Count);
            foreach (var val in board)
            {
                sb.Append(" " + val);
            }
            sb.Append(" " + activePlayer);
            sb.Append(" " + winners.Count);
            foreach(var w in winners)
            {
                sb.Append(" " + w);
            }

            return sb.ToString();
        }

        public void Decode(string[] data)
        {
            int i = 0;
            begun = Convert.ToBoolean(data[i++]);
            playerOrder = new string[Convert.ToInt32(data[i++])].ToList();
            players.Clear();
            for (int j = 0; j < playerOrder.Count; j++)
            {
                playerOrder[j] = data[i++];
                PlayerState ps = new PlayerState();
                ps.connected = Convert.ToBoolean(data[i++]);
                ps.ready = Convert.ToBoolean(data[i++]);
                ps.score = Convert.ToInt32(data[i++]);
                players.Add(playerOrder[j], ps);
            }
            board = new int[Convert.ToInt32(data[i++])].ToList();
            for (int j = 0; j < board.Count; j++)
            {
                board[j] = Convert.ToInt32(data[i++]);
            }
            activePlayer = Convert.ToInt32(data[i++]);
            winners = new string[Convert.ToInt32(data[i++])].ToList();
            winners.Clear();
            for (int j = 0; j < winners.Count; j++)
            {
                winners[j] = data[i];
            }
        }
    }
}
