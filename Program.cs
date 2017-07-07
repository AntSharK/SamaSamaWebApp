using Microsoft.Owin.Hosting;
using System;

namespace SamaSamaLAN
{
    public class Program
    {
        static void Main()
        {
            string baseAddress = "http://localhost:9000";

            // Start OWIN host 
            using (WebApp.Start<Startup>(url: baseAddress))
            {
                Console.ReadLine();
            }
        }
    }
}