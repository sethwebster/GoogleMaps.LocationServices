using System;
using System.Linq;

namespace GoogleMaps.LocationServices.Console;

public static class Program
{
    public static void Main(string[] args)
    {
        AddressData[] addresses =
        [
            // Resolve from post code only
            new AddressData
            {
                Zip = "10025",
            },
            // Non-US
            new AddressData // Belgium
            {
                Address = "Rue du Cornet 6",
                City = "VERVIERS",
                State = string.Empty,
                Country = "Belgium",
                Zip = "B-4800",
            },
            new AddressData
            {
                Address = "1600 Pennsylvania ave",
                City = "Washington",
                State = "DC",
            },
            new AddressData
            {
                Address = "407 N Maple Dr. #1",
                City = "Beverly Hills",
                State = "CA",
            },
        ];

        // You'll need to acquire your own Google Maps API key.
        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY") ?? "YOUR_API_KEY";
        var gls = new GoogleLocationService(apiKey);

        var results = addresses.AsParallel().Select(a =>
        {
            var latlong = gls.GetLatLongFromAddress(a);
            if (latlong == null)
            {
                return new { Success = false, Forward = a.ToString(), Reverse = "" };
            }

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
