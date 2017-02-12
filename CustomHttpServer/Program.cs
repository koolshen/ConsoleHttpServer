using DemoHttpServer;
using System;
using System.Threading;

namespace HttpServerConsole
{

    class Program {

        static void Main()
        {
            try
            {
                if (Config.Error != string.Empty)
                {
                    throw new Exception(Config.Error);
                }

                HttpServerEvent.Evt_StatusChange += ServerEvents_Evt_StatusChange;
                HttpServer httpServer = new HttpServer(Config.PortNumber, Config.DbFile);

                Thread thread = new Thread(new ThreadStart(httpServer.Listen));
                thread.Start();

            } catch (Exception ex)
            {
                Console.WriteLine("Could not start Http Server! " + ex.Message);
                Console.ReadLine();
            }
        }

        private static void ServerEvents_Evt_StatusChange(string data)
        {
            Console.WriteLine(data);
        }
    }

}
