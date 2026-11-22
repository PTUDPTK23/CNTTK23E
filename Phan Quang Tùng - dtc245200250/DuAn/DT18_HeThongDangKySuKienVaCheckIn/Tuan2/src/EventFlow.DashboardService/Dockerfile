# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/EventFlow.Shared/EventFlow.Shared.csproj", "src/EventFlow.Shared/"]
COPY ["src/EventFlow.DashboardService/EventFlow.DashboardService.csproj", "src/EventFlow.DashboardService/"]
RUN dotnet restore "src/EventFlow.DashboardService/EventFlow.DashboardService.csproj"

COPY ["src/EventFlow.Shared/", "src/EventFlow.Shared/"]
COPY ["src/EventFlow.DashboardService/", "src/EventFlow.DashboardService/"]
WORKDIR "/src/src/EventFlow.DashboardService"
RUN dotnet publish "EventFlow.DashboardService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001
ENV DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventFlow.DashboardService.dll"]
