FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["src/Shared/AuctionSystem.Shared/AuctionSystem.Shared.csproj", "src/Shared/AuctionSystem.Shared/"]
COPY ["src/Services/Auction.Hub/Auction.Hub.csproj", "src/Services/Auction.Hub/"]

RUN dotnet restore "src/Services/Auction.Hub/Auction.Hub.csproj"

COPY src/Shared/ src/Shared/
COPY src/Services/Auction.Hub/ src/Services/Auction.Hub/

WORKDIR "/src/src/Services/Auction.Hub"
RUN dotnet publish "Auction.Hub.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5003
EXPOSE 5003
ENTRYPOINT ["dotnet", "Auction.Hub.dll"]
