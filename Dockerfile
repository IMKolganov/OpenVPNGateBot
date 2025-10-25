# Use the .NET SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set the working directory
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["DataGateVPNBot/DataGateVPNBot.csproj", "DataGateVPNBot/"]
WORKDIR /src/DataGateVPNBot
RUN dotnet restore "DataGateVPNBot.csproj"

# Copy the rest of the application source code
WORKDIR /src
COPY . .

# Publish the application (framework-dependent)
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN echo "Using build configuration: $BUILD_CONFIGURATION" && \
    dotnet publish "DataGateVPNBot/DataGateVPNBot.csproj" \
      -c $BUILD_CONFIGURATION \
      -o /app/publish

# Use the ASP.NET runtime for the final image (framework-dependent)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Use root initially to allow setting permissions
USER root

WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Copy entrypoint script
COPY entrypoint.sh /entrypoint.sh

# 🔧 Convert CRLF to LF just in case
RUN sed -i 's/\r$//' /entrypoint.sh

RUN chmod +x /entrypoint.sh

# Don't switch to app here — entrypoint.sh will drop privileges
ENTRYPOINT ["/entrypoint.sh"]