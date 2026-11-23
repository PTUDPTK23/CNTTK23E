# Build context là THƯ MỤC GỐC của repo (để copy được Library.Shared)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Library.Shared/Library.Shared.csproj src/Library.Shared/
COPY src/Library.Api/Library.Api.csproj src/Library.Api/
RUN dotnet restore src/Library.Api/Library.Api.csproj
COPY src/ src/
RUN dotnet publish src/Library.Api/Library.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Library.Api.dll"]
