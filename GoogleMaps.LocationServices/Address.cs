using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace GoogleMaps.LocationServices
{
    public class AddressData
    {
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Country { get; set; }


        public override string ToString()
        {
            return String.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}{2}{3}{4}",
                Address != null ? Address + ", " : "",
                City != null ? City + ", " : "",
                State != null ? State + ", " : "",
                Zip != null ? Zip + ", " : "",
                Country != null ? Country : "").TrimEnd(' ', ',');
        }
    }
}
