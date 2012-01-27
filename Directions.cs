using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GoogleMaps.LocationServices
{
    public class Directions
    {
        public enum Status
        {
            OK,
            FAILED
        }
        public Directions()
        {
            Steps = new List<Step>();
        }
        public List<Step> Steps { get; set; }
        public string Duration { get; set; }
        public string Distance { get; set; }

        public Status StatusCode { get; set; }
    }
}
