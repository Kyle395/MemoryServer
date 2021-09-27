using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryServer2
{
    class Menu
    {
        public void BeginDataTransmission(NetworkStream stream, List<Room> roms)
        {
            DatabaseConnector dc = new DatabaseConnector();
            List<Room> rooms = roms;
            bool logged = false;
            StreamWriter writer = new StreamWriter(stream);
            writer.AutoFlush = true;
            StreamReader reader = new StreamReader(stream);

            do
            {
                String data = reader.ReadLine();
                Console.WriteLine(data);
                string[] logData = checkMessage(data);
                if (logData[0] == "log")
                {
                    if (dc.checkUserData(logData[1], logData[2]))
                    {

                        Console.WriteLine("user logged");
                        writer.WriteLine("1");
                        logged = true;
                    }
                    else
                    {
                        writer.WriteLine("!");
                        Console.WriteLine("wrong login data");
                    }
                }
                else if (logData[0] == "reg")
                {
                    if (dc.registerUser(logData[1], logData[2]))
                    {
                        writer.WriteLine("1");
                    }
                    else writer.WriteLine("!");
                }
                else Console.WriteLine("wrong command");
            } while (!logged);


            
            while (logged)
            {
                String data = reader.ReadLine();
                Console.WriteLine(data);
                string[] logData = checkMessage(data);
                if (data == "logout")
                {
                    Thread.Sleep(2000);
                    logged = false;
                }
                else if (logData[0]=="ref")
                {
                    if (rooms.Count != 0)
                    {
                        foreach (Room room in rooms)
                        {
                            string msg = "rom " + room.id + " " + room.isPrivate + " " + room.activeUsers + " " + room.begun;

                            if (room == rooms.Last())
                            {
                                msg = msg + " end";
                            }
                            writer.Write(msg);
                        }
                    }
                    else writer.Write("     end");
                }
                else if (logData[0] == "crm")
                {
                    rooms.Add(new Room(rooms.Count, bool.Parse(logData[1]), logData[2]));
                    writer.WriteLine(rooms[rooms.Count - 1].id);
                }
                else if (logData[0] == "jrm")
                {
                    rooms[int.Parse(logData[1])].join(Guid.Parse(logData[2]), logData[3]);
                }
                else if (logData[0] == "lrm")
                {
                    rooms[int.Parse(logData[1])].leave(Guid.Parse(logData[2]));
                }
            }
        }
        private string[] checkMessage(string s)
        {
            return s.Split(' ');
        }

    }
}
