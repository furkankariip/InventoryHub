FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Önce yalnızca proje dosyalarını kopyala; paket geri yükleme katmanı önbellekte kalsın.
COPY global.json InventoryHub.sln ./
COPY src/InventoryHub.Domain/*.csproj src/InventoryHub.Domain/
COPY src/InventoryHub.Application/*.csproj src/InventoryHub.Application/
COPY src/InventoryHub.Infrastructure/*.csproj src/InventoryHub.Infrastructure/
COPY src/InventoryHub.Web/*.csproj src/InventoryHub.Web/
RUN dotnet restore src/InventoryHub.Web/InventoryHub.Web.csproj

COPY src/ src/
RUN dotnet publish src/InventoryHub.Web/InventoryHub.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "InventoryHub.Web.dll"]
