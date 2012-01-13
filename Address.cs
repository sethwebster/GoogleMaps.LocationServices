using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GoogleMaps.Geolocation
{
    public class Address
    {
        public string Address1 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }


        public override string ToString()
        {
            return String.Format("{0},{1},{2} {3}",Address1 != null ? Address1 : "", City != null ? City : "", State != null ? State : "", Zip != null ? Zip : "");
        }
    }
}
