using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleMaps.LocationServices.Console
{
    class Program
    {
        public static void Main(string[] args)
        {
            AddressData[] addresses = new AddressData[] 
            {
                new AddressData // Belgium
                {
                    Address = "Rue du Cornet 6",
                    City = "VERVIERS",
                    State = null,
                    Country = "Belgium",
                    Zip = "B-4800"
                },
                new AddressData
                {
                    Address = "1600 Pennsylvania ave",
                    City = "Washington",
                    State = "DC"
                }
            };

            var gls = new GoogleLocationService();
            foreach (var address in addresses)
            {
                var latlong = gls.GetLatLongFromAddress(address);
                var Latitude = latlong.Latitude;
                var Longitude = latlong.Longitude;
                System.Console.WriteLine("Address ({0}) is at {1},{2}", address, Latitude, Longitude);
            }
            System.Console.ReadLine();
        }
    }
}
