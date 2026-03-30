# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Marketplace.Domain/Marketplace.Domain.csproj src/Marketplace.Domain/
COPY src/Marketplace.Application/Marketplace.Application.csproj src/Marketplace.Application/
COPY src/Marketplace.Infrastructure/Marketplace.Infrastructure.csproj src/Marketplace.Infrastructure/
COPY src/Marketplace.API/Marketplace.API.csproj src/Marketplace.API/

RUN dotnet restore src/Marketplace.API/Marketplace.API.csproj

COPY src/ src/
RUN dotnet publish src/Marketplace.API/Marketplace.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Google OAuth backchannel can require GSSAPI runtime dependency on Linux containers.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Marketplace.API.dll"]
