using DemoHttpServer.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace DemoHttpServer
{
    class HttpRequestHandler
    {

        public HttpRequestHandler() { }

        public void HandleClientRequest(TcpClient client, string dbFile)
        {
            HttpRequest request = new HttpRequest();
            HttpResponse response = new HttpResponse();

            try
            {

                request = GetRequest(client.GetStream());

                HttpServerEvent.OnStatusChange(string.Format("{0 } request perfomed. Url: {1}, Parameters Length: {2}", request.Method, request.Url, request.Parameters.Count));

                XDocument htmlBody = new XDocument();

                switch (request.Method)
                {
                    case "GET":
                        if (request.Url.Trim('/').Trim('\\').ToLower() == "guestbook")
                        {
                            htmlBody = HtmlBuilder.BuildGuestbookTable(dbFile);
                        } else
                        {
                            htmlBody = HtmlBuilder.BuildDefaultResponse();
                        }
                        break;
                    case "POST":
                        if (request.Url.Trim('/').ToLower() == "guestbook" && request.Parameters.Count == 2)
                        {
                            AddNewMessages(request, dbFile);
                        }
                        break;
                }

                response = SetResponse(client.GetStream(), request, htmlBody);
            } catch (Exception ex)
            {
                response = SetErrorResponse(client.GetStream());
                HttpServerEvent.OnStatusChange(ex.Message);
            } finally
            {
                try
                {
                    WriteResponse(client.GetStream(), response);
                } catch (Exception ex)
                {
                    HttpServerEvent.OnStatusChange(ex.Message);
                }
            }
        }

        private void AddNewMessages(HttpRequest request, string dbFile)
        {
            var dbFileFullPath = Environment.CurrentDirectory + "/" + dbFile;

            switch (Path.GetExtension(dbFile))
            {
                case ".xml":

                    var newMessage = new XElement("message", request.Parameters["message"], new XAttribute("user", request.Parameters["user"]));
                    var xmlFile = XDocument.Load(dbFileFullPath);
                    xmlFile.Root.Add(newMessage);
                    xmlFile.Save(dbFileFullPath);

                    break;
                case ".sqlite":

                    using (var sqliteConnection = new SQLiteConnection("Data Source=" + dbFileFullPath))
                    {
                        using (var sqliteCommand = new SQLiteCommand(string.Format("INSERT INTO Messages(UserName, Message) VALUES('{0}','{1}')", request.Parameters["user"], request.Parameters["message"]), sqliteConnection))
                        {
                            sqliteConnection.Open();
                            sqliteCommand.Parameters.Add(request.Parameters["user"], System.Data.DbType.String);
                            sqliteCommand.Parameters.Add(request.Parameters["message"], System.Data.DbType.String);
                            sqliteCommand.ExecuteNonQuery();
                            sqliteConnection.Close();
                        }
                    }

                     break;

            }

            HttpServerEvent.OnStatusChange("New message has been added. User Name: " + request.Parameters["user"] + " Message: " + request.Parameters["message"]);
        }

        private static void WriteResponse(Stream stream, HttpResponse response)
        {
            if (response.Content == null)
            {
                response.Content = new byte[] { };
            }

            if (!response.Headers.ContainsKey("Content-Type"))
            {
                response.Headers["Content-Type"] = "text/html";
            }

            lock (stream)
            {

                response.Headers["Content-Length"] = response.Content.Length.ToString();

                Write(stream, string.Format("HTTP/1.0 {0} {1}\r\n", response.StatusCode, response.ReasonPhrase));
                Write(stream, string.Join("\r\n", response.Headers.Select(x => string.Format("{0}: {1}", x.Key, x.Value))));
                Write(stream, "\r\n\r\n");

                stream.Write(response.Content, 0, response.Content.Length);
            }
        }

        private static void Write(Stream stream, string text)
        {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                stream.Write(bytes, 0, bytes.Length);
        }

        protected virtual HttpResponse SetErrorResponse(Stream inputStream)
        {
            return new HttpResponse()
            {
                ReasonPhrase = "Internal Server Error",
                StatusCode = "500",
                Content = Encoding.ASCII.GetBytes("<h1>Internal Server Error</h1>")
            };
        }

        protected virtual HttpResponse SetResponse(Stream inputStream, HttpRequest request, XDocument body)
        {
            StringBuilder htmlRespone = new StringBuilder();
            htmlRespone.Append("<!DOCTYPE html>\r\n");
            htmlRespone.Append("<html>\n<head>\n");
            htmlRespone.Append("<link rel=\"stylesheet\" href=\"https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap.min.css\">\n");
            htmlRespone.Append("<script src=\"https://ajax.googleapis.com/ajax/libs/jquery/3.1.1/jquery.min.js\"></script>\n");
            htmlRespone.Append("<script src=\"https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/js/bootstrap.min.js\"></script>\n");
            htmlRespone.Append("</head>\n");
            htmlRespone.Append("<body>\n");
            htmlRespone.Append(body.ToString());
            htmlRespone.Append("</body>\n</html>\n");

            return new HttpResponse()
            {
                ReasonPhrase = "Ok",
                StatusCode = "200",
                Content = Encoding.ASCII.GetBytes(htmlRespone.ToString())
            };
        }

        private HttpRequest GetRequest(Stream inputStream)
        {

            string request = ParseStreamData(inputStream);

            string[] tokens = request.Split(' ');
            if (tokens.Length != 3)
            {
                throw new Exception("invalid http request line");
            }

            string method = tokens[0].ToUpper();
            string url = tokens[1];
            string protocolVersion = tokens[2];

            Dictionary<string, string> headers = new Dictionary<string, string>();
            string line;
            while ((line = ParseStreamData(inputStream)) != null)
            {
                if (line.Equals(""))
                {
                    break;
                }

                int separator = line.IndexOf(':');
                if (separator == -1)
                {
                    throw new Exception("invalid http header line: " + line);
                }
                string name = line.Substring(0, separator);
                int pos = separator + 1;
                while ((pos < line.Length) && (line[pos] == ' '))
                {
                    pos++;
                }

                string value = line.Substring(pos, line.Length - pos);
                headers.Add(name, value);
            }

            string content = null;
            Dictionary<string, string> ParametersList = new Dictionary<string, string>();

            if (headers.ContainsKey("Content-Length"))
            {
                ParametersList = new Dictionary<string, string>();
                int totalBytes = Convert.ToInt32(headers["Content-Length"]);
                int bytesLeft = totalBytes;
                byte[] bytes = new byte[totalBytes];

                while (bytesLeft > 0)
                {
                    byte[] buffer = new byte[bytesLeft > 1024 ? 1024 : bytesLeft];
                    int n = inputStream.Read(buffer, 0, buffer.Length);
                    buffer.CopyTo(bytes, totalBytes - bytesLeft);

                    bytesLeft -= n;
                }

                content = Encoding.ASCII.GetString(bytes);

                var parametersArray = content.Split('&');

                if (!content.Contains("user=") || !content.Contains("message=")) {
                    throw new Exception("Invalid Post Data! Please send user=*&message=* like parameters in request body");
                }

                foreach (var parameter in parametersArray)
                {
                    var item = parameter.Split('=');
                    ParametersList.Add(item[0], item[1]);
                }

            }


            return new HttpRequest()
            {
                Method = method,
                Url = url,
                Headers = headers,
                Content = content,
                Parameters = ParametersList
            };
        }


        private static string ParseStreamData(Stream stream)
        {
            int next_char;
            string data = "";
            while (true)
            {
                next_char = stream.ReadByte();
                if (next_char == '\n') { break; }
                if (next_char == '\r') { continue; }
                if (next_char == -1) { Thread.Sleep(1); continue; };
                data += Convert.ToChar(next_char);
            }
            return data;
        }

    }

}
