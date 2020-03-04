using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using NDde.Client;


namespace HTTP_Poke_lesen
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Befehl in Browser eingeben");
            //try
            //{
                //Konfigurationsdatei wird eingelesen
                System.IO.StreamReader Konfigurationsdatei = new System.IO.StreamReader("settings.ini");
                String Prefix = Konfigurationsdatei.ReadLine(); //http://*:8080/
                String Application = Konfigurationsdatei.ReadLine(); //myapp
                String Topic = Konfigurationsdatei.ReadLine(); //mytopic  
                //Serveranwendung wird gestartet
                SimpleListener(Prefix, Application, Topic);
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e.Message);
            //}

        }


        public static void SimpleListener(string prefix, string Application, string Topic)
        {

            // Listener erstellen, Prefix einfügen und Starten
            // HttpListener "lauscht" um auf zugriffe zu reagieren
            // Mit diesem Aufruf erstellen wir uns das Listener Objekt.
            // listener ist das Objekt. Objekte könnefn Methoden (beschleunigen_auf(100km/h) und Eigenschaften (ferrari,rot) beinhalten

            HttpListener listener = new HttpListener();

            // Diesem teilen wir nun mit auf was er lauschen soll
            // Methode vom Objekt listener ausführen und Eigenschat anpasssen 
            listener.Prefixes.Add(prefix);

            // Jetzt muss dieser nur noch gestartet werden.
            listener.Start();

            DdeClient KarlsClient = new DdeClient(Application, Topic);

            // Greift auf das Objekt KarlsClient zu und "fragt" ob dieser Verdunden ist
            while (KarlsClient.IsConnected == false)//Versuch starten
            {
                try
                {
                    // Methode des Obejktes KarlsClient aufrufen und zwar soll verbunden werden.
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
                // Wartet auf die Anfrage durch mich im Browser (mit URL http://localhost:8080/?request=hallo)
                // in RawUrl steht meine Anfrage "/?request=hallo"	string

                // http://localhost:8080/?request=hallo)
                // http://localhost:8080/?variable=R-Parameter1&wert=150
                // Prüfen ob es anfrage oder Schreibbefehl ist

                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                String Antwort = "";
                //Try für die Anfrage. Bei einem Fehler wird diese auch über HTTP gepostet.
                //try
                //{
                    //Get Anfragen auswerten
                    // In dem Objekt request wird auf die Anfrage url eingegangen
                    // in Gets steht als string "?request=hallo"
                    // Entfernt wird das Fragezeichen aus dem String der URL
                    // Als Ergebnis der Operation steht in Gets => request=hallo 

                    // "variable=R-Parameter1&wert=150"
                    // "request=hallo"


                    String Gets = request.Url.Query.Remove(0, 1);

                    // Konvertiert eine Zeichenfolge in eine Darstellung ohne Escapezeichen
                    // Was soll das bringen? //Das bestimmte Zeicen vernünftig dargestellt werden
                   Gets = Uri.UnescapeDataString(Gets);
                                          
                   if (Gets.Split('=')[0] == "variable")
                   {
                        //Welcher Parameter. Hier ist der Wert R-Parameter1
                       //Entfernt variable=
                       string Befehl = Gets.Remove(0, "variable=".Length);
                       // R-Parameter1&wert=150
                       string Variable = Befehl.Split('&')[0];
                       string Wert = Befehl.Split('=')[1];
                       Console.WriteLine("{0}={1}", Variable, Wert);
                       KarlsClient.Poke(Variable, Wert, 1000);
                       Antwort = "true";
                   }
                   
                //}
                //catch (Exception e)
                //{
                //    Console.WriteLine(e.Message);
                //    Antwort = e.Message;
                //}

                // Erstellen des Antwortobjektes
                // Stellt eine Antwort auf eine Anforderung dar, die von einem HttpListener-Objekt behandelt wird
                HttpListenerResponse response = context.Response;
                // string responseString = Antwort;

                // Fügt den HTTP-Headern für diese Antwort den angegebenen Header und Wert hinzu
                // einzig Wirksamme ist das und änder lediglich die Formatierung der Zahl
                response.AddHeader("Content-type", "text/html");
                // response.AddHeader("Pragma", "no-cache");
                // response.AddHeader("Cache-Control", "no-cache");

                // In UTF8 decodieren der Antowrt und Bytes zerlegen. Die Zahl 29 ist 50 (=>2) und 57 (=>9)
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(Antwort);

                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                // You must close the output stream.
                output.Close();
                // Console.WriteLine("\nAnfrage von " + request.RemoteEndPoint + "\n" + request.Url.OriginalString);
            }
        }
    }
}
