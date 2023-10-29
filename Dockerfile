FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /App

# Replace API Key in Config File
RUN sed -i 's|API_KEY|'"${ API_KEY }"'|g' Telegram.Bot.AsJoke.Polling/appsettings.json

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:7.0
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "Telegram.BotAsJoke.Polling.dll"]
