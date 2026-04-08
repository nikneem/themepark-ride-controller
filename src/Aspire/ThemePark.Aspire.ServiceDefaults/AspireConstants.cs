namespace ThemePark.Aspire.ServiceDefaults;

public class AspireConstants
{
    public sealed class Projects
    {
        public const string ControlCenterApi = "controlcenter-api";
        public const string RidesApi = "rides-api";
        public const string QueueApi = "queue-api";
        public const string MaintenanceApi = "maintenance-api";
        public const string WeatherApi = "weather-api";
        public const string MascotsApi = "mascots-api";
        public const string RefundsApi = "refunds-api";
    }

    public sealed class DaprComponents
    {
        public const string PubSub = "themepark-pubsub";
        public const string StateStore = "themepark-statestore";
    }
}
