using System;
using System.Configuration;
using System.IO;
using System.Xml.Linq;

namespace HttpServerConsole
{
    static class Config
    {
        public static int PortNumber { get; }
        public static string DbFile { get; }
        public static string Error { get; set; }

        static Config()
        {
            Error = string.Empty;

            try {

                if(!File.Exists("config.xml"))
                {
                    var newConfigFile = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("appSettings"));
                    newConfigFile.Root.Add(new XElement("add", new XAttribute("key", "PortNumber"), new XAttribute("value", "80")));
                    newConfigFile.Root.Add(new XElement("add", new XAttribute("key", "DbFile"), new XAttribute("value", "Guestbook/UserMessages.xml")));
                    newConfigFile.Save("config.xml");
                }

                int intParseResult;
                if (int.TryParse(ConfigurationManager.AppSettings["PortNumber"], out intParseResult))
                {
                    PortNumber = int.Parse(ConfigurationManager.AppSettings["PortNumber"]);
                } else
                {
                    Error = "Unable to parse port number from config.xml. Wrong value is: " + ConfigurationManager.AppSettings["PortNumber"];
                }

                DbFile = ConfigurationManager.AppSettings["DbFile"].Trim('/').Trim('\\');

            }
            catch (Exception ex)
            {
                Error  = ex.Message ;                
            } 
        }
    }
}

