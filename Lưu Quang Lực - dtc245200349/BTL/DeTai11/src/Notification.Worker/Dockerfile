FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY Directory.Build.props global.json ./
COPY src/Notification.Shared/ src/Notification.Shared/
COPY src/Notification.Worker/ src/Notification.Worker/
RUN dotnet publish src/Notification.Worker/Notification.Worker.csproj -c Release -o /app
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Notification.Worker.dll"]
