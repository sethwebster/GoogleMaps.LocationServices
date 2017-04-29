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

            var gls = new GoogleLocationService();
            foreach (var address in addresses)
            {
                try
                {
                    var latlong = gls.GetLatLongFromAddress(address);
                    if (latlong == null) continue;
                    var latitude = latlong.Latitude;
                    var longitude = latlong.Longitude;
                    System.Console.WriteLine("Address ({0}) is at {1},{2}", address, latitude, longitude);
                    var reversedAddress = gls.GetAddressFromLatLang(latitude, longitude);
                    System.Console.WriteLine("Reversed Address from ({1},{2}) is {0}", reversedAddress, latitude, longitude);
                    System.Console.WriteLine("=======================================");
                }
                catch(System.Net.WebException ex)
                {
                    System.Console.WriteLine("Google Maps API Error {0}", ex.Message);
                }
                
            }
            System.Console.ReadLine();
        }
    }
}
