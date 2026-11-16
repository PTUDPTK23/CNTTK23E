# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["src/EventFlow.Shared/EventFlow.Shared.csproj", "src/EventFlow.Shared/"]
COPY ["src/EventFlow.EventApi/EventFlow.EventApi.csproj", "src/EventFlow.EventApi/"]
RUN dotnet restore "src/EventFlow.EventApi/EventFlow.EventApi.csproj"

# Copy all source files and publish
COPY ["src/EventFlow.Shared/", "src/EventFlow.Shared/"]
COPY ["src/EventFlow.EventApi/", "src/EventFlow.EventApi/"]
WORKDIR "/src/src/EventFlow.EventApi"
RUN dotnet publish "EventFlow.EventApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventFlow.EventApi.dll"]
