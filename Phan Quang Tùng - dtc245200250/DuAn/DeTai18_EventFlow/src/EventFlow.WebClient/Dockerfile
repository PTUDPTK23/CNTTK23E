# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/EventFlow.Shared/EventFlow.Shared.csproj", "src/EventFlow.Shared/"]
COPY ["src/EventFlow.WebClient/EventFlow.WebClient.csproj", "src/EventFlow.WebClient/"]
RUN dotnet restore "src/EventFlow.WebClient/EventFlow.WebClient.csproj"

COPY ["src/EventFlow.Shared/", "src/EventFlow.Shared/"]
COPY ["src/EventFlow.WebClient/", "src/EventFlow.WebClient/"]
WORKDIR "/src/src/EventFlow.WebClient"
RUN dotnet publish "EventFlow.WebClient.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 5003
ENV ASPNETCORE_URLS=http://+:5003
ENV DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventFlow.WebClient.dll"]
