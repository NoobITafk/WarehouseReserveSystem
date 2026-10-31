FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY WarehouseReserveSystem.sln ./
COPY WarehouseReserveSystem/WarehouseReserveSystem.csproj WarehouseReserveSystem/
COPY WarehouseReserveSystem.Tests/WarehouseReserveSystem.Tests.csproj WarehouseReserveSystem.Tests/
RUN dotnet restore WarehouseReserveSystem/WarehouseReserveSystem.csproj

COPY . ./
RUN dotnet publish WarehouseReserveSystem/WarehouseReserveSystem.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install fontconfig & DejaVu fonts for PdfSharpCore Waybill generation
RUN apt-get update && apt-get install -y --no-install-recommends \
    fontconfig \
    fonts-dejavu-core \
    libgdiplus \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish ./

RUN mkdir -p /app/logs     && chown -R $APP_UID:$APP_UID /app

USER $APP_UID

ENTRYPOINT ["dotnet", "WarehouseReserveSystem.dll"]
