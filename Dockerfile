# Use .NET 9.0 Preview SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build

WORKDIR /app

# Copy solution and restore dependencies
COPY *.sln ./
COPY projectTracker.Application/*.csproj ./projectTracker.Application/
COPY projectTracker.Infrastructure/*.csproj ./projectTracker.Infrastructure/
COPY projectTracker.Domain/*.csproj ./projectTracker.Domain/
COPY projectTracker.Api/*.csproj ./projectTracker.Api/

RUN dotnet restore

# Copy everything else and build
COPY . .

WORKDIR /app/projectTracker.Api
RUN dotnet publish -c Release -o out

# Use .NET 9.0 Preview ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview AS base

WORKDIR /app
COPY --from=build /app/projectTracker.Api/out .

# Set the entry point
ENTRYPOINT ["dotnet", "projectTracker.Api.dll"]
