# Build and publish .NET API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/*.sln ./
COPY backend/src/Api/Api.csproj backend/src/Api/
COPY backend/src/Simulation/Simulation.csproj backend/src/Simulation/
COPY backend/src/Processing/Processing.csproj backend/src/Processing/
COPY backend/src/Infrastructure/Infrastructure.csproj backend/src/Infrastructure/
RUN dotnet restore backend/src/Api/Api.csproj
COPY backend/ ./backend/
RUN dotnet publish backend/src/Api/Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443
# Install curl for container healthchecks
RUN apt-get update \
	&& apt-get install -y --no-install-recommends curl \
	&& rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]
