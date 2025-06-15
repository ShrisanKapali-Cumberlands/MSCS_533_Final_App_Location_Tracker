using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location_Tracker.Models
{
    public class HeatPoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Intensity { get; set; }
        public double Radius { get; set; }
    }
}
