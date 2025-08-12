using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace HidaSushi.Admin.Services;

public interface IWeatherService
{
    Task<WeatherData?> GetCurrentWeatherAsync();
    Task<List<ForecastData>?> GetWeatherForecastAsync();
}

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WeatherService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _apiKey;
    private readonly string _city;
    private readonly int _refreshIntervalMinutes;

    // Rate limiting: Track API calls
    private static int _dailyApiCalls = 0;
    private static DateTime _lastResetDate = DateTime.Today;
    private const int MAX_DAILY_CALLS = 950; // Leave some buffer under 1000

    public WeatherService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<WeatherService> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
        _apiKey = configuration["Weather:ApiKey"] ?? "b0d48d16272696c5710c5292e5bbbac9";
        _city = configuration["Weather:City"] ?? "Brussels";
        _refreshIntervalMinutes = configuration.GetValue<int>("Weather:RefreshIntervalMinutes", 30);
    }

    public async Task<WeatherData?> GetCurrentWeatherAsync()
    {
        // Check cache first
        var cacheKey = $"weather_current_{_city}";
        if (_cache.TryGetValue(cacheKey, out WeatherData? cachedWeather))
        {
            _logger.LogInformation("Returning cached weather data for {City}", _city);
            return cachedWeather;
        }

        // Check rate limiting
        if (!CanMakeApiCall())
        {
            _logger.LogWarning("Daily API call limit reached. Using cached data if available.");
            return null;
        }

        try
        {
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={_city}&appid={_apiKey}&units=metric";
            _logger.LogInformation("Fetching current weather for {City} (API calls today: {CallCount})", _city, _dailyApiCalls);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            // Increment API call counter
            IncrementApiCallCount();

            var jsonString = await response.Content.ReadAsStringAsync();
            var weatherResponse = JsonSerializer.Deserialize<OpenWeatherMapResponse>(jsonString, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (weatherResponse != null)
            {
                var weatherData = new WeatherData
                {
                    Temperature = Math.Round(weatherResponse.Main.Temp, 1),
                    FeelsLike = Math.Round(weatherResponse.Main.FeelsLike, 1),
                    Humidity = weatherResponse.Main.Humidity,
                    Pressure = weatherResponse.Main.Pressure,
                    Description = weatherResponse.Weather[0].Description,
                    Icon = weatherResponse.Weather[0].Icon,
                    WindSpeed = weatherResponse.Wind?.Speed ?? 0,
                    WindDirection = weatherResponse.Wind?.Deg ?? 0,
                    Visibility = weatherResponse.Visibility / 1000.0, // Convert to km
                    City = weatherResponse.Name,
                    Country = weatherResponse.Sys.Country,
                    Sunrise = DateTimeOffset.FromUnixTimeSeconds(weatherResponse.Sys.Sunrise).DateTime,
                    Sunset = DateTimeOffset.FromUnixTimeSeconds(weatherResponse.Sys.Sunset).DateTime,
                    LastUpdated = DateTime.UtcNow
                };

                // Cache for the configured interval
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_refreshIntervalMinutes),
                    Priority = CacheItemPriority.High
                };
                _cache.Set(cacheKey, weatherData, cacheOptions);

                return weatherData;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching current weather data");
            return null;
        }
    }

    public async Task<List<ForecastData>?> GetWeatherForecastAsync()
    {
        // Check cache first
        var cacheKey = $"weather_forecast_{_city}";
        if (_cache.TryGetValue(cacheKey, out List<ForecastData>? cachedForecast))
        {
            _logger.LogInformation("Returning cached forecast data for {City}", _city);
            return cachedForecast;
        }

        // Check rate limiting
        if (!CanMakeApiCall())
        {
            _logger.LogWarning("Daily API call limit reached. Using cached data if available.");
            return null;
        }

        try
        {
            var url = $"https://api.openweathermap.org/data/2.5/forecast?q={_city}&appid={_apiKey}&units=metric";
            _logger.LogInformation("Fetching weather forecast for {City} (API calls today: {CallCount})", _city, _dailyApiCalls);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            // Increment API call counter
            IncrementApiCallCount();

            var jsonString = await response.Content.ReadAsStringAsync();
            var forecastResponse = JsonSerializer.Deserialize<OpenWeatherMapForecastResponse>(jsonString, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (forecastResponse?.List != null)
            {
                var forecastData = forecastResponse.List.Select(item => new ForecastData
                {
                    DateTime = DateTimeOffset.FromUnixTimeSeconds(item.Dt).DateTime,
                    Temperature = Math.Round(item.Main.Temp, 1),
                    Description = item.Weather[0].Description,
                    Icon = item.Weather[0].Icon,
                    Humidity = item.Main.Humidity,
                    WindSpeed = item.Wind?.Speed ?? 0
                }).ToList();

                // Cache for the configured interval
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_refreshIntervalMinutes),
                    Priority = CacheItemPriority.High
                };
                _cache.Set(cacheKey, forecastData, cacheOptions);

                return forecastData;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather forecast data");
            return null;
        }
    }

    private bool CanMakeApiCall()
    {
        // Reset counter if it's a new day
        if (DateTime.Today > _lastResetDate)
        {
            _dailyApiCalls = 0;
            _lastResetDate = DateTime.Today;
            _logger.LogInformation("Reset daily API call counter for new day");
        }

        return _dailyApiCalls < MAX_DAILY_CALLS;
    }

    private void IncrementApiCallCount()
    {
        _dailyApiCalls++;
        _logger.LogDebug("API call count incremented to {CallCount}/{MaxCalls}", _dailyApiCalls, MAX_DAILY_CALLS);
    }
}

// Data models for weather
public class WeatherData
{
    public double Temperature { get; set; }
    public double FeelsLike { get; set; }
    public int Humidity { get; set; }
    public int Pressure { get; set; }
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public double WindSpeed { get; set; }
    public double WindDirection { get; set; }
    public double Visibility { get; set; }
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
    public DateTime Sunrise { get; set; }
    public DateTime Sunset { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class ForecastData
{
    public DateTime DateTime { get; set; }
    public double Temperature { get; set; }
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
}

// OpenWeatherMap API response models
public class OpenWeatherMapResponse
{
    public MainData Main { get; set; } = new();
    public List<WeatherInfo> Weather { get; set; } = new();
    public WindData? Wind { get; set; }
    public SysData Sys { get; set; } = new();
    public string Name { get; set; } = "";
    public int Visibility { get; set; }
}

public class OpenWeatherMapForecastResponse
{
    public List<ForecastItem> List { get; set; } = new();
}

public class ForecastItem
{
    public long Dt { get; set; }
    public MainData Main { get; set; } = new();
    public List<WeatherInfo> Weather { get; set; } = new();
    public WindData? Wind { get; set; }
}

public class MainData
{
    public double Temp { get; set; }
    public double FeelsLike { get; set; }
    public int Humidity { get; set; }
    public int Pressure { get; set; }
}

public class WeatherInfo
{
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
}

public class WindData
{
    public double Speed { get; set; }
    public double Deg { get; set; }
}

public class SysData
{
    public string Country { get; set; } = "";
    public long Sunrise { get; set; }
    public long Sunset { get; set; }
} 