# Reference API Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG NUGET_FEED_SOURCE
ARG NUGET_FEED_USER
ARG NUGET_FEED_TOKEN

# Copy build files
COPY nuget.config .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY ApiSolution.slnx .
# Copy project file for restore
COPY src/Reference.Api/Reference.Api.csproj src/Reference.Api/
# Authenticate the private package feed. Which feed, and the credential shape it wants, are
# the caller's concern — this image only needs to be told.
RUN if [ -n "$NUGET_FEED_TOKEN" ]; then \
        dotnet nuget update source "$NUGET_FEED_SOURCE" \
            --username "$NUGET_FEED_USER" \
            --password "$NUGET_FEED_TOKEN" \
            --store-password-in-clear-text \
            --configfile nuget.config; \
    fi
# Restore
RUN dotnet restore src/Reference.Api/Reference.Api.csproj --locked-mode
# Copy all source files
COPY src/ src/
# Publish
RUN dotnet publish src/Reference.Api/Reference.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore
# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN groupadd -r fdw && useradd -r -g fdw -s /sbin/nologin fdw
USER fdw
COPY --from=build /app/publish .
EXPOSE 5000
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD curl -sf http://localhost:5000/health || exit 1
ENTRYPOINT ["dotnet", "Reference.Api.dll"]
