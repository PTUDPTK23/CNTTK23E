FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props global.json ./
COPY src/Clinic.Core/ src/Clinic.Core/
COPY src/Appointment.Api/ src/Appointment.Api/
RUN dotnet publish src/Appointment.Api/Appointment.Api.csproj -c Release -o /app --no-self-contained
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Appointment.Api.dll"]
