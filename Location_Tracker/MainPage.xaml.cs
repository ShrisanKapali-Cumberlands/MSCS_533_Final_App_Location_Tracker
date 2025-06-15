using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Collections.ObjectModel;
using Location_Tracker.Services;
using Location_Tracker.Models;

namespace Location_Tracker;
public partial class MainPage : ContentPage
{
    private readonly LocationService _locationService;
    private readonly DatabaseService _databaseService;
    private Timer _trackingTimer;
    private bool _isTracking = false;

    public MainPage()
    {
        InitializeComponent();
        _locationService = new LocationService();
        _databaseService = new DatabaseService();

        LoadExistingLocations();
    }

    private async void LoadExistingLocations()
    {
        try
        {
            var locations = await _databaseService.GetAllLocationsAsync();
            await UpdateHeatMap(locations);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load existing locations: {ex.Message}", "OK");
        }
    }

    private async void OnStartTrackingClicked(object sender, EventArgs e)
    {
        try
        {
            var hasPermission = await _locationService.RequestLocationPermissionAsync();
            if (!hasPermission)
            {
                await DisplayAlert("Permission Required", "Location permission is required for tracking.", "OK");
                return;
            }

            _isTracking = true;
            StartTrackingButton.IsEnabled = false;
            StopTrackingButton.IsEnabled = true;

            // Start tracking timer (every 30 seconds)
            _trackingTimer = new Timer(async _ => await TrackLocationAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

            await DisplayAlert("Tracking Started", "Location tracking has started.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to start tracking: {ex.Message}", "OK");
        }
    }

    private async void OnStopTrackingClicked(object sender, EventArgs e)
    {
        _isTracking = false;
        _trackingTimer?.Dispose();

        StartTrackingButton.IsEnabled = true;
        StopTrackingButton.IsEnabled = false;

        await DisplayAlert("Tracking Stopped", "Location tracking has stopped.", "OK");
    }

    private async void OnClearDataClicked(object sender, EventArgs e)
    {
        var result = await DisplayAlert("Clear Data", "Are you sure you want to clear all location data?", "Yes", "No");
        if (result)
        {
            await _databaseService.ClearAllLocationsAsync();
            HeatMap.MapElements.Clear();
            await DisplayAlert("Data Cleared", "All location data has been cleared.", "OK");
        }
    }

    private async Task TrackLocationAsync()
    {
        if (!_isTracking) return;

        try
        {
            var location = await _locationService.GetCurrentLocationAsync();
            if (location != null)
            {
                var locationPoint = new LocationPoint
                {
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    Timestamp = DateTime.UtcNow
                };

                await _databaseService.SaveLocationAsync(locationPoint);

                // Update heat map on main thread
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var allLocations = await _databaseService.GetAllLocationsAsync();
                    await UpdateHeatMap(allLocations);
                });
            }
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("Tracking Error", $"Error tracking location: {ex.Message}", "OK");
            });
        }
    }

    private async Task UpdateHeatMap(List<LocationPoint> locations)
    {
        if (locations == null || !locations.Any()) return;

        HeatMap.MapElements.Clear();

        // Group locations by proximity to create heat intensity
        var heatPoints = CreateHeatPoints(locations);

        foreach (var heatPoint in heatPoints)
        {
            var circle = new Circle
            {
                Center = new Location(heatPoint.Latitude, heatPoint.Longitude),
                Radius = Distance.FromMeters(heatPoint.Radius),
                StrokeColor = GetHeatColor(heatPoint.Intensity),
                FillColor = GetHeatColor(heatPoint.Intensity, 0.3),
                StrokeWidth = 2
            };

            HeatMap.MapElements.Add(circle);
        }

        // Center map on latest location
        if (locations.Any())
        {
            var latest = locations.OrderByDescending(l => l.Timestamp).First();
            var mapSpan = MapSpan.FromCenterAndRadius(
                new Location(latest.Latitude, latest.Longitude),
                Distance.FromKilometers(1));

            HeatMap.MoveToRegion(mapSpan);
        }
    }

    private List<HeatPoint> CreateHeatPoints(List<LocationPoint> locations)
    {
        var heatPoints = new List<HeatPoint>();
        const double proximityThreshold = 0.001; // ~100 meters

        var processedLocations = new HashSet<LocationPoint>();

        foreach (var location in locations)
        {
            if (processedLocations.Contains(location)) continue;

            var nearbyLocations = locations.Where(l =>
                !processedLocations.Contains(l) &&
                GetDistance(location.Latitude, location.Longitude, l.Latitude, l.Longitude) <= proximityThreshold)
                .ToList();

            if (nearbyLocations.Any())
            {
                var intensity = nearbyLocations.Count;
                var centerLat = nearbyLocations.Average(l => l.Latitude);
                var centerLng = nearbyLocations.Average(l => l.Longitude);

                heatPoints.Add(new HeatPoint
                {
                    Latitude = centerLat,
                    Longitude = centerLng,
                    Intensity = intensity,
                    Radius = Math.Max(50, intensity * 20) // Dynamic radius based on intensity
                });

                foreach (var nearby in nearbyLocations)
                {
                    processedLocations.Add(nearby);
                }
            }
        }

        return heatPoints;
    }

    private double GetDistance(double lat1, double lng1, double lat2, double lng2)
    {
        return Math.Sqrt(Math.Pow(lat2 - lat1, 2) + Math.Pow(lng2 - lng1, 2));
    }

    private Color GetHeatColor(int intensity, double opacity = 1.0)
    {
        // Create color gradient from blue (low) to red (high)
        if (intensity <= 1)
            return Color.FromRgba(0, 0, 255, opacity); // Blue
        else if (intensity <= 3)
            return Color.FromRgba(0, 255, 255, opacity); // Cyan
        else if (intensity <= 5)
            return Color.FromRgba(0, 255, 0, opacity); // Green
        else if (intensity <= 8)
            return Color.FromRgba(255, 255, 0, opacity); // Yellow
        else if (intensity <= 12)
            return Color.FromRgba(255, 165, 0, opacity); // Orange
        else
            return Color.FromRgba(255, 0, 0, opacity); // Red
    }
}