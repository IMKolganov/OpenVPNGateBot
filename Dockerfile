# Define the TARGETARCH argument
ARG TARGETARCH

# Use the .NET SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Check if the argument is passed
ARG TARGETARCH
RUN if [ -z "$TARGETARCH" ]; then echo "ERROR: TARGETARCH is not set!"; exit 1; fi
RUN echo "BUILD STAGE: TARGETARCH=${TARGETARCH}"

# Set the working directory
WORKDIR /src

# Debug: Display the directory contents before copying
RUN echo "Contents before copying:" && ls -la

# Copy the solution file
COPY DataGateVPNBot.sln ./

# Copy only .csproj files to optimize caching
COPY . /
RUN find / -name "*.csproj" -exec cp --parents {} . \;

# Debug: Display copied .csproj files
RUN echo "Copied .csproj files:" && find . -name "*.csproj"

# Restore dependencies (this gets cached if files don’t change)
RUN dotnet restore DataGateVPNBot.sln

# Copy the entire source code after restore
COPY . .

# Debug: Display the contents before building
RUN echo "Contents before building:" && ls -la

# Require BUILD_CONFIGURATION argument (no default value)
ARG BUILD_CONFIGURATION
RUN if [ -z "$BUILD_CONFIGURATION" ]; then echo "ERROR: BUILD_CONFIGURATION is not set!"; exit 1; fi
RUN echo "BUILD_CONFIGURATION=${BUILD_CONFIGURATION}"

# Build the solution
RUN echo "Building for TARGETARCH=${TARGETARCH}, BUILD_CONFIGURATION=${BUILD_CONFIGURATION}" && \
    dotnet build DataGateVPNBot.sln -c ${BUILD_CONFIGURATION} --no-restore

# Pass the TARGETARCH variable to the next FROM stage
FROM build AS publish
ARG TARGETARCH
ARG BUILD_CONFIGURATION

# Check if TARGETARCH and BUILD_CONFIGURATION are set
RUN if [ -z "$TARGETARCH" ]; then echo "ERROR: TARGETARCH is not set!"; exit 1; fi
RUN if [ -z "$BUILD_CONFIGURATION" ]; then echo "ERROR: BUILD_CONFIGURATION is not set!"; exit 1; fi
RUN echo "PUBLISH STAGE: TARGETARCH=${TARGETARCH}, BUILD_CONFIGURATION=${BUILD_CONFIGURATION}"

# Debug: Check the `dotnet publish` command before executing
RUN echo "Running: dotnet publish DataGateVPNBot/DataGateVPNBot.csproj -c ${BUILD_CONFIGURATION} --runtime linux-${TARGETARCH} --self-contained false -o /app/publish"

# Publish the application (Runtime is required)
RUN dotnet publish DataGateVPNBot/DataGateVPNBot.csproj \
    -c ${BUILD_CONFIGURATION} \
    --runtime linux-${TARGETARCH} \
    --self-contained false \
    -o /app/publish

# Final image with .NET runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Check if TARGETARCH is still available
ARG TARGETARCH
RUN if [ -z "$TARGETARCH" ]; then echo "ERROR: TARGETARCH is not set!"; exit 1; fi
RUN echo "FINAL STAGE: TARGETARCH=${TARGETARCH}"

# Update packages
RUN apt-get update && apt-get install -y curl

# Create a non-root user
RUN id -u app >/dev/null 2>&1 || useradd -m app

# Debug: Check the current user
RUN echo "Current user: $(whoami)"

# Create the application directory
RUN mkdir -p /app && chown -R app:app /app

# Switch to a non-root user
USER app

# Set the working directory
WORKDIR /app

# Copy the compiled application
COPY --from=publish /app/publish .

# Debug: Display the contents before running
RUN echo "Contents before execution:" && ls -la

# Set the entry point
ENTRYPOINT ["dotnet", "DataGateVPNBot.dll"]