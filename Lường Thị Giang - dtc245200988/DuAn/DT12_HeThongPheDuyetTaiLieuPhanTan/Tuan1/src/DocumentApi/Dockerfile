FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/DocumentApi/DocumentApi.csproj", "src/DocumentApi/"]
RUN dotnet restore "src/DocumentApi/DocumentApi.csproj"

COPY . .
WORKDIR "/src/src/DocumentApi"
RUN dotnet publish "DocumentApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DocumentApi.dll"]
