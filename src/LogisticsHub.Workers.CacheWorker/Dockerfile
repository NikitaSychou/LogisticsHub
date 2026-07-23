FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/CompanyService/LogisticsHub.CompanyService/LogisticsHub.CompanyService.csproj", "src/CompanyService/LogisticsHub.CompanyService/"]
COPY ["src/CompanyService/LogisticsHub.CompanyService.Application/LogisticsHub.CompanyService.Application.csproj", "src/CompanyService/LogisticsHub.CompanyService.Application/"]
COPY ["src/CompanyService/LogisticsHub.CompanyService.Domain/LogisticsHub.CompanyService.Domain.csproj", "src/CompanyService/LogisticsHub.CompanyService.Domain/"]
COPY ["src/CompanyService/LogisticsHub.CompanyService.Infrastructure/LogisticsHub.CompanyService.Infrastructure.csproj", "src/CompanyService/LogisticsHub.CompanyService.Infrastructure/"]
COPY ["src/InventoryService/LogisticsHub.InventoryService.Application/LogisticsHub.InventoryService.Application.csproj", "src/InventoryService/LogisticsHub.InventoryService.Application/"]
COPY ["src/InventoryService/LogisticsHub.InventoryService.Domain/LogisticsHub.InventoryService.Domain.csproj", "src/InventoryService/LogisticsHub.InventoryService.Domain/"]
COPY ["src/InventoryService/LogisticsHub.InventoryService.Infrastructure/LogisticsHub.InventoryService.Infrastructure.csproj", "src/InventoryService/LogisticsHub.InventoryService.Infrastructure/"]
COPY ["src/InventoryService/LogisticsHub.InventoryService/LogisticsHub.InventoryService.csproj", "src/InventoryService/LogisticsHub.InventoryService/"]
COPY ["src/ShipmentService/LogisticsHub.ShipmentService.Application/LogisticsHub.ShipmentService.Application.csproj", "src/ShipmentService/LogisticsHub.ShipmentService.Application/"]
COPY ["src/ShipmentService/LogisticsHub.ShipmentService.Domain/LogisticsHub.ShipmentService.Domain.csproj", "src/ShipmentService/LogisticsHub.ShipmentService.Domain/"]
COPY ["src/ShipmentService/LogisticsHub.ShipmentService.Infrastructure/LogisticsHub.ShipmentService.Infrastructure.csproj", "src/ShipmentService/LogisticsHub.ShipmentService.Infrastructure/"]
COPY ["src/ShipmentService/LogisticsHub.ShipmentService/LogisticsHub.ShipmentService.csproj", "src/ShipmentService/LogisticsHub.ShipmentService/"]
COPY ["src/Gateway/LogisticsHub.Gateway/LogisticsHub.Gateway.csproj", "src/Gateway/LogisticsHub.Gateway/"]
COPY ["src/LogisticsHub.Workers.CacheWorker/LogisticsHub.Workers.CacheWorker.csproj", "src/LogisticsHub.Workers.CacheWorker/"]
COPY ["src/Shared/LogisticsHub.AspNetCore/LogisticsHub.AspNetCore.csproj", "src/Shared/LogisticsHub.AspNetCore/"]
COPY ["src/Shared/LogisticsHub.Caching/LogisticsHub.Caching.csproj", "src/Shared/LogisticsHub.Caching/"]
COPY ["src/Shared/LogisticsHub.Http.Resilience/LogisticsHub.Http.Resilience.csproj", "src/Shared/LogisticsHub.Http.Resilience/"]
COPY ["src/Shared/LogisticsHub.IntegrationEvents/LogisticsHub.IntegrationEvents.csproj", "src/Shared/LogisticsHub.IntegrationEvents/"]
COPY ["src/Shared/LogisticsHub.Messaging.RabbitMQ/LogisticsHub.Messaging.RabbitMQ.csproj", "src/Shared/LogisticsHub.Messaging.RabbitMQ/"]
COPY ["src/Shared/LogisticsHub.Results/LogisticsHub.Results.csproj", "src/Shared/LogisticsHub.Results/"]
RUN dotnet restore "src/LogisticsHub.Workers.CacheWorker/LogisticsHub.Workers.CacheWorker.csproj"

COPY . .
RUN dotnet publish "src/LogisticsHub.Workers.CacheWorker/LogisticsHub.Workers.CacheWorker.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
USER $APP_UID

ENTRYPOINT ["dotnet", "LogisticsHub.Workers.CacheWorker.dll"]
