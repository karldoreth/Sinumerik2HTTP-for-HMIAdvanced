using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using NDde.Client;
using System.IO;

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
                // Wartet auf die Anfrage durch mich im Browser
                // http://localhost:8080/?request=hallo)
                // http://localhost:8080/?write=R-Parameter1&wert=150

                //Meldung an Console in Gelb.
                DebugToFile("Warte auf Anfrage...");

                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                String Antwort = "";
                DebugToFile("Abfrage");

                //Try für die Anfrage. Bei einem Fehler wird diese auch über HTTP gepostet.
                try
                {
                    //Get Anfragen auswerten
                    String Gets = request.Url.Query.Remove(0, 1);
                    Gets = Uri.UnescapeDataString(Gets);
                    
                    //Schreiben?
                    if (Gets.Split('=')[0] == "write")
                    {
                        //Korrekt aufteilen.
                        string[] Befehlsteile = Gets.Split('&');
                        string[] Variablenbefehl = Befehlsteile[0].Split('=');
                        string[] Wertbefehl = Befehlsteile[1].Split('=');
                        DebugToFile("Schreibe...");

                        //Schreibe über CAP
                        string Variablenname = Variablenbefehl[1];
                        string Variablenwert = Wertbefehl[1];
                        KarlsClient.Poke(Variablenname, Variablenwert, 5000);

                        //Antwort via http
                        Antwort = " true";
                    }
                    else if (Gets.Split('=')[0] == "request")
                    {
                        string[] Befehlsteile = Gets.Split('&'); //Wenn mehrere Variablen abgefragt werden.
                        foreach (String EinBefehlsteil in Befehlsteile)
                        {
                            string[] Lesebefehl = EinBefehlsteil.Split('=');
                            DebugToFile("Lese");

                            string Variablenname = Lesebefehl[1];
                            
                            Antwort = Antwort + KarlsClient.Request(Variablenname, 5000) + ";";
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

        private static void DebugToFile(string text)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(text);
            Console.ResetColor();

            //Console.ReadLine();
            System.IO.StreamWriter SW = new StreamWriter("Debug.txt", true);
            string zeit = DateTime.Now.ToString();
            SW.WriteLine(zeit + "; " + text);
            SW.Close();

        }
    }
}
