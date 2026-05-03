# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 8080
EXPOSE 8081

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["SevSharks.Identity.WebUI/SevSharks.Identity.WebUI.csproj", "SevSharks.Identity.WebUI/"]
COPY ["SevSharks.Identity.BusinessLogic/SevSharks.Identity.BusinessLogic.csproj", "SevSharks.Identity.BusinessLogic/"]
COPY ["SevSharks.Identity.DataAccess/SevSharks.Identity.DataAccess.csproj", "SevSharks.Identity.DataAccess/"]
RUN dotnet restore "./SevSharks.Identity.WebUI/SevSharks.Identity.WebUI.csproj"
COPY . .
WORKDIR "/src/SevSharks.Identity.WebUI"
RUN dotnet build "./SevSharks.Identity.WebUI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SevSharks.Identity.WebUI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app

# Создаём директории для хранения ключей
RUN mkdir -p /app/DataProtectionKeys && \
    mkdir -p /app/keys && \
    chown -R 1000:1000 /app

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SevSharks.Identity.WebUI.dll"]
