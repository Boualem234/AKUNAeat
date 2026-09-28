using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;


namespace AKUNAeat.API
{
    public class OpenRouteServiceDistance
    {
        private readonly HttpClient _httpClient = new();
        private readonly string _apiKey;

        public OpenRouteServiceDistance(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<double> GetDistanceAsync(string origin, string destination)
        {
            var from = await GeocodeAsync(origin);
            var to = await GeocodeAsync(destination);
            if (from == null || to == null) return 0;

            var requestBody = new
            {
                locations = new[]
                {
                new[] { from.Value.lon, from.Value.lat },
                new[] { to.Value.lon, to.Value.lat }
            },
                metrics = new[] { "distance" }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openrouteservice.org/v2/matrix/driving-car");
            request.Headers.Add("Authorization", _apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            double distanceMeters = doc.RootElement.GetProperty("distances")[0][1].GetDouble();
            return distanceMeters / 1000;
        }

        private async Task<(double lat, double lon)?> GeocodeAsync(string address)
        {
            var url = $"https://api.openrouteservice.org/geocode/search?api_key={_apiKey}&text={Uri.EscapeDataString(address)}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var features = doc.RootElement.GetProperty("features");
            if (features.GetArrayLength() == 0) return null;

            var coords = features[0].GetProperty("geometry").GetProperty("coordinates");
            return (coords[1].GetDouble(), coords[0].GetDouble());
        }
    }

}
