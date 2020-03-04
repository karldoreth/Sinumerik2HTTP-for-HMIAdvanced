using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using NDde.Client;

namespace DDEtoHTTPrequest
{
    class Program
    {


        static void Main(string[] args)
        {
            Console.WriteLine("Willkommen zum DDE2HTTP-Host von Karl Doreth. Host wird gestartet.");
            try
            {
                //Konfigurationsdatei wird eingelesen
                System.IO.StreamReader Konfigurationsdatei = new System.IO.StreamReader("settings.ini");
                String Prefix = Konfigurationsdatei.ReadLine();
                String Application = Konfigurationsdatei.ReadLine();
                String Topic = Konfigurationsdatei.ReadLine();
                //Serveranwendung wird gestartet
                SimpleListener(Prefix, Application, Topic);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static void SimpleListener(string prefix, string Application, string Topic)
        {

            // Listener erstellen, Prefix einfügen und Starten
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            //Verbindung zu DDE-Server herstellen
            DdeClient KarlsClient = new DdeClient(Application, Topic);
            while (KarlsClient.IsConnected == false)//Versuch starten
            {
                try
                {
                    KarlsClient.Connect();
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Fehler bei der DDE-Verbindung. Erneuter Versuch in 5 Sekunden.");
                    Console.ResetColor();
                    System.Threading.Thread.Sleep(5000);
                }
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Einsatzbereit...");
            Console.ResetColor();

            while (true)
            {
               
                // Note: The GetContext method blocks while waiting for a request. 
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                String Antwort = "";
                //Try für die Anfrage. Bei einem Fehler wird diese auch über HTTP gepostet.
                try
                {
                    //Get Anfragen auswerten
                    String Gets = request.Url.Query.Remove(0, 1);
                    Gets = Uri.UnescapeDataString(Gets);
                    String[] Getarray = Gets.Split('&');
                    foreach (String i in Getarray)
                    {

                        if (i.Split('=')[0] == "request")
                        {
                            Antwort = Antwort + ";" + KarlsClient.Request(i.Split('=')[1], 5000).TrimEnd('\0');
                            //Console.WriteLine("- Anfrage: " + i.Split('=')[1]);
                        }

                        if (i.Split('=')[0] == "poke")
                        {
                            string Variable = i.Split('=')[1];
                            string Wert = i.Split('=')[2];
                            Wert = Wert + '\0';
                            KarlsClient.Poke(Variable, Wert, 5000);
                            //Console.WriteLine("- Anfrage: " + i.Split('=')[1]);
                        }
                    }
                Antwort = Antwort.Remove(0, 1);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Antwort = e.Message;
                }

                // Erstellen des Antwortobjektes
                HttpListenerResponse response = context.Response;
                string responseString = Antwort;
                response.AddHeader("Content-type", "text/html");
                response.AddHeader("Pragma", "no-cache");
                response.AddHeader("Cache-Control", "no-cache");
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                // You must close the output stream.
                output.Close();
               // Console.WriteLine("\nAnfrage von " + request.RemoteEndPoint + "\n" + request.Url.OriginalString);
        }
            listener.Stop();
            KarlsClient.Disconnect();
        }
    }
}
