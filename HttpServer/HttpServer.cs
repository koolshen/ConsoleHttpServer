using System;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Linq;

namespace DemoHttpServer
{

    public class HttpServer
    {
        private int Port;
        private TcpListener Listener;
        private HttpRequestHandler Handler;
        private string DbFile;

        public HttpServer(int port, string dbFile)
        {
                Port = port;
                Handler = new HttpRequestHandler();
                DbFile = dbFile;

                InitDataBaseFiles(dbFile);

                Console.WriteLine("***********************************************************");
                Console.WriteLine("            Welcome to Demo Console Http Server            ");
                Console.WriteLine("            Server Port: " + Port + "");
                Console.WriteLine("            Server DataBase File: " + Path.GetFileName(DbFile) + "");
                Console.WriteLine("***********************************************************");
        }

        public void Listen()
        {
                Listener = new TcpListener(IPAddress.Any, Port);
                Listener.Start();

                Console.WriteLine("Http Server is running... ");

                while (true)
                {
                    TcpClient clientData = Listener.AcceptTcpClient();
                    Thread thread = new Thread(() =>
                    {
                         Handler.HandleClientRequest(clientData, DbFile);
                    });
                    thread.Start();
                    Thread.Sleep(1);
                }

        }

        private void InitDataBaseFiles(string dbFile)
        {
            var dbFileFullPath = Environment.CurrentDirectory + "/" + dbFile;

            if (!Directory.Exists(Environment.CurrentDirectory + "/Guestbook"))
            {
                Directory.CreateDirectory(Environment.CurrentDirectory + "/Guestbook");
            }

            switch (Path.GetExtension(dbFile))
            {

                case ".xml":
                    if (!File.Exists(dbFileFullPath))
                    {
                        new XDocument(
                            new XDeclaration("1.0", "utf-8", "yes"),
                            new XElement("messages")
                            ).Save(dbFileFullPath);
                    }
                    break;

                case ".sqlite":
                    if (!File.Exists(dbFileFullPath))
                    {
                        SQLiteConnection.CreateFile(dbFileFullPath);

                        using (var sqlite = new SQLiteConnection("Data Source=" + dbFileFullPath))
                        {
                            sqlite.Open();
                            string sql = "create table Messages (UserName varchar(20), Message varchar(500))";
                            SQLiteCommand command = new SQLiteCommand(sql, sqlite);
                            command.ExecuteNonQuery();
                        }
                    }
                    break;
            }

        }


    }

    public delegate void ChangeEvent(string data);

    public static class HttpServerEvent
    {
        public static event ChangeEvent Evt_StatusChange;

        public static void OnStatusChange(string data)
        {
            Evt_StatusChange("Event from HttpServer (" + DateTime.Now.ToString() + "): " + data);
        }

    }

}
