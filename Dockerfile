# Use .NET 7.0 SDK as build environment
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /App

# Accept build arguments for API keys
ARG API_KEY

# Copy csproj and restore as distinct layers
COPY . ./
RUN dotnet restore

# Copy everything else and build the application
COPY . ./
RUN dotnet publish -c Release -o out

# Replace API Key in Config File
RUN sed -i "s|\"API_KEY\"|\"$API_KEY\"|g" Telegram.Bot.AsJoke.Polling/appsettings.json

# List the content of appsettings.json
RUN cat Telegram.Bot.AsJoke.Polling/appsettings.json

# Build runtime image
FROM mcr.microsoft.com/dotnet/sdk:7.0
WORKDIR /App
COPY --from=build-env /App/out .

ENTRYPOINT ["dotnet", "Telegram.BotAsJoke.Polling.dll"]
