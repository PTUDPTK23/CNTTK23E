FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["src/Shared/AuctionSystem.Contracts/AuctionSystem.Contracts.csproj", "src/Shared/AuctionSystem.Contracts/"]
COPY ["src/Shared/AuctionSystem.Shared/AuctionSystem.Shared.csproj", "src/Shared/AuctionSystem.Shared/"]
COPY ["src/Services/Auction.API/Auction.API.csproj", "src/Services/Auction.API/"]

RUN dotnet restore "src/Services/Auction.API/Auction.API.csproj"

COPY src/Shared/ src/Shared/
COPY src/Services/Auction.API/ src/Services/Auction.API/

WORKDIR "/src/src/Services/Auction.API"
RUN dotnet publish "Auction.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5001
EXPOSE 5001
ENTRYPOINT ["dotnet", "Auction.API.dll"]
