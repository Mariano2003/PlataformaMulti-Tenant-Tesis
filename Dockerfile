# Build context: raíz del repo (donde está MarketSaaS.sln)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY MarketSaaS.sln ./
COPY src/MarketSaaS.Api/MarketSaaS.Api.csproj src/MarketSaaS.Api/
RUN dotnet restore src/MarketSaaS.Api/MarketSaaS.Api.csproj
COPY src/MarketSaaS.Api/ src/MarketSaaS.Api/
WORKDIR /src/src/MarketSaaS.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
# Render inyecta PORT; fallback 8080 para pruebas locales
EXPOSE 8080
CMD ["sh", "-c", "exec dotnet MarketSaaS.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
