FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Library.Shared/Library.Shared.csproj src/Library.Shared/
COPY src/Library.Worker/Library.Worker.csproj src/Library.Worker/
RUN dotnet restore src/Library.Worker/Library.Worker.csproj
COPY src/ src/
RUN dotnet publish src/Library.Worker/Library.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Library.Worker.dll"]
