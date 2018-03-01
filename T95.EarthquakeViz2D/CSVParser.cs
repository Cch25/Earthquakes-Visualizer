using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;

namespace T95.EarthquakeViz2D
{
    public class CSVParser
    {
        private string ConnectToSite(string url)
        {
            string result = "404";
            try
            {
                HttpWebRequest req = WebRequest.CreateHttp(url);
                HttpWebResponse res = req.GetResponse() as HttpWebResponse;
                using (StreamReader sr = new StreamReader(res.GetResponseStream()))
                {
                    result = sr.ReadToEnd();
                    sr.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Timeout");
            }

            return result;
        }

        public List<CSVCells> SplitCSVFile(string ConnectTo)
        {
            //http://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_day.csv
            //http://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_month.csv
            //https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_week.csv
            List<CSVCells> list = new List<CSVCells>();
            string res = "";
            res = ConnectToSite(ConnectTo);
            string[] lineItems = res.Split('\n');
            for (int i = 1; i < lineItems.Length - 1; i++)
            {
                string[] tempItems = lineItems[i].SplitWithQualifier(',', '"', true);
                try
                {
                    list.Add(new CSVCells(Convert.ToString(tempItems[13]),
                        float.Parse(tempItems[4], CultureInfo.InvariantCulture),
                        float.Parse(tempItems[1], CultureInfo.InvariantCulture),
                        float.Parse(tempItems[2], CultureInfo.InvariantCulture),
                        Convert.ToString(tempItems[12]),
                        Convert.ToString(tempItems[14]),
                        Convert.ToString(tempItems[11]) ));
                }
                catch (Exception) {  };
            }
            return list;
        }
    }
}
