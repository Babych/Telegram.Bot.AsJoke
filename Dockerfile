# Use .NET 7.0 SDK as build environment
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /App

# Copy csproj and restore as distinct layers
COPY /Telegram.Bot.AsJoke.Polling/*.csproj ./
RUN dotnet restore

# Copy everything else and build the application
COPY /Telegram.Bot.AsJoke.Polling/. ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/sdk:6.0 as runtime
WORKDIR /App
COPY --from=build-env /App/out .

ENTRYPOINT ["dotnet", "Telegram.BotAsJoke.Polling.dll"]
