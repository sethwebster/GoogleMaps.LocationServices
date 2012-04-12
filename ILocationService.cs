using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GoogleMaps.LocationServices
{
    public interface ILocationService
    {
        /// <summary>
        /// Translates a Latitude / Longitude into a Region (state)
        /// </summary>
        /// <param name="Latitude"></param>
        /// <param name="Longitude"></param>
        /// <returns></returns>
        Region GetRegionFromLatLong(double latitude, double longitude);
        MapPoint GetLatLongFromAddress(string address);
        Directions GetDirections(double latitude, double longitude);
        Directions GetDirections(AddressData fromAddress, AddressData toAddress);

    }
}
