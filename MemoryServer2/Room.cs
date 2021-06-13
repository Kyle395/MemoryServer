﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryServer2
{

    class Room
    {
        public int id;
        private string password;
        public bool isPrivate;

        GameState state = new GameState();
        List<int> boardValues = new List<int>(16);
        int nextPlayer = -1;
        Stopwatch sw = new Stopwatch();

        private Random rng = new Random();
        public void Shuffle<T>(ref List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public Room(int id, bool isPrivate, string password)
        {
            this.isPrivate = isPrivate;
            this.password = password;
            this.id = id;
            for (int i = 0; i < 8; i++)
            {
                boardValues.Add(i);
                boardValues.Add(i);
            }
        }
        public string Password
        {
            get { return password; }
            set { password = ""; }
        }

        public void UpdateGame()
        {
            lock (state)
            {
                if (!state.begun)
                {
                    int readyCount = 0;
                    foreach (var player in state.players.Values)
                    {
                        if (player.ready) readyCount++;
                    }
                    if (readyCount == state.players.Count && readyCount > 1)
                    {
                        state.begun = true;
                        Shuffle(ref boardValues);
                        Shuffle(ref state.playerOrder);
                        state.activePlayer = 0;
                    }
                }
                else
                {
                    List<int> shown = new List<int>();
                    for (int i = 0; i < 16; i++)
                    {
                        if (state.board[i] >= 0)
                        {
                            shown.Add(i);
                        }
                    }

                    if (state.activePlayer == -1)
                    {
                        if (sw.ElapsedMilliseconds >= 3000)
                        {
                            state.activePlayer = nextPlayer;
                            if (shown.Count == 2)
                            {
                                if (state.board[shown[0]] == state.board[shown[1]])
                                {
                                    state.board[shown[0]] = state.board[shown[1]] = -2;
                                    state.players[state.playerOrder[state.activePlayer]].score++;
                                }
                                else
                                {
                                    state.board[shown[0]] = state.board[shown[1]] = -1;
                                }
                            }
                        }
                    }
                    else
                    {
                        if(!state.players[state.playerOrder[state.activePlayer]].connected)
                        {
                            nextPlayer = (state.activePlayer + 1) % state.players.Count;
                        }
                        else if (shown.Count == 2)
                        {
                            if (state.board[shown[0]] == state.board[shown[1]])
                            {
                                nextPlayer = state.activePlayer;
                            }
                            else
                            {
                                nextPlayer = (state.activePlayer + 1) % state.players.Count;
                            }
                            // start timer
                            sw.Restart();
                            state.activePlayer = -1;
                        }                        
                    }
                }
            }
        }

        public void HandleClient(TcpClient client, string playerID, string password)
        {
            NetworkStream stream = client.GetStream();

            if (this.password == password)
            {
                lock (state)
                {
                    if (state.players.ContainsKey(playerID))
                    {
                        state.players[playerID].ready = false;
                        state.players[playerID].connected = true;
                    }
                    else
                    {
                        if (!state.begun && state.players.Count < 4)
                        {
                            var player = new PlayerState();
                            state.players[playerID] = player;
                            state.playerOrder.Add(playerID);
                        }
                        else
                        {
                            string error = "error 2";
                            CommProtocol.Write(stream, error);
                            return;
                        }
                    }
                }
            }
            else
            {
                string error = "error 1";
                CommProtocol.Write(stream, error);
                return;
            }
            CommProtocol.Write(stream, "ok");

            bool inRoom = true;

            while (inRoom)
            {
                string stateCode;
                lock (state)
                {
                    stateCode = state.Encode();
                }
                
                CommProtocol.Write(stream, "game" + stateCode);
                string sData="";
                try
                { 
                    sData = CommProtocol.Read(stream);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine("Disconnecting the player " + playerID);
                    sData = "lrm";
                }
                Console.WriteLine(sData);
                string[] logData = CommProtocol.CheckMessage(sData);

                if (sData == "noop")
                {
                    
                }
                else if (logData[0] == "lrm")
                {
                    lock (state)
                    {
                        if (state.begun)
                        {
                            state.players[playerID].connected = false;
                        }
                        else
                        {
                            state.players.Remove(playerID);
                            state.playerOrder.Remove(playerID);
                        }
                    }
                    inRoom = false;
                }
                else if (logData[0] == "ready")
                {
                    lock (state)
                    {
                        if (logData[1] == "false")
                        {
                            state.players[playerID].ready = false;
                        }
                        else
                        {
                            state.players[playerID].ready = true;
                        }
                    }
                }
                else if (logData[0] == "move")
                {
                    lock (state)
                    {
                        if (state.activePlayer != -1 && playerID == state.playerOrder[state.activePlayer])
                        {
                            int pos = Convert.ToInt32(logData[1]);
                            if (state.board[pos] == -1)
                            {
                                state.board[pos] = boardValues[pos];
                            }
                        }
                    }
                }
                UpdateGame();
            }
        }
        public string Encode()
        {
            lock (state)
            {
                return " " + id + " " + isPrivate + " " + state.players.Count + " " + state.begun;
            }
        }
    }
}
