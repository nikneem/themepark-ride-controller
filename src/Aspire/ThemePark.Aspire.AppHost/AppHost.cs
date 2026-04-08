using Aspire.Hosting.Yarp.Transforms;
using ThemePark.Aspire.ServiceDefaults;

var builder = DistributedApplication.CreateBuilder(args);

// RabbitMQ — fixed port so Dapr sidecar metadata can reference it directly.
const int rabbitMqPort = 5672;
var rabbitMqUsername = builder.AddParameter("rabbitmq-username", secret: false);
var rabbitMqPassword = builder.AddParameter("rabbitmq-password", secret: true);
var rabbitMqUsernameValue = builder.Configuration["Parameters:rabbitmq-username"] ?? "guest";
var rabbitMqPasswordValue = builder.Configuration["Parameters:rabbitmq-password"] ?? "guest";

var rabbitmq = builder.AddRabbitMQ("messaging", rabbitMqUsername, rabbitMqPassword, rabbitMqPort)
    .WithManagementPlugin();

// Redis — backing store for the Dapr state store.
var redis = builder.AddRedis("redis")
    .WithLifetime(ContainerLifetime.Persistent);

var redisHost = redis.Resource.PrimaryEndpoint.Property(EndpointProperty.Host);
var redisPort = redis.Resource.PrimaryEndpoint.Property(EndpointProperty.Port);

// Dapr pub/sub component backed by RabbitMQ.
var pubSub = builder.AddDaprComponent(AspireConstants.DaprComponents.PubSub, "pubsub.rabbitmq")
    .WithMetadata("hostname", "localhost")
    .WithMetadata("port", rabbitMqPort.ToString())
    .WithMetadata("protocol", "amqp")
    .WithMetadata("username", rabbitMqUsernameValue)
    .WithMetadata("password", rabbitMqPasswordValue)
    .WaitFor(rabbitmq);

// Dapr state store component backed by Redis.
var stateStore = builder.AddDaprComponent(AspireConstants.DaprComponents.StateStore, "state.redis")
    .WithMetadata("redisHost", ReferenceExpression.Create($"{redisHost}:{redisPort}"))
    .WithMetadata("actorStateStore", "true")
    .WaitFor(redis);

// Helper to attach a Dapr sidecar referencing the shared pub/sub component.

// Explicit HTTP endpoints ensure YARP (running in Docker) routes to HTTP, not HTTPS.
// Ports match the "http" profile in each service's launchSettings.json.
var controlCenterApi = builder.AddProject<Projects.ThemePark_ControlCenter_Api>(AspireConstants.Projects.ControlCenterApi)
    .WithHttpEndpoint(port: 5288, name: "http")
    .WithDaprSidecar(opts => opts.WithReference(pubSub).WithReference(stateStore));
var ridesApi = builder.AddProject<Projects.ThemePark_Rides_Api>(AspireConstants.Projects.RidesApi)
    .WithHttpEndpoint(port: 5070, name: "http")
    .WithDaprSidecar(opts => opts.WithReference(pubSub).WithReference(stateStore));
var queueApi = builder.AddProject<Projects.ThemePark_Queue_Api>(AspireConstants.Projects.QueueApi)
    .WithHttpEndpoint(port: 5102, name: "http")
    .WithDaprSidecar(opts => opts.WithReference(pubSub).WithReference(stateStore));
var maintenanceApi = builder.AddProject<Projects.ThemePark_Maintenance_Api>(AspireConstants.Projects.MaintenanceApi)
    .WithHttpEndpoint(port: 5103, name: "http")
    .WithDaprSidecar(opts => opts.WithReference(pubSub).WithReference(stateStore));
var weatherApi = builder.AddProject<Projects.ThemePark_Weather_Api>(AspireConstants.Projects.WeatherApi)
    .WithHttpEndpoint(port: 5104, name: "http")
    .WithDaprSidecar(opts => opts.WithReference(pubSub));
var mascotsApi = builder.AddProject<Projects.ThemePark_Mascots_Api>(AspireConstants.Projects.MascotsApi)
    .WithHttpEndpoint(port: 5105, name: "http")
    .WithDaprSidecar(opts => opts.WithReference(pubSub));
var refundsApi = builder.AddProject<Projects.ThemePark_Refunds_Api>(AspireConstants.Projects.RefundsApi)
    .WithHttpEndpoint(port: 5106, name: "http")
    .WithDaprSidecar(opts => opts.WithReference(pubSub).WithReference(stateStore));

var gateway = builder.AddYarp("gateway")
    .WithHostPort(5000)
    .WithConfiguration(yarp =>
    {
        // ControlCenter routes its handlers under /api/..., so strip /controlcenter and add /api
        yarp.AddRoute("/controlcenter/{**catch-all}", controlCenterApi.GetEndpoint("http"))
            .WithTransformPathRemovePrefix("/controlcenter")
            .WithTransformPathPrefix("/api");

        yarp.AddRoute("/rides/{**catch-all}", ridesApi.GetEndpoint("http"));
        yarp.AddRoute("/queue/{**catch-all}", queueApi.GetEndpoint("http"));
        yarp.AddRoute("/maintenance/{**catch-all}", maintenanceApi.GetEndpoint("http"));
        yarp.AddRoute("/weather/{**catch-all}", weatherApi.GetEndpoint("http"));
        yarp.AddRoute("/mascots/{**catch-all}", mascotsApi.GetEndpoint("http"));
        yarp.AddRoute("/refunds/{**catch-all}", refundsApi.GetEndpoint("http"));
    });

var frontendSourceFolder = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "app"));
if (Directory.Exists(frontendSourceFolder))
{
    builder.AddJavaScriptApp("frontend", frontendSourceFolder)
        .WaitFor(gateway)
        //.WithNpm(false)
        .WithRunScript("start")
        .WithHttpEndpoint(port: 4200, isProxied: false)
        .WithEnvironment("ASPIRE_GATEWAY_URL", gateway.GetEndpoint("http"));
}

builder.Build().Run();
