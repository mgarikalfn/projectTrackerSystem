# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build
WORKDIR /src

# Copy solution and project files
COPY projectTracker.sln ./
COPY projectTracker/projectTracker.Api.csproj ./projectTracker/
COPY projectTracker.Application/projectTracker.Application.csproj ./projectTracker.Application/
COPY projectTracker.Infrastructure/projectTracker.Infrastructure.csproj ./projectTracker.Infrastructure/
COPY projectTracker.Domain/projectTracker.Domain.csproj ./projectTracker.Domain/

# Restore dependencies
RUN dotnet restore

# Copy everything
COPY . .

# Build the application
WORKDIR /src/projectTracker
RUN dotnet build -c Release --no-restore

# Publish the application
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "projectTracker.Api.dll"]
