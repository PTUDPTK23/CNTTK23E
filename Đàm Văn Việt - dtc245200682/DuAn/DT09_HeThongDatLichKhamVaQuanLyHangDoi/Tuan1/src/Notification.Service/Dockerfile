FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props global.json ./
COPY src/Clinic.Core/ src/Clinic.Core/
COPY src/Notification.Service/ src/Notification.Service/
RUN dotnet publish src/Notification.Service/Notification.Service.csproj -c Release -o /app --no-self-contained
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Notification.Service.dll"]
