using FeatureHubSDK;

namespace server.FHHelper;

public class FeatureStateProvider
{
    private string FeatureHubUrl { get; }
    private string FeatureHubApiKey { get; }
    private readonly EdgeFeatureHubConfig _config;
    private readonly bool _enableAllFeatures;

    public FeatureStateProvider()
    {
        var bypassFeatureHub = Environment.GetEnvironmentVariable("FEATURE_HUB_BYPASS");
        if (string.Equals(bypassFeatureHub, "true", StringComparison.OrdinalIgnoreCase))
        {
            _enableAllFeatures = true;
            _config = null!;
            FeatureHubUrl = string.Empty;
            FeatureHubApiKey = string.Empty;
            return;
        }

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (string.Equals(environment, "Test", StringComparison.OrdinalIgnoreCase))
        {
            _enableAllFeatures = true;
            _config = null!;
            FeatureHubUrl = string.Empty;
            FeatureHubApiKey = string.Empty;
            return;
        }

        FeatureHubUrl = Environment.GetEnvironmentVariable("FEATURE_HUB_URL")
            ?? throw new InvalidOperationException("FEATURE_HUB_URL is not set.");

        FeatureHubApiKey = Environment.GetEnvironmentVariable("FEATURE_HUB_API_KEY")
            ?? throw new InvalidOperationException("FEATURE_HUB_API_KEY is not set.");
        
        var config = new EdgeFeatureHubConfig(FeatureHubUrl, FeatureHubApiKey);
        config.Init().Wait();
        _config = config;
    }

    public bool IsEnabled(string featureKey)
    {
        if (_enableAllFeatures)
        {
            return true;
        }

        return (bool) _config.Repository[featureKey].Value;
    }
}