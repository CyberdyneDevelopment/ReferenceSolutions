# Reference Scheduler Server Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copy build files
COPY nuget.config .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY SchedulerServer.slnx .
# Copy project file for restore
COPY src/Reference.Scheduler.Server/Reference.Scheduler.Server.csproj src/Reference.Scheduler.Server/
# Restore
RUN dotnet restore src/Reference.Scheduler.Server/Reference.Scheduler.Server.csproj
# Copy all source files
COPY src/ src/
# Publish
RUN dotnet publish src/Reference.Scheduler.Server/Reference.Scheduler.Server.csproj \
    -c Release \
    -o /app/publish \
    --no-restore
# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN groupadd -r fdw && useradd -r -g fdw -s /sbin/nologin fdw
USER fdw
COPY --from=build /app/publish .
EXPOSE 5004
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD curl -sf http://localhost:5004/health || exit 1
ENTRYPOINT ["dotnet", "Reference.Scheduler.Server.dll"]
