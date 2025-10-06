# Use the official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set working directory
WORKDIR /app

# Copy solution file
COPY OilTrading-Production.sln .

# Copy project files
COPY src/OilTrading.Core/*.csproj ./src/OilTrading.Core/
COPY src/OilTrading.Application/*.csproj ./src/OilTrading.Application/
COPY src/OilTrading.Infrastructure/*.csproj ./src/OilTrading.Infrastructure/
COPY src/OilTrading.Api/*.csproj ./src/OilTrading.Api/

# Restore dependencies
RUN dotnet restore "OilTrading-Production.sln"

# Copy source code
COPY src/ ./src/

# Build the application
RUN dotnet build src/OilTrading.Api/OilTrading.Api.csproj -c Release --no-restore

# Publish the application
RUN dotnet publish src/OilTrading.Api/OilTrading.Api.csproj -c Release -o /app/publish --no-restore

# Use the official .NET 9 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Set working directory
WORKDIR /app

# Install Python for risk engine and curl for health checks
RUN apt-get update && apt-get install -y \
    python3 \
    python3-pip \
    python3-venv \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Create virtual environment and install Python packages
RUN python3 -m venv /opt/venv
RUN /opt/venv/bin/pip install --upgrade pip
RUN /opt/venv/bin/pip install numpy pandas scipy arch

# Add virtual environment to PATH
ENV PATH="/opt/venv/bin:$PATH"

# Copy published application
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p logs

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Expose port
EXPOSE 8080

# Add health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "OilTrading.Api.dll"]