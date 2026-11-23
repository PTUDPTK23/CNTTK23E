FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Library.Client/Library.Client.csproj src/Library.Client/
RUN dotnet restore src/Library.Client/Library.Client.csproj
COPY src/Library.Client/ src/Library.Client/
RUN dotnet publish src/Library.Client/Library.Client.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Library.Client.dll"]
