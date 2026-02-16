FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["TelegramAds/TelegramAds.csproj", "TelegramAds/"]
RUN dotnet restore "TelegramAds/TelegramAds.csproj"
COPY . .
WORKDIR "/src/TelegramAds"
RUN dotnet build "TelegramAds.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TelegramAds.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY TelegramAds/tonlibjson-linux-x86_64.so /app/
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TelegramAds.dll"]
