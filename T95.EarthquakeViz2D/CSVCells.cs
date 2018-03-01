using System.Drawing;

namespace T95.EarthquakeViz2D
{
    public class CSVCells
    {
        public string Place { get; set; }
        public float Magnitude { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public Point Coordinates { get; set; }
        public string Updated { get; set; }
        public string Type { get; set; }
        public string Id { get; set; }


        public CSVCells(string Place, float Magnitude, float Latitude, float Longitude,string Updated, string Type, string Id)
        {
            this.Place = Place;
            this.Magnitude = Magnitude;
            this.Latitude = Latitude;
            this.Longitude = Longitude;
            this.Updated = Updated;
            this.Type = Type;
            this.Id = Id;
        }
        public CSVCells(string Place, Point Coordinates,string Updated, float Magnitude,string Type)
        {
            this.Place = Place;
            this.Coordinates = Coordinates;
            this.Updated = Updated;
            this.Magnitude = Magnitude;
            this.Type = Type;
        }
    }
}
