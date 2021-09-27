using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryServer
{    
    class ServerTCP
    {
        private TcpListener _server;
        private Boolean _isRunning;

        private List<string> loggedUsers = new List<string>();        
        private ConcurrentDictionary<int, Room> roomDict = new ConcurrentDictionary<int, Room>();
        private ConcurrentDictionary<string, int> roomOwners = new ConcurrentDictionary<string, int>();
        private int nextRoomId = 0;

        public ServerTCP(int port)
        {
            _server = new TcpListener(IPAddress.Parse("10.99.201.42"), port);
            _server.Start();            
            _isRunning = true;

            LoopClients();
        }
        private int getNextId()
        {
            return Interlocked.Increment(ref nextRoomId) - 1;
        }

        public void LoopClients()
        {
            while (_isRunning)
            {
                // wait for client connection
                // object[] vs = new object[2];
                TcpClient newClient = _server.AcceptTcpClient();                
                //vs[0] = newClient;
                //vs[1] = rooms;
                // client found.
                // create a thread to handle communication

                Thread t = new Thread(unused =>
                {
                    try
                    {
                        HandleClient(newClient);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Client has disconnected due to error");
                    }
                });
                t.Start();
            }
        }

        static string Hash(string password)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString();
            }
        }

        public void HandleClient(TcpClient client)
        {            
            NetworkStream stream = client.GetStream();
            CommProtocol.setAes(stream);           

            bool clientConnected = true;
            DatabaseConnector dc = new DatabaseConnector();
            bool logged = false;
            string playerID = "";


            while (clientConnected)
            {
                do
                {
                    string sData = CommProtocol.Read(stream);
                    Console.WriteLine(sData);
                    string[] logData = CommProtocol.CheckMessage(sData);
                    if (logData[0] == "log")
                    {
                        if (!loggedUsers.Contains(logData[1]))
                        {
                            if (dc.checkUserData(logData[1], Hash(logData[1]+logData[2])))
                            {

                                Console.WriteLine("user logged");
                                CommProtocol.Write(stream, "log ok");
                                logged = true;
                                playerID = logData[1];
                                lock(loggedUsers)
                                {
                                    loggedUsers.Add(playerID);
                                }
                            }
                            else
                            {
                                CommProtocol.Write(stream, "error wrong_credentials");
                                Console.WriteLine("wrong login data");
                            }
                        }
                        else CommProtocol.Write(stream,"error already_logged_in");
                    }
                    else if (logData[0] == "reg")
                    {
                        if (dc.registerUser(logData[1], Hash(logData[1]+logData[2])))
                        {
                            CommProtocol.Write(stream, "reg ok");
                        }
                        else CommProtocol.Write(stream, "error login_already_used");
                    }
                    else Console.WriteLine("wrong command");
                } while (!logged);

                while (logged)
                {
                    string sData = "";
                    try
                    {
                        sData = CommProtocol.Read(stream);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Logging out player " + playerID +" due to error");                        
                        sData = "logout";
                    }
                    Console.WriteLine(sData);
                    string[] logData = CommProtocol.CheckMessage(sData);

                    var rooms = this.roomDict.ToArray().Select(x => x.Value).ToList();
                    rooms.Sort((x, y) => x.id - y.id);

                    if (sData == "logout")
                    {                        
                        logged = false;
                        clientConnected = false;
                        lock(loggedUsers)
                        { 
                            loggedUsers.Remove(playerID);
                        }
                    }
                    else if (logData[0] == "ref")
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("ref ");
                        sb.Append(rooms.Count);
                        foreach (Room room in rooms)
                        {
                            sb.Append(room.Encode());
                        }
                        CommProtocol.Write(stream, sb.ToString());                        
                    }
                    else if (logData[0] == "crm")
                    {
                        int id = getNextId();
                        
                        if (!roomOwners.ContainsKey(playerID))
                        {
                            roomDict.TryAdd(id, new Room(id, bool.Parse(logData[1]), logData[2]));
                            roomOwners.TryAdd(playerID, id);
                            string str = "crm " + id.ToString();
                            CommProtocol.Write(stream, str);
                        }
                        else CommProtocol.Write(stream, "error room_already_created");
                    }
                    else if (logData[0] == "jrm")
                    {
                        string pwd = "";
                        if (logData.Length == 4)
                        {
                            pwd = logData[3];
                        }
                        rooms[int.Parse(logData[1])].HandleClient(client, logData[2], pwd);
                    }
                    else if (logData[0] == "chngpass")
                    {
                        dc.editUserPassword(logData[1], Hash(logData[1] + logData[2]));
                        CommProtocol.Write(stream, "ok");
                    }
                    else if (logData[0] == "delacc")
                    {
                        dc.deleteUser(logData[1], Hash(logData[1] + logData[2]));
                        CommProtocol.Write(stream, "ok");
                    }
                }
            }
        }
    }
}
