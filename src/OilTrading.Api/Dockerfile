# Use the official .NET 9 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

# Install Python and dependencies for risk engine
RUN apt-get update && apt-get install -y \
    python3 \
    python3-pip \
    python3-venv \
    python3-dev \
    build-essential \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Create virtual environment and install Python packages
COPY requirements.txt .
RUN python3 -m venv /opt/venv && \
    /opt/venv/bin/pip install --upgrade pip && \
    /opt/venv/bin/pip install -r requirements.txt

# Add virtual environment to PATH
ENV PATH="/opt/venv/bin:$PATH"
ENV PYTHON_PATH="/opt/venv/bin/python3"

# Use the official .NET 9 SDK as a build image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file (without tests)
COPY OilTrading-Production.sln .

# Copy project files
COPY ["src/OilTrading.Core/OilTrading.Core.csproj", "src/OilTrading.Core/"]
COPY ["src/OilTrading.Application/OilTrading.Application.csproj", "src/OilTrading.Application/"]
COPY ["src/OilTrading.Infrastructure/OilTrading.Infrastructure.csproj", "src/OilTrading.Infrastructure/"]
COPY ["src/OilTrading.Api/OilTrading.Api.csproj", "src/OilTrading.Api/"]

# Restore dependencies
RUN dotnet restore "OilTrading-Production.sln"

# Copy source code
COPY src/ ./src/

# Build the application
WORKDIR "/src/src/OilTrading.Api"
RUN dotnet build "OilTrading.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "OilTrading.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /src/src/OilTrading.Api/Scripts ./Scripts

# Copy virtual environment from base image
COPY --from=base /opt/venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"

# Create logs directory
RUN mkdir -p logs

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "OilTrading.Api.dll"]