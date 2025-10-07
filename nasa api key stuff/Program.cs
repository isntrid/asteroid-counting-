using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json.Linq;

namespace nasa_api_key_stuff
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string apiKey = "your key here";
            string startDate = DateTime.Today.ToString("yyyy-MM-dd");
            string endDate = startDate;

            string url = $"https://api.nasa.gov/neo/rest/v1/feed?start_date={startDate}&end_date={endDate}&api_key={apiKey}";

            using HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();

            JObject data = JObject.Parse(json);

            int counter = 0;

            var NearEarthObjs = data["near_earth_objects"]?[startDate];

            if (NearEarthObjs == null)
            {
                Console.WriteLine("No data available for today.");
                return;
            }

            var saved = new List<JToken>();

            foreach (var neo in NearEarthObjs)
            {
                bool hazard = (bool)neo["is_potentially_hazardous_asteroid"];

                if (hazard)
                {
                    counter++;
                    string name = neo["name"].ToString();
                    saved.Add(neo);

                }
            }

            double percent = (double)counter / (int)data["element_count"] * 100;

            string verb = counter == 1 ? "was" : "were";
            string noun = counter == 1 ? "asteroid" : "asteroids";
            Console.WriteLine($"There {verb} {counter} potentially dangerous {noun} today! That's {percent:F0}% of the total amount of asteroids today ({data["element_count"]}).");


            foreach (var asteroid in saved) { Console.WriteLine(asteroid); }


            string path = "ast_data.csv";
            string dataLine = $"{startDate}, {data["element_count"]}, {counter}";


            Console.Write("Do you wish to view the history? \n");
            string answer = Console.ReadLine();

            Program programInstance = new Program();

            while (true)
            {
                if (answer.ToLowerInvariant() == "yes" || answer.ToLowerInvariant() == "y")
                {
                    programInstance.logger(counter, NearEarthObjs, path, dataLine);
                    break;
                }
                else if (answer.ToLowerInvariant() == "no" || answer.ToLowerInvariant() == "n")
                {
                    Console.WriteLine("Thank you for using my app. Have a nice day.");
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid input, please try again (Y / N).");
                    answer = Console.ReadLine();
                }
            }


            File.AppendAllText(path, dataLine + Environment.NewLine);

        }



        void logger(int counter, JToken NearEarthObjs, string path, string dataLine)
        {

            Console.WriteLine("Specify the range in which you'd like you'd like to see the average asteroid amount (e.g: '7' for seven days).");
            int desiredRange = int.Parse(Console.ReadLine());


            string[] asteroid = File.ReadAllLines(path);
            int count = asteroid.Length;
            double totalAstCount = 0;
            double totalHazardAstCount = 0;
            string currentDate = string.Empty;

            bool valid = false;


            int range = Math.Min(desiredRange, count);

            while (!valid)
            {
                if (count >= desiredRange)
                {
                    for (int i = 0; i < range; i++)
                    {
                        var columns = asteroid[i].Split(',');
                        currentDate = columns[0];
                        int astCount = int.Parse(columns[1]);
                        int hazardCount = int.Parse(columns[2]);


                        totalAstCount += astCount;
                        totalHazardAstCount += hazardCount;
                        valid = true;

                    }
                }
                else
                {
                    Console.WriteLine($"Sorry, but currently our data only has {count} near earth objects, please choose a range that is less than or equal to that.");
                    desiredRange = int.Parse(Console.ReadLine());
                }
            }


            double avg = (totalHazardAstCount / totalAstCount) * 100;

            Console.WriteLine($"\nIn {range} days, there have been {totalAstCount} near earth objects (NEOs), out of which, there have been {totalHazardAstCount} hazardous ones, \n" +
             $"this means that {avg:F0}% of NEOs were hazardous.");
        }
    }
}                
