using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace GoogleMaps.LocationServices
{
    public class GoogleLocationService : ILocationService
    {
        #region Constants
        #endregion


        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleLocationService"/> class.
        /// </summary>
        /// <param name="useHttps">Indicates whether to call the Google API over HTTPS or not.</param>
        public GoogleLocationService(bool useHttps)
        {
            APIKey = "";
            UseHttps = useHttps;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleLocationService"/> class. Default calling the API over regular
        /// HTTP (not HTTPS).
        /// </summary>
        public GoogleLocationService()
            : this(false)
        {
            APIKey = "";
        }

        public GoogleLocationService(string apikey)
        {
            APIKey = apikey;
            UseHttps = true;
        }
        #endregion


        #region Properties
        /// <summary>
        /// Gets a value indicating whether to use the Google API over HTTPS.
        /// </summary>
        /// <value>
        ///   <c>true</c> if using the API over HTTPS; otherwise, <c>false</c>.
        /// </value>
        public bool UseHttps { get; private set; }


        private string APIKey { get; set; }

        private string UrlProtocolPrefix
        {
            get
            {
                return UseHttps ? "https://" : "http://";
            }
        }


        protected string APIUrlRegionFromLatLong
        {
            get
            {
                return UrlProtocolPrefix + Constants.ApiUriTemplates.ApiRegionFromLatLong;
            }
        }

        protected string APIUrlLatLongFromAddress
        {
            get
            {
                return UrlProtocolPrefix + Constants.ApiUriTemplates.ApiLatLongFromAddress;
            }
        }

        protected string APIUrlDirections
        {
            get
            {
                return UrlProtocolPrefix + Constants.ApiUriTemplates.ApiDirections;
            }
        }
        #endregion


        #region Public instance methods
        /// <summary>
        /// Translates a Latitude / Longitude into a Region (state) using Google Maps api
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public Region GetRegionFromLatLong(double latitude, double longitude)
        {
            XDocument doc = XDocument.Load(string.Format(CultureInfo.InvariantCulture, APIUrlRegionFromLatLong, latitude, longitude) + "&key=" + APIKey);

            var els = doc.Descendants("result").First().Descendants("address_component").FirstOrDefault(s => s.Descendants("type").First().Value == "administrative_area_level_1");
            if (null != els)
            {
                return new Region() { Name = els.Descendants("long_name").First().Value, ShortCode = els.Descendants("short_name").First().Value };
            }
            return null;
        }

        /// <summary>
        /// Translates a Latitude / Longitude into an address using Google Maps api
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public AddressData GetAddressFromLatLang(double latitude, double longitude)
        {
            var addressShortName = string.Empty;
            var addressCountry = string.Empty;
            var addressAdministrativeAreaLevel1 = string.Empty;
            var addressAdministrativeAreaLevel2 = string.Empty;
            var addressAdministrativeAreaLevel3 = string.Empty;
            var addressColloquialArea = string.Empty;
            var addressLocality = string.Empty;
            var addressSublocality = string.Empty;
            var addressNeighborhood = string.Empty;
            var addressRoute = string.Empty;
            var addressStreetNumber = string.Empty;
            var addressPostalCode = string.Empty;

            XmlDocument doc = new XmlDocument();

            doc.Load(string.Format(CultureInfo.InvariantCulture, APIUrlRegionFromLatLong, latitude, longitude) + "&key=" + APIKey);
            var element = doc.SelectSingleNode("//GeocodeResponse/status");
            if (element == null || element.InnerText == Constants.ApiResponses.ZeroResults)
            {
                return null;
            }

            XmlNodeList xnList = doc.SelectNodes("//GeocodeResponse/result/address_component");

            if (xnList == null) return null;

            foreach (XmlNode xn in xnList)
            {
                var longname = xn["long_name"].InnerText;
                var shortname = xn["short_name"].InnerText;
                var typename = xn["type"]?.InnerText;

                switch (typename)
                {
                    case "country":
                        addressCountry = longname;
                        addressShortName = shortname;
                        break;
                    case "locality":
                        addressLocality = longname;
                        break;
                    case "sublocality":
                        addressSublocality = longname;
                        break;
                    case "neighborhood":
                        addressNeighborhood = longname;
                        break;
                    case "colloquial_area":
                        addressColloquialArea = longname;
                        break;
                    case "administrative_area_level_1":
                        addressAdministrativeAreaLevel1 = shortname;
                        break;
                    case "administrative_area_level_2":
                        addressAdministrativeAreaLevel2 = longname;
                        break;
                    case "administrative_area_level_3":
                        addressAdministrativeAreaLevel3 = longname;
                        break;
                    case "route":
                        addressRoute = shortname;
                        break;
                    case "street_number":
                        addressStreetNumber = shortname;
                        break;
                    case "postal_code":
                        addressPostalCode = longname;
                        break;
                }
            }

            return new AddressData
            {
                Country = addressCountry,
                State = addressAdministrativeAreaLevel1,
                City = addressLocality,
                Address = addressStreetNumber + " " + addressRoute + " " + addressSublocality,
                Zip = addressPostalCode,
            };
        }



        /// <summary>
        /// Gets the latitude and longitude that belongs to an address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns></returns>
        /// <exception cref="System.Net.WebException"></exception>
        public MapPoint GetLatLongFromAddress(string address)
        {
            XDocument doc = XDocument.Load(string.Format(CultureInfo.InvariantCulture, APIUrlLatLongFromAddress, Uri.EscapeDataString(address)) + "&key=" + APIKey);

            string status = doc.Descendants("status").FirstOrDefault().Value;
            if (status == Constants.ApiResponses.OverQueryLimit)
            {
                throw new System.Net.WebException("QueryLimit exceeded, check your dashboard");
            }

            if (status == Constants.ApiResponses.RequestDenied)
            {
                throw new System.Net.WebException("Request denied, it's likely you need to enable the necessary Google maps APIs");
            }

            var els = doc.Descendants("result").Descendants("geometry").Descendants("location").FirstOrDefault();
            if (null != els)
            {
                var latitude = ParseUS((els.Nodes().First() as XElement).Value);
                var longitude = ParseUS((els.Nodes().ElementAt(1) as XElement).Value);
                return new MapPoint() { Latitude = latitude, Longitude = longitude };
            }
            return null;
        }

        /// <summary>
        /// Gets the latitude and longitude that belongs to an address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns></returns>
        public MapPoint GetLatLongFromAddress(AddressData address)
        {
            return GetLatLongFromAddress(address.ToString());
        }

        /// <summary>
        /// Gets an array of string addresses that matched a possibly ambiguous address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns></returns>
        /// <exception cref="System.Net.WebException"></exception>
        public string[] GetAddressesListFromAddress(string address)
        {

            XDocument doc = XDocument.Load(string.Format(CultureInfo.InvariantCulture, APIUrlLatLongFromAddress, Uri.EscapeDataString(address)) + "&key=" + APIKey);
            var status = doc.Descendants("status").FirstOrDefault().Value;

            if (status == Constants.ApiResponses.OverQueryLimit)
            {
                throw new System.Net.WebException("QueryLimit exceeded, check your dashboard");
            }

            if (status == Constants.ApiResponses.RequestDenied)
            {
                throw new System.Net.WebException("Request denied, it's likely you need to enable the necessary Google maps APIs");
            }

            var results = doc.Descendants("result").Descendants("formatted_address").ToArray();
            var addresses = (from elem in results select elem.Value).ToArray();
            if (addresses.Length > 0) return addresses;
            return null;
        }

        /// <summary>
        /// Gets an array of string addresses that matched a possibly ambiguous address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns></returns>
        /// <exception cref="System.Net.WebException"></exception>
        public string[] GetAddressesListFromAddress(AddressData address)
        {
            return GetAddressesListFromAddress(address.ToString());
        }


        /// <summary>
        /// Gets the directions.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Directions GetDirections(double latitude, double longitude)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the directions.
        /// </summary>
        /// <param name="originAddress">From address.</param>
        /// <param name="destinationAddress">To address.</param>
        /// <returns>The directions</returns>
        public Directions GetDirections(AddressData originAddress, AddressData destinationAddress)
        {
            Directions direction = new Directions();

            XDocument xdoc = XDocument.Load(String.Format(CultureInfo.InvariantCulture,
                                                APIUrlDirections,
                                                Uri.EscapeDataString(originAddress.ToString()),
                                                Uri.EscapeDataString(destinationAddress.ToString())) + "&key=" + APIKey);

            var status = (from s in xdoc.Descendants("DirectionsResponse").Descendants("status")
                          select s).FirstOrDefault();

            if (status != null && status.Value == "OK")
            {
                direction.StatusCode = Directions.Status.OK;
                var distance = (from d in xdoc.Descendants("DirectionsResponse").Descendants("route").Descendants("leg")
                               .Descendants("distance").Descendants("text")
                                select d).LastOrDefault();

                if (distance != null)
                {
                    direction.Distance = distance.Value;
                }

                var duration = (from d in xdoc.Descendants("DirectionsResponse").Descendants("route").Descendants("leg")
                               .Descendants("duration").Descendants("text")
                                select d).LastOrDefault();

                if (duration != null)
                {
                    direction.Duration = duration.Value;
                }

                var steps = from s in xdoc.Descendants("DirectionsResponse").Descendants("route").Descendants("leg").Descendants("step")
                            select s;

                foreach (var step in steps)
                {
                    Step directionStep = new Step();

                    directionStep.Instruction = step.Element("html_instructions").Value;
                    directionStep.Distance = step.Descendants("distance").First().Element("text").Value;
                    direction.Steps.Add(directionStep);

                }
                return direction;
            }
            else if (status != null && status.Value != "OK")
            {
                direction.StatusCode = Directions.Status.FAILED;
                return direction;
            }
            else
            {
                throw new Exception("Unable to get Directions from Google");
            }

        }
        #endregion

        double ParseUS(string value)
        {
            return Double.Parse(value, new CultureInfo("en-US"));
        }
    }
}
