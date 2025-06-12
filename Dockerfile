# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY projectTracker.sln .
COPY projectTracker.Application/projectTracker.Application.csproj projectTracker.Application/
COPY projectTracker.Domain/projectTracker.Domain.csproj projectTracker.Domain/
COPY projectTracker.Infrastructure/projectTracker.Infrastructure.csproj projectTracker.Infrastructure/
COPY projectTracker/projectTracker.Api.csproj projectTracker/

# Restore
RUN dotnet restore projectTracker.sln

# Copy everything and publish
COPY . .
RUN dotnet publish projectTracker/projectTracker.Api.csproj -c Release -o /app/out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "projectTracker.Api.dll"]
