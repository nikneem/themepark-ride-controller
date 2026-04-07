var builder = DistributedApplication.CreateBuilder(args);

// RabbitMQ — fixed port so Dapr sidecar metadata can reference it directly.
const int rabbitMqPort = 5672;
var rabbitMqUsername = builder.AddParameter("rabbitmq-username", secret: false);
var rabbitMqPassword = builder.AddParameter("rabbitmq-password", secret: true);
var rabbitMqUsernameValue = builder.Configuration["Parameters:rabbitmq-username"] ?? "guest";
var rabbitMqPasswordValue = builder.Configuration["Parameters:rabbitmq-password"] ?? "guest";

var rabbitmq = builder.AddRabbitMQ("messaging", rabbitMqUsername, rabbitMqPassword, rabbitMqPort)
    .WithManagementPlugin();

// Dapr pub/sub component backed by RabbitMQ.
var pubSub = builder.AddDaprComponent("themepark-pubsub", "pubsub.rabbitmq")
    .WithMetadata("hostname", "localhost")
    .WithMetadata("port", rabbitMqPort.ToString())
    .WithMetadata("protocol", "amqp")
    .WithMetadata("username", rabbitMqUsernameValue)
    .WithMetadata("password", rabbitMqPasswordValue)
    .WaitFor(rabbitmq);

// Helper to attach a Dapr sidecar referencing the shared pub/sub component.

builder.AddProject<Projects.ThemePark_ControlCenter_Api>("controlcenter-api")
    .WithDaprSidecar(opts => opts.WithReference(pubSub));
builder.AddProject<Projects.ThemePark_Rides_Api>("rides-api")
    .WithDaprSidecar(opts => opts.WithReference(pubSub));
builder.AddProject<Projects.ThemePark_Queue_Api>("queue-api")
    .WithDaprSidecar(opts => opts.WithReference(pubSub));
builder.AddProject<Projects.ThemePark_Maintenance_Api>("maintenance-api")
    .WithDaprSidecar(opts => opts.WithReference(pubSub));
builder.AddProject<Projects.ThemePark_Weather_Api>("weather-api")
    .WithDaprSidecar(opts => opts.WithReference(pubSub));
builder.AddProject<Projects.ThemePark_Mascots_Api>("mascots-api")
    .WithDaprSidecar(opts => opts.WithReference(pubSub));
builder.AddProject<Projects.ThemePark_Refunds_Api>("refunds-api")
    .WithDaprSidecar(opts => opts.WithReference(pubSub));

builder.Build().Run();
