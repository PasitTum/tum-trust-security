# ==============================================================================
# Multi-Stage Dockerfile for TUM TRUST SECURITY Identity Gateway
# Multi-arch compatible (.NET 8.0 LTS)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore
COPY TUM.Identity.SDK.csproj ./
COPY MockServer/TUM.Identity.MockServer.csproj ./MockServer/
RUN dotnet restore MockServer/TUM.Identity.MockServer.csproj

# Copy source and publish
COPY . ./
RUN dotnet publish MockServer/TUM.Identity.MockServer.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TUM.Identity.MockServer.dll"]
