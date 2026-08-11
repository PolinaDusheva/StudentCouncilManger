# syntax=docker/dockerfile:1
# Multi-stage build for the Student Council API (spec 18). Build context is the `backend/` directory:
#   docker build -f backend/Dockerfile -t studentcouncil-api backend

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against the project files first so the restore layer is cached until a .csproj changes.
COPY Directory.Build.props ./
COPY StudentCouncil.slnx ./
COPY src/StudentCouncil.Domain/StudentCouncil.Domain.csproj src/StudentCouncil.Domain/
COPY src/StudentCouncil.Application/StudentCouncil.Application.csproj src/StudentCouncil.Application/
COPY src/StudentCouncil.Infrastructure/StudentCouncil.Infrastructure.csproj src/StudentCouncil.Infrastructure/
COPY src/StudentCouncil.Api/StudentCouncil.Api.csproj src/StudentCouncil.Api/
RUN dotnet restore src/StudentCouncil.Api/StudentCouncil.Api.csproj

# Copy the rest of the source and publish a release build.
COPY src/ src/
RUN dotnet publish src/StudentCouncil.Api/StudentCouncil.Api.csproj -c Release -o /app --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is only needed for the container HEALTHCHECK; install it before dropping privileges.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./

# Run as the image's built-in non-root user (spec 18).
USER app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Liveness probe against the dependency-free endpoint.
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -fsS http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "StudentCouncil.Api.dll"]
