# Build context: raíz del repo (donde está MarketSaaS.sln)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1
WORKDIR /src
COPY MarketSaaS.sln ./
COPY src/MarketSaaS.Api/MarketSaaS.Api.csproj src/MarketSaaS.Api/
RUN dotnet restore src/MarketSaaS.Api/MarketSaaS.Api.csproj --disable-parallel
COPY src/MarketSaaS.Api/ src/MarketSaaS.Api/
WORKDIR /src/src/MarketSaaS.Api
# -maxcpucount:1 reduce picos de RAM en builders free (Render)
RUN dotnet publish -c Release -o /app/publish --no-restore -maxcpucount:1

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
ENV DOTNET_EnableDiagnostics=0 \
    ASPNETCORE_URLS=http://0.0.0.0:8080
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
CMD ["sh", "-c", "exec dotnet MarketSaaS.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
