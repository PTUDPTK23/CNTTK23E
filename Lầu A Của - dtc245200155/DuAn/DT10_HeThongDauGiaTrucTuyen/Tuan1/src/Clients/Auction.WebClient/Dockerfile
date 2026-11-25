FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["src/Clients/Auction.WebClient/Auction.WebClient.csproj", "src/Clients/Auction.WebClient/"]
RUN dotnet restore "src/Clients/Auction.WebClient/Auction.WebClient.csproj"

COPY src/Clients/Auction.WebClient/ src/Clients/Auction.WebClient/

WORKDIR "/src/src/Clients/Auction.WebClient"
RUN dotnet publish "Auction.WebClient.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "Auction.WebClient.dll"]
