using SQLite;


namespace Location_Tracker.Models
{
    public class LocationPoint
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public DateTime Timestamp { get; set; }

        public double? Accuracy { get; set; }
    }
}
