using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DemoHttpServer
{
    class HtmlBuilder
    {
        public static XDocument BuildDefaultResponse()
        {
            return new XDocument(
                    new XElement("div", new XAttribute("class", "container"),
                      new XElement("div", new XAttribute("class", "jumbotron center"),
                          new XElement("h1", "Hello World")
                      )));            
        }

        public static XDocument BuildGuestbookTable(string dbFile)
        {
             XDocument htmlBody = new XDocument(
                new XElement("div", new XAttribute("class", "container"),
                  new XElement("div", new XAttribute("class", "table-responsive"),
                      new XElement("table", new XAttribute("class", "table"),
                          new XElement("thead",
                              new XElement("tr",
                                   new XElement("th", "User Name"),
                                   new XElement("th", "Message"))),
                          new XElement("tbody")
                  ))));

            switch (Path.GetExtension(dbFile).TrimStart('.'))
            {
                case "xml":

                        XDocument xmlGuestbookMessages = XDocument.Load(dbFile);
                        foreach (var element in xmlGuestbookMessages.Root.Descendants())
                        {
                            htmlBody.Root.Descendants("tbody").First().Add(
                                new XElement("tr", 
                                    new XElement("td", element.FirstAttribute.Value), 
                                    new XElement("td", element.Value)
                                    ));
                        }
                    
                    return htmlBody;
                case "sqlite":

                    var dbFileFullPath = Environment.CurrentDirectory + "/" + dbFile;

                    using (var sqliteConnection = new SQLiteConnection("Data Source=" + dbFileFullPath))
                    {
                        sqliteConnection.Open();
                        using (var sqliteCommand = sqliteConnection.CreateCommand())
                        {
                            sqliteCommand.CommandText = "SELECT * FROM Messages";
                            sqliteCommand.CommandType = System.Data.CommandType.Text;
                            SQLiteDataReader reader = sqliteCommand.ExecuteReader();
                            while (reader.Read())
                            {
                                htmlBody.Root.Descendants("tbody").First().Add(
                                    new XElement("tr", 
                                        new XElement("td", Convert.ToString(reader["UserName"]), 
                                        new XElement("td", Convert.ToString(reader["Message"]))
                                        )));
                            }
                        }
                    }

                    return htmlBody;
                default:
                    return htmlBody;
            }


        }

        public static XDocument BuildTableFromXmlFile()
        {
            return new XDocument(
                    new XElement("div", new XAttribute("class", "container"),
                      new XElement("div", new XAttribute("class", "table-responsive"),
                          new XElement("table", new XAttribute("class", "table"),
                              new XElement("thead",
                                  new XElement("tr",
                                       new XElement("th", "UserName"),
                                       new XElement("th", "Message"))),
                              new XElement("tbody")
                      ))));
        }


    }
}
