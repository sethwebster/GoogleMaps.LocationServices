using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GoogleMaps.Geolocation
{
    public interface ILocationService
    {
        /// <summary>
        /// Translates a Latitude / Longitude into a Region (state)
        /// </summary>
        /// <param name="Latitude"></param>
        /// <param name="Longitude"></param>
        /// <returns></returns>
        Region GetRegionFromLatLong(double Latitude, double Longitude);
        MapPoint GetLatLongFromAddress(string Address);
        Directions GetDirections(double Latitude, double Longitude);
        Directions GetDirections(Address fromAddress, Address toAddress);

    }
}
