# =======================================
# BUILD STAGE
# =======================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore early
COPY ["EcoguardPoller.csproj", "./"]
RUN dotnet restore "EcoguardPoller.csproj"

# Copy the remaining files and build
COPY . . 
RUN dotnet publish "EcoguardPoller.csproj" -c Release -o /app/publish /p:UseAppHost=false


# =======================================
# FINAL STAGE (runtime-only image)
# =======================================
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Set default environment (can be overridden at runtime)
ENV DOTNET_ENVIRONMENT=Production

# Default command
ENTRYPOINT ["dotnet", "EcoguardPoller.dll"]
