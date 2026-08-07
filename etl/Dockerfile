# Reference ETL Server Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copy build files
COPY nuget.config .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY EtlServer.slnx .
# Copy project file for restore
COPY src/Reference.Etl.Server/Reference.Etl.Server.csproj src/Reference.Etl.Server/
# Restore
RUN dotnet restore src/Reference.Etl.Server/Reference.Etl.Server.csproj
# Copy all source files
COPY src/ src/
# Publish
RUN dotnet publish src/Reference.Etl.Server/Reference.Etl.Server.csproj \
    -c Release \
    -o /app/publish \
    --no-restore
# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN groupadd -r fdw && useradd -r -g fdw -s /sbin/nologin fdw
USER fdw
COPY --from=build /app/publish .
EXPOSE 5002
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD curl -sf http://localhost:5002/health || exit 1
ENTRYPOINT ["dotnet", "Reference.Etl.Server.dll"]
