# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (better layer caching)
COPY EduNest_Backend.sln .
COPY EduNest_Backend/EduNest_Backend.csproj EduNest_Backend/
COPY DataAccessLayer/DataAccessLayer.csproj DataAccessLayer/
COPY BusinessLayer/BusinessLayer.csproj BusinessLayer/

# Restore dependencies
RUN dotnet restore EduNest_Backend/EduNest_Backend.csproj

# Copy everything else
COPY . .

# Build and publish
RUN dotnet publish EduNest_Backend/EduNest_Backend.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published files from build stage
COPY --from=build /app/publish .

# Render uses PORT environment variable
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "EduNest_Backend.dll"]