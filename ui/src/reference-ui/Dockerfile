# ManagementUI-Tailwind Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copy root build files
COPY nuget.config .
COPY Directory.Build.props .
COPY Directory.Packages.props .
# Copy project files
COPY ManagementUI-Tailwind/ ManagementUI-Tailwind/
# Restore and publish
RUN dotnet publish ManagementUI-Tailwind/ManagementUI-Tailwind.csproj \
    -c Release \
    -o /app/publish
# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN groupadd -r fdw && useradd -r -g fdw -s /sbin/nologin fdw
USER fdw
COPY --from=build /app/publish .
EXPOSE 5008
ENTRYPOINT ["dotnet", "ManagementUI-Tailwind.dll"]
