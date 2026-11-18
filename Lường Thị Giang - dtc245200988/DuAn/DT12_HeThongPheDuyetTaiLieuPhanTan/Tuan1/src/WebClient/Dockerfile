FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/WebClient/WebClient.csproj", "src/WebClient/"]
RUN dotnet restore "src/WebClient/WebClient.csproj"

COPY . .
WORKDIR "/src/src/WebClient"
RUN dotnet publish "WebClient.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WebClient.dll"]
