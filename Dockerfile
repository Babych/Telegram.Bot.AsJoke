FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /App

# Print the API_KEY environment variable in the build logs
RUN echo "API_KEY is $API_KEY"
RUN echo "AZURE_WEBAPP_NAME is $AZURE_WEBAPP_NAME"
RUN echo "API_KEY2 is $API_KEY2"
RUN echo "API_KEY3 is $API_KEY3"
RUN echo "github.actor is $github.actor"


# Copy everything
COPY . ./

# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Run ls -la to list files and directories in the current directory
RUN ls

# Replace API Key in Config File
RUN sed -i "s|\"API_KEY\"|\"$API_KEY\"|g" Telegram.Bot.AsJoke.Polling/appsettings.json

# List the content of appsettings.json
RUN cat Telegram.Bot.AsJoke.Polling/appsettings.json

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:7.0
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "Telegram.BotAsJoke.Polling.dll"]
