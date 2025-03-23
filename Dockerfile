# Use the official .NET 9.0.2 SDK image as a build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set the working directory
WORKDIR /app

# Copy the project files
COPY . .

# Restore the dependencies
RUN dotnet restore

# Build the project
RUN dotnet publish -c Release -o out

# Use the official .NET 9.0.2 runtime image as a runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0.2

# Set the working directory
WORKDIR /app

# Copy the build output from the build stage
COPY --from=build /app/out .

# Set the entry point for the application
ENTRYPOINT ["dotnet", "AMScreen-RDM-Sim.dll"]