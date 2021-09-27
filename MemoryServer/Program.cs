using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MemoryServer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ServerTCP server = new ServerTCP(8080);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
