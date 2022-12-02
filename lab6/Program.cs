using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace lab6
{
    class Program
    {
        public static string SuccessHeaders(int contentLength)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("HTTP/1.1 200 OK").Append("\r\n");
            builder.Append("Date:").Append(DateTime.Now).Append("\r\n");
            builder.Append("Content-Type: text/html; charset=UTF-8").Append("\r\n");
            builder.Append("Content-Length:").Append(contentLength).Append("\r\n");
            builder.Append("Connection:close").Append("\r\n");
            builder.Append("\r\n");
            return builder.ToString();

        }

        private static string AnswerPage(String val)
        {
            StringBuilder bodyBuilder = new StringBuilder();
            bodyBuilder.Append("Result:");
            bodyBuilder.Append("<br><br>");
            bodyBuilder.Append(val);
            bodyBuilder.Append("<br>");
            bodyBuilder.Append("<br>");
            bodyBuilder.Append("<a href='/'>OpenIndexPage</a>");
            String body = bodyBuilder.ToString();
            return String.Concat(SuccessHeaders(body.Length), body);
        }

        private static string BadAnswerPage()
        {
            StringBuilder bodyBuilder = new StringBuilder();
            bodyBuilder.Append("Something was bad");
            bodyBuilder.Append("<br>");
            bodyBuilder.Append("<a href='/'>OpenIndexPage</a>");
            String body = bodyBuilder.ToString();

            return String.Concat(SuccessHeaders(body.Length), body);
        }

        private static string IndexPage()
        {
            StringBuilder bodyBuilder = new StringBuilder();

            bodyBuilder.Append("<div id='body'>");
            bodyBuilder.Append("Input number:");
            bodyBuilder.Append("<br>");
            bodyBuilder.Append("<input type='text' name='val' id='value' >");
            bodyBuilder.Append("<br>");
            bodyBuilder.Append("<input type='submit' onclick=post()>");
            bodyBuilder.Append("<script>function post(){let textA = document.getElementById('value').value;let reqObj = textA;let request = new XMLHttpRequest();request.open('POST', 'https://localhost:8008/');request.send(reqObj); request.onload = function() {if (request.status == 200){document.getElementById('body').innerHTML = request.response;}}}</script>");
            bodyBuilder.Append("</div>");

            String body = bodyBuilder.ToString();
            //warning,ifbodycontainsnon-asciisymbolsnextrecordcanbe
            return String.Concat(SuccessHeaders(body.Length), body);
        }

        static X509Certificate2 serverCertificate = null;

        static void ProcessClient(Object obj)
        {
            TcpClient client = (TcpClient)obj;
            SslStream sslStream = new SslStream(client.GetStream(), false);

            try
            {
                Console.WriteLine("start...");
                
                sslStream.AuthenticateAsServer(serverCertificate, false, SslProtocols.Tls12, false);
                Console.WriteLine("end...");
                sslStream.ReadTimeout = 2000;
                sslStream.WriteTimeout = 2000;
                //Readamessagefromtheclient.
                Console.WriteLine("Waiting for client message...");
                string messageData = ReadMessage(sslStream);
                Console.WriteLine("request:");
                Console.WriteLine(messageData);

                string page = "";
                if (messageData.StartsWith("POST"))
                {
                    Regex r = new Regex("\r\n\r\n");
                    String[] request = r.Split(messageData, 2);
                    if (request.Length <= 0)
                    {
                        page = BadAnswerPage();
                    }
                    else
                    {
                        String val = request[1].Split('\\')[0];
                        double answer = Teilor(Convert.ToDouble(val));
                        page = AnswerPage(answer.ToString());
                    }
                }
                else
                {
                    page = IndexPage();
                }

                Console.WriteLine("response:");
                Console.WriteLine(page);
                byte[] message = Encoding.UTF8.GetBytes(page);
                sslStream.Write(message, 0, message.Length);
                sslStream.Flush();
            }
            catch (AuthenticationException e)
            {
                Console.WriteLine("Exception:{0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception:{0}",e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed-closing the connection.");

            }
            finally
            {
                sslStream.Close();
                client.Close();
            }

        }

        static void Main(string[] args)
        {
            serverCertificate = new X509Certificate2("output.cer", "123");
            TcpListener listener = new TcpListener(IPAddress.Any, 8008);
            listener.Start();
            Console.WriteLine("Server run on:https://localhost:8008");
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(ProcessClient, client);
            }
        }

        static string ReadMessage(SslStream sslStream)
        {
            StringBuilder messageData = new StringBuilder();
            try
            {
                byte[] buffer = new byte[2048];
                int bytes = -1;
                do
                {
                    bytes = sslStream.Read(buffer, 0, buffer.Length);
                    Decoder decoder = Encoding.UTF8.GetDecoder();
                    char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                    decoder.GetChars(buffer, 0, bytes, chars, 0);
                    messageData.Append(chars);


                } while (bytes != 0);
            }
            catch (Exception) { }

            return messageData.ToString();
        }

        public static double Teilor(double x)
        {
            double e = 0.001;
            if (x == 0.0)
            {
                return 0;
            }
            if (x < 0.0)
                x = -x;
            double t = x - 1;
            double u = t;
            double lnabsx = u;
            int n = 1;
            do
            {
                n++;
                u *= -((n - 1) * t) / n;
                lnabsx += u;
            } while (u > e || u < -e);
            return lnabsx;
        }
    }
}
//string responseStr = "<form method='POST'><span>Введите х</span><input></input><button type='submit'>Отправить</button></form>";