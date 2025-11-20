var builder = DistributedApplication.CreateBuilder(args);

var marten = builder
    .AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin(config => config.WithHostPort(8888).WithLifetime(ContainerLifetime.Persistent))
    .WithLifetime(ContainerLifetime.Persistent);

var martendb = marten.AddDatabase("marten");

var apiService = builder.AddProject<Projects.TimeTrackingDemo_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithUrl("scalar")
    .WithReference(martendb);

builder.Build().Run();