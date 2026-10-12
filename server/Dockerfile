# Two stages: the SDK compiles, and only the published output goes into the
# smaller runtime image, without the SDK or the source code.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# The project file alone first, so Docker reuses the restored packages until
# a dependency changes, not on every code change.
COPY DeliveryTracking.Api/DeliveryTracking.Api.csproj DeliveryTracking.Api/
RUN dotnet restore DeliveryTracking.Api/DeliveryTracking.Api.csproj

COPY DeliveryTracking.Api/ DeliveryTracking.Api/
RUN dotnet publish DeliveryTracking.Api/DeliveryTracking.Api.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# The image's built-in non-root user, so a compromised app can't change the
# container's system files.
USER $APP_UID

# Render chooses the port and passes it in PORT; 8080 is the image's default
# for running locally. exec replaces the shell with dotnet, so dotnet gets
# Render's stop signal and shuts down cleanly.
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet DeliveryTracking.Api.dll"]
