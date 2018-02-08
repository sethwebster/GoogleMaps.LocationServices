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
                },
                new AddressData
                {
                    Address = "407 N Maple Dr. #1",
                    City = "Beverly Hills",
                    State = "CA"
                }
            };

            var gls = new GoogleLocationService("AIzaSyAzXXP9EpudRef0ac4ggTt4tmhFQ_8fsc4");
            var results = addresses.Select(a =>
            {
                var latlong = gls.GetLatLongFromAddress(a);
                if (latlong == null) return new {Success = false, Forward = a.ToString(), Reverse = "" };
                var latitude = latlong.Latitude;
                var longitude = latlong.Longitude;
                var reversedAddress = gls.GetAddressFromLatLang(latlong.Latitude, latlong.Longitude);
                return new
                {
                    Success = true,
                    Forward = $"Address {a} is at {latlong.Latitude}, {latlong.Longitude}",
                    Reverse = $"Reversed Address {reversedAddress} from {latlong.Latitude}, {latlong.Longitude}",
                };
            });
            foreach (var result in results)
            {
                if (result == null) continue;
                System.Console.WriteLine($"{result.Success}: {result.Forward}");
                System.Console.WriteLine($"{result.Success}: {result.Reverse}");
            }
            System.Console.ReadLine();
        }
    }
}
