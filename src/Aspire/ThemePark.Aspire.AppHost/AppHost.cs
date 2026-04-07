var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ThemePark_ControlCenter_Api>("controlcenter-api");
builder.AddProject<Projects.ThemePark_Rides_Api>("rides-api");
builder.AddProject<Projects.ThemePark_Queue_Api>("queue-api");
builder.AddProject<Projects.ThemePark_Maintenance_Api>("maintenance-api");
builder.AddProject<Projects.ThemePark_Weather_Api>("weather-api");
builder.AddProject<Projects.ThemePark_Mascots_Api>("mascots-api");
builder.AddProject<Projects.ThemePark_Refunds_Api>("refunds-api");

builder.Build().Run();
