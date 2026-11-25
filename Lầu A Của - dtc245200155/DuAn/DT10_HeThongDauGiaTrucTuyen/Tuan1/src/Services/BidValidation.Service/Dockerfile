FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["src/Shared/AuctionSystem.Contracts/AuctionSystem.Contracts.csproj", "src/Shared/AuctionSystem.Contracts/"]
COPY ["src/Shared/AuctionSystem.Shared/AuctionSystem.Shared.csproj", "src/Shared/AuctionSystem.Shared/"]
COPY ["src/Services/BidValidation.Service/BidValidation.Service.csproj", "src/Services/BidValidation.Service/"]

RUN dotnet restore "src/Services/BidValidation.Service/BidValidation.Service.csproj"

COPY src/Shared/ src/Shared/
COPY src/Services/BidValidation.Service/ src/Services/BidValidation.Service/

WORKDIR "/src/src/Services/BidValidation.Service"
RUN dotnet publish "BidValidation.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5002
EXPOSE 5002
ENTRYPOINT ["dotnet", "BidValidation.Service.dll"]
