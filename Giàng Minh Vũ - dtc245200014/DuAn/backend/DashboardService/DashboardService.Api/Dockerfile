FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/Shared/EventContracts/EventContracts.csproj backend/Shared/EventContracts/
COPY backend/DashboardService/DashboardService.Api/DashboardService.Api.csproj backend/DashboardService/DashboardService.Api/
RUN dotnet restore backend/DashboardService/DashboardService.Api/DashboardService.Api.csproj
COPY backend/Shared/EventContracts/ backend/Shared/EventContracts/
COPY backend/DashboardService/DashboardService.Api/ backend/DashboardService/DashboardService.Api/
RUN dotnet publish backend/DashboardService/DashboardService.Api/DashboardService.Api.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 ASPNETCORE_ENVIRONMENT=Production
# The ASP.NET runtime image does not include wget; install curl for Docker's readiness probe.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet","DashboardService.Api.dll"]
