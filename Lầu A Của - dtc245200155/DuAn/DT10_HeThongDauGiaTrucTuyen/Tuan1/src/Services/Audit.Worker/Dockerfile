FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["src/Shared/AuctionSystem.Shared/AuctionSystem.Shared.csproj", "src/Shared/AuctionSystem.Shared/"]
COPY ["src/Services/Audit.Worker/Audit.Worker.csproj", "src/Services/Audit.Worker/"]

RUN dotnet restore "src/Services/Audit.Worker/Audit.Worker.csproj"

COPY src/Shared/ src/Shared/
COPY src/Services/Audit.Worker/ src/Services/Audit.Worker/

WORKDIR "/src/src/Services/Audit.Worker"
RUN dotnet publish "Audit.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5004
EXPOSE 5004
ENTRYPOINT ["dotnet", "Audit.Worker.dll"]
