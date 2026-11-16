# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/EventFlow.Shared/EventFlow.Shared.csproj", "src/EventFlow.Shared/"]
COPY ["src/EventFlow.AuditWorker/EventFlow.AuditWorker.csproj", "src/EventFlow.AuditWorker/"]
RUN dotnet restore "src/EventFlow.AuditWorker/EventFlow.AuditWorker.csproj"

COPY ["src/EventFlow.Shared/", "src/EventFlow.Shared/"]
COPY ["src/EventFlow.AuditWorker/", "src/EventFlow.AuditWorker/"]
WORKDIR "/src/src/EventFlow.AuditWorker"
RUN dotnet publish "EventFlow.AuditWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 5002
ENV ASPNETCORE_URLS=http://+:5002
ENV DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventFlow.AuditWorker.dll"]
