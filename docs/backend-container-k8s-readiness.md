# Backend Container And Kubernetes Readiness

This checklist captures the current backend container assumptions before adding AKS manifests.

## Container Images

- Gateway, CompanyService, InventoryService, ShipmentService, and CacheWorker each have a Dockerfile.
- Web service images publish from the repository root and bind to `http://+:8080` inside the container.
- CacheWorker uses a .NET runtime image, has no HTTP listener, and should run without public ingress.

## Future AKS Topology

- Gateway is the only backend service expected to receive public ingress.
- CompanyService, InventoryService, and ShipmentService should be internal Kubernetes services.
- CacheWorker should run as a worker workload, scheduled job, or deployment without ingress.
- Angular is hosted separately as static files; it should not be served from the Gateway container.

## Runtime Configuration

Local appsettings and Docker Compose contain development defaults such as `localhost`, `host.docker.internal`, local SQL Server, local Redis, and local RabbitMQ service names. Keep those for local development.

For Kubernetes, provide production values through environment variables, Kubernetes secrets, or mounted configuration:

- `ConnectionStrings__CompanyDb`
- `ConnectionStrings__InventoryDb`
- `ConnectionStrings__ShipmentDb`
- `ConnectionStrings__Redis`
- `RabbitMq__HostName`, `RabbitMq__Port`, `RabbitMq__UserName`, `RabbitMq__Password`, `RabbitMq__ExchangeName`
- Gateway `ReverseProxy__Clusters__...__Destinations__...__Address`
- `CompanyService__BaseUrl` for ShipmentService reference validation
- `AzureAd__Instance`, `AzureAd__TenantId`, `AzureAd__ClientId`, `AzureAd__Audience`, `AzureAd__RequiredScope`

Do not put production passwords, connection strings, or client secrets in appsettings files.

## Service URLs

Gateway reverse proxy destinations are configurable and should later point to internal Kubernetes service DNS names for CompanyService, InventoryService, and ShipmentService.

ShipmentService `CompanyService__BaseUrl` is also configurable and should later point to the internal CompanyService Kubernetes service URL.

Do not hardcode future AKS service names into checked-in production code before the AKS deployment skeleton exists.

## Health Endpoints

Gateway and the HTTP services expose:

- `/health`
- `/health/live`
- `/health/ready`

Health endpoints are mapped separately from protected API routes and should remain available for Kubernetes probes.

## Dependency Notes

- Azure SQL Database should provide the service database connection strings.
- Azure Cache for Redis should provide the CompanyService and CacheWorker Redis connection string.
- RabbitMQ is currently kept as RabbitMQ and can run in AKS later, with credentials supplied from secrets.
- Database schema changes must remain manual SQL; EF migrations are not used.

## Follow-Up AKS PR Scope

The later AKS deployment PR should add manifests, Helm, or other deployment assets for:

- public Gateway ingress;
- internal service DNS and service discovery;
- secrets/config references;
- health probes;
- CacheWorker workload shape;
- RabbitMQ, Azure SQL, and Azure Cache for Redis wiring.
