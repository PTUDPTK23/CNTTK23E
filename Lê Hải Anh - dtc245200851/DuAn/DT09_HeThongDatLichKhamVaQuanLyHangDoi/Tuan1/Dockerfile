FROM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build
WORKDIR /source
COPY ClinicQueue.slnx global.json ./
COPY src/ src/
RUN dotnet restore ClinicQueue.slnx
ARG SERVICE=Api
RUN dotnet publish src/ClinicQueue.${SERVICE}/ClinicQueue.${SERVICE}.csproj --no-restore -c Release -o /app
FROM mcr.microsoft.com/dotnet/aspnet:10.0
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ARG SERVICE=Api
ENV SERVICE=${SERVICE}
ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
ENTRYPOINT ["/bin/sh", "-c", "exec dotnet ClinicQueue.${SERVICE}.dll \"$@\"", "--"]
