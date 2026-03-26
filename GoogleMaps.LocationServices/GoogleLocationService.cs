using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace GoogleMaps.LocationServices;

public class GoogleLocationService : ILocationService, IDisposable
{
    private const int DefaultMaxRetryAttempts = 3;
    private const int DefaultRetryDelayMilliseconds = 250;
    private const int DefaultRequestTimeoutSeconds = 10;
    private const int DefaultMaxDelayMilliseconds = 5000;

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly Random _jitter;

    #region Constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleLocationService"/> class.
    /// </summary>
    /// <param name="useHttps">Indicates whether to call the Google API over HTTPS or not.</param>
    public GoogleLocationService(bool useHttps)
        : this(string.Empty, useHttps)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleLocationService"/> class.
    /// Defaults to HTTPS for modern API compatibility.
    /// </summary>
    public GoogleLocationService()
        : this(true)
    {
    }

    public GoogleLocationService(string apiKey)
        : this(apiKey, true)
    {
    }

    public GoogleLocationService(string apiKey, HttpClient httpClient)
        : this(apiKey, true, httpClient)
    {
    }

    protected GoogleLocationService(string apiKey, bool useHttps, HttpClient? httpClient = null)
    {
        APIKey = apiKey;
        UseHttps = useHttps;
        _httpClient = httpClient ?? new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        _ownsHttpClient = httpClient == null;

        MaxRetryAttempts = DefaultMaxRetryAttempts;
        RetryDelay = TimeSpan.FromMilliseconds(DefaultRetryDelayMilliseconds);
        RequestTimeout = TimeSpan.FromSeconds(DefaultRequestTimeoutSeconds);
        _jitter = new Random();

        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("GoogleMaps.LocationServices/2.1.0");
        }
    }

    ~GoogleLocationService()
    {
        Dispose(false);
    }
    #endregion


    #region Properties
    /// <summary>
    /// Gets a value indicating whether to use the Google API over HTTPS.
    /// </summary>
    public bool UseHttps { get; private set; }

    /// <summary>
    /// Number of attempts (including retries) to request Google APIs.
    /// Set to 1 to disable retries.
    /// </summary>
    public int MaxRetryAttempts { get; set; }

    /// <summary>
    /// Timeout per request attempt.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; }

    /// <summary>
    /// Base delay for exponential backoff retry.
    /// </summary>
    public TimeSpan RetryDelay { get; set; }

    private string APIKey { get; set; }

    private string UrlProtocolPrefix => UseHttps ? "https://" : "http://";

    protected string APIUrlRegionFromLatLong => UrlProtocolPrefix + Constants.ApiUriTemplates.ApiRegionFromLatLong;

    protected string APIUrlLatLongFromAddress => UrlProtocolPrefix + Constants.ApiUriTemplates.ApiLatLongFromAddress;

    protected string APIUrlDirections => UrlProtocolPrefix + Constants.ApiUriTemplates.ApiDirections;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion

    #region Protected helpers
    protected virtual XDocument LoadXDocumentFromUrl(string requestUrl)
    {
        return LoadXDocumentFromUrlAsync(requestUrl, CancellationToken.None).GetAwaiter().GetResult();
    }

    protected virtual XmlDocument LoadXmlDocumentFromUrl(string requestUrl)
    {
        return LoadXmlDocumentFromUrlAsync(requestUrl, CancellationToken.None).GetAwaiter().GetResult();
    }

    protected virtual async Task<XDocument> LoadXDocumentFromUrlAsync(string requestUrl, CancellationToken cancellationToken = default)
    {
        var content = await LoadResponseStringFromUrlAsync(requestUrl, cancellationToken).ConfigureAwait(false);
        return XDocument.Parse(content);
    }

    protected virtual async Task<XmlDocument> LoadXmlDocumentFromUrlAsync(string requestUrl, CancellationToken cancellationToken = default)
    {
        var content = await LoadResponseStringFromUrlAsync(requestUrl, cancellationToken).ConfigureAwait(false);
        var doc = new XmlDocument();
        using var reader = new StringReader(content);
        doc.Load(reader);
        return doc;
    }

    protected virtual Task<string> BuildAndLoadResponseAsync(string requestUrl, CancellationToken cancellationToken = default)
    {
        return LoadResponseStringFromUrlAsync(requestUrl, cancellationToken);
    }

    protected string BuildRequestUrl(string template, params object[] parameters)
    {
        var rawUrl = string.Format(CultureInfo.InvariantCulture, template, parameters);
        return rawUrl + "&key=" + APIKey;
    }

    protected virtual async Task<string> LoadResponseStringFromUrlAsync(string requestUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestUrl))
        {
            throw new ArgumentException("Request URL cannot be null or whitespace.", nameof(requestUrl));
        }

        if (RequestTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(RequestTimeout), "Request timeout must be greater than zero.");
        }

        if (MaxRetryAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxRetryAttempts), "MaxRetryAttempts must be greater than zero.");
        }

        var attempt = 0;
        Exception? lastError = null;

        while (attempt < MaxRetryAttempts)
        {
            attempt++;

            try
            {
                using var requestTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                requestTokenSource.CancelAfter(RequestTimeout);

                using var response = await _httpClient.GetAsync(requestUrl, requestTokenSource.Token).ConfigureAwait(false);

                if ((int)response.StatusCode == 429 || ((int)response.StatusCode >= 500 && (int)response.StatusCode < 600))
                {
                    var statusMessage = $"Google Maps API returned transient HTTP status {(int)response.StatusCode} ({response.ReasonPhrase}).";

                    if (attempt >= MaxRetryAttempts)
                    {
                        response.EnsureSuccessStatusCode();
                    }

                    throw new HttpRequestException(statusMessage);
                }

                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(body))
                {
                    throw new WebException("Google Maps API returned an empty response.");
                }

                return body;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (IsRetryableError(ex) && attempt < MaxRetryAttempts)
            {
                lastError = ex;
                await WaitForRetry(attempt, cancellationToken).ConfigureAwait(false);
                continue;
            }
            catch (Exception ex) when (IsRetryableError(ex) && attempt >= MaxRetryAttempts)
            {
                lastError = ex;
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        throw lastError ?? new WebException("Failed to retrieve data from Google Maps API after retry attempts.");
    }

    protected virtual bool IsRetryableError(Exception ex)
    {
        if (ex is HttpRequestException)
        {
            return true;
        }

        if (ex is TimeoutException)
        {
            return true;
        }

        if (ex is TaskCanceledException)
        {
            return true;
        }

        return false;
    }

    private async Task WaitForRetry(int attempt, CancellationToken cancellationToken)
    {
        if (RetryDelay <= TimeSpan.Zero)
        {
            return;
        }

        var baseDelay = Math.Min(
            RetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1),
            DefaultMaxDelayMilliseconds);

        var jitterMs = _jitter.NextDouble() * DefaultRetryDelayMilliseconds;
        var totalDelay = TimeSpan.FromMilliseconds(baseDelay + jitterMs);

        await Task.Delay(totalDelay, cancellationToken).ConfigureAwait(false);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
    #endregion

    #region Public instance methods
    /// <summary>
    /// Translates a Latitude / Longitude into a Region (state) using Google Maps api
    /// </summary>
    public Region? GetRegionFromLatLong(double latitude, double longitude)
    {
        return GetRegionFromLatLongAsync(latitude, longitude).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Translates a Latitude / Longitude into a Region (state) using Google Maps api
    /// </summary>
    public async Task<Region?> GetRegionFromLatLongAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var doc = await LoadXDocumentFromUrlAsync(BuildRequestUrl(APIUrlRegionFromLatLong, latitude, longitude), cancellationToken).ConfigureAwait(false);

        var administrativeArea = doc
            .Descendants("result")
            .SelectMany(r => r.Descendants("address_component"))
            .FirstOrDefault(component => component.Descendants("type").Any(t => t.Value == "administrative_area_level_1"));

        if (administrativeArea == null)
        {
            return null;
        }

        return new Region
        {
            Name = administrativeArea.Descendants("long_name").First().Value,
            ShortCode = administrativeArea.Descendants("short_name").First().Value,
        };
    }

    /// <summary>
    /// Translates a Latitude / Longitude into an address using Google Maps api
    /// </summary>
    public AddressData? GetAddressFromLatLang(double latitude, double longitude)
    {
        return GetAddressFromLatLangAsync(latitude, longitude).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Translates a Latitude / Longitude into an address using Google Maps api
    /// </summary>
    public async Task<AddressData?> GetAddressFromLatLangAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var addressCountry = string.Empty;
        var addressAdministrativeAreaLevel1 = string.Empty;
        var addressLocality = string.Empty;
        var addressSublocality = string.Empty;
        var addressRoute = string.Empty;
        var addressStreetNumber = string.Empty;
        var addressPostalCode = string.Empty;

        var doc = await LoadXmlDocumentFromUrlAsync(BuildRequestUrl(APIUrlRegionFromLatLong, latitude, longitude), cancellationToken).ConfigureAwait(false);

        var status = doc.SelectSingleNode("//GeocodeResponse/status");
        if (status == null || status.InnerText == Constants.ApiResponses.ZeroResults)
        {
            return null;
        }

        var addressComponents = doc.SelectNodes("//GeocodeResponse/result/address_component");
        if (addressComponents == null)
        {
            return null;
        }

        foreach (XmlNode addressNode in addressComponents)
        {
            var longName = addressNode["long_name"]?.InnerText ?? string.Empty;
            var shortName = addressNode["short_name"]?.InnerText ?? string.Empty;
            var componentType = addressNode["type"]?.InnerText;

            switch (componentType)
            {
                case "country":
                    addressCountry = longName;
                    break;
                case "locality":
                    addressLocality = longName;
                    break;
                case "sublocality":
                    addressSublocality = longName;
                    break;
                case "administrative_area_level_1":
                    addressAdministrativeAreaLevel1 = shortName;
                    break;
                case "route":
                    addressRoute = shortName;
                    break;
                case "street_number":
                    addressStreetNumber = shortName;
                    break;
                case "postal_code":
                    addressPostalCode = longName;
                    break;
            }
        }

        return new AddressData
        {
            Country = addressCountry,
            State = addressAdministrativeAreaLevel1,
            City = addressLocality,
            Address = string.Join(" ", new[]
            {
                addressStreetNumber,
                addressRoute,
                addressSublocality,
            }.Where(part => !string.IsNullOrWhiteSpace(part))),
            Zip = addressPostalCode,
        };
    }

    /// <summary>
    /// Gets the latitude and longitude that belongs to an address.
    /// </summary>
    /// <param name="address">The address.</param>
    public MapPoint? GetLatLongFromAddress(string address)
    {
        return GetLatLongFromAddressAsync(address).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the latitude and longitude that belongs to an address.
    /// </summary>
    /// <param name="address">The address.</param>
    public async Task<MapPoint?> GetLatLongFromAddressAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("Address cannot be null or whitespace.", nameof(address));
        }

        var doc = await LoadXDocumentFromUrlAsync(BuildRequestUrl(APIUrlLatLongFromAddress, Uri.EscapeDataString(address)), cancellationToken).ConfigureAwait(false);

        string status = doc.Descendants("status").FirstOrDefault()?.Value ?? string.Empty;
        if (status == Constants.ApiResponses.OverQueryLimit)
        {
            throw new WebException("QueryLimit exceeded, check your dashboard");
        }

        if (status == Constants.ApiResponses.RequestDenied)
        {
            throw new WebException("Request denied, it's likely you need to enable the necessary Google maps APIs");
        }

        var elements = doc.Descendants("result").Descendants("geometry").Descendants("location").FirstOrDefault();
        if (elements == null)
        {
            return null;
        }

        var locationValues = elements.Elements("lat").Concat(elements.Elements("lng")).ToArray();
        if (locationValues.Length < 2)
        {
            return null;
        }

        var latitudeValue = ParseUS(locationValues[0].Value);
        var longitudeValue = ParseUS(locationValues[1].Value);
        return new MapPoint { Latitude = latitudeValue, Longitude = longitudeValue };
    }

    /// <summary>
    /// Gets the latitude and longitude that belongs to an address.
    /// </summary>
    /// <param name="address">The address.</param>
    public MapPoint? GetLatLongFromAddress(AddressData address) => GetLatLongFromAddressAsync(address, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>
    /// Gets the latitude and longitude that belongs to an address.
    /// </summary>
    /// <param name="address">The address.</param>
    public Task<MapPoint?> GetLatLongFromAddressAsync(AddressData address, CancellationToken cancellationToken = default)
    {
        if (address == null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        return GetLatLongFromAddressAsync(address.ToString(), cancellationToken);
    }

    /// <summary>
    /// Gets an array of string addresses that matched a possibly ambiguous address.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <returns></returns>
    public string[]? GetAddressesListFromAddress(string address)
    {
        return GetAddressesListFromAddressAsync(address).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets an array of string addresses that matched a possibly ambiguous address.
    /// </summary>
    /// <param name="address">The address.</param>
    public async Task<string[]?> GetAddressesListFromAddressAsync(string address, CancellationToken cancellationToken = default)
    {
        var doc = await LoadXDocumentFromUrlAsync(BuildRequestUrl(APIUrlLatLongFromAddress, Uri.EscapeDataString(address)), cancellationToken).ConfigureAwait(false);
        var status = doc.Descendants("status").FirstOrDefault()?.Value;

        if (status == Constants.ApiResponses.OverQueryLimit)
        {
            throw new WebException("QueryLimit exceeded, check your dashboard");
        }

        if (status == Constants.ApiResponses.RequestDenied)
        {
            throw new WebException("Request denied, it's likely you need to enable the necessary Google maps APIs");
        }

        var addresses = doc.Descendants("result").Descendants("formatted_address").Select(elem => elem.Value).ToArray();
        if (addresses.Length > 0)
        {
            return addresses;
        }

        return null;
    }

    /// <summary>
    /// Gets an array of string addresses that matched a possibly ambiguous address.
    /// </summary>
    /// <param name="address">The address.</param>
    public string[]? GetAddressesListFromAddress(AddressData address) => GetAddressesListFromAddressAsync(address).GetAwaiter().GetResult();

    /// <summary>
    /// Gets an array of string addresses that matched a possibly ambiguous address.
    /// </summary>
    /// <param name="address">The address.</param>
    public Task<string[]?> GetAddressesListFromAddressAsync(AddressData address, CancellationToken cancellationToken = default)
    {
        if (address == null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        return GetAddressesListFromAddressAsync(address.ToString(), cancellationToken);
    }


    /// <summary>
    /// Gets the directions.
    /// </summary>
    /// <param name="latitude">The latitude.</param>
    /// <param name="longitude">The longitude.</param>
    public Directions GetDirections(double latitude, double longitude)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the directions.
    /// </summary>
    /// <param name="originAddress">From address.</param>
    /// <param name="destinationAddress">To address.</param>
    public Directions GetDirections(AddressData originAddress, AddressData destinationAddress)
    {
        return GetDirectionsAsync(originAddress, destinationAddress).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the directions.
    /// </summary>
    /// <param name="originAddress">From address.</param>
    /// <param name="destinationAddress">To address.</param>
    public async Task<Directions> GetDirectionsAsync(AddressData originAddress, AddressData destinationAddress, CancellationToken cancellationToken = default)
    {
        var xdoc = await LoadXDocumentFromUrlAsync(BuildRequestUrl(APIUrlDirections,
            Uri.EscapeDataString(originAddress.ToString()),
            Uri.EscapeDataString(destinationAddress.ToString())), cancellationToken).ConfigureAwait(false);

        var status = xdoc.Descendants("DirectionsResponse").Descendants("status").FirstOrDefault();

        var direction = new Directions();

        if (status != null && status.Value == "OK")
        {
            direction.StatusCode = Directions.Status.OK;
            direction.Distance = xdoc
                .Descendants("DirectionsResponse")
                .Descendants("route")
                .Descendants("leg")
                .Elements("distance")
                .Elements("text")
                .FirstOrDefault()?.Value;

            direction.Duration = xdoc
                .Descendants("DirectionsResponse")
                .Descendants("route")
                .Descendants("leg")
                .Elements("duration")
                .Elements("text")
                .FirstOrDefault()?.Value;

            var steps = xdoc
                .Descendants("DirectionsResponse")
                .Descendants("route")
                .Descendants("leg")
                .Descendants("step");

            foreach (var step in steps)
            {
                direction.Steps.Add(new Step
                {
                    Instruction = step.Element("html_instructions")?.Value ?? string.Empty,
                    Distance = step.Descendants("distance").FirstOrDefault()?.Element("text")?.Value,
                });
            }

            return direction;
        }

        if (status != null && status.Value != "OK")
        {
            direction.StatusCode = Directions.Status.FAILED;
            return direction;
        }

        throw new Exception("Unable to get Directions from Google");
    }

    private double ParseUS(string value)
    {
        return double.Parse(value, CultureInfo.GetCultureInfo("en-US"));
    }
    #endregion
}