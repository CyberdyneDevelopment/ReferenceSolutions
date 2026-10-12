#!/usr/bin/env bash
# Generate from every template in this repo and COMPILE the output.
#
# Why this exists: until it did, nothing in this repo built. A template carrying a call signature
# the framework had dropped months earlier packed, installed and generated perfectly — and the
# breakage only appeared when a person compiled the generated code by hand. Instantiating each
# template and building it is the only check that catches that.
#
# The template store is redirected with DOTNET_CLI_HOME so this never disturbs the developer's own
# `dotnet new install` list, and so CI starts from an empty store every run. That redirect also
# moves where NuGet looks for the user-level config, which is why restore is pointed back at the
# real one explicitly -- the Fdw.* and Reference* packages live on the internal package feeds it
# declares. The config is referenced in place, never copied: it holds feed credentials.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

export DOTNET_CLI_HOME="$WORK/clihome"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p "$DOTNET_CLI_HOME"

FDW_VERSION="${FDW_VERSION:-1.1.0-rc.1.*}"
REFERENCE_VERSION="${REFERENCE_VERSION:-1.1.0-rc.1.*}"
NUGET_CONFIG="${NUGET_CONFIG:-$HOME/.nuget/NuGet/NuGet.Config}"

if [[ ! -f "$NUGET_CONFIG" ]]; then
  echo "no NuGet config at $NUGET_CONFIG - set NUGET_CONFIG to one declaring the Fdw feeds" >&2
  exit 1
fi

# Why this runs before anything is packed or built: the defaults above decide which packages the
# generated projects restore, and a default left behind after a base bump does not fail. The
# retired packages are still on the feed, so every template compiles green against an API that has
# since moved, and this script reports PASS for code that would not build in its real home. That is
# not hypothetical - the sibling reference-templates repo sat four bases behind on 1.0.0-rc.6.1.*,
# and six of its seven build targets were broken against current packages while its gate passed.
# Compiling proves a template matches SOME published version; only this proves it matches the
# CURRENT one. Overriding either variable on the command line is still allowed and skips the check
# for that variable, since testing against another base is a deliberate act.
BASE_PROPS="$REPO/../Directory.Build.props"
if [[ -f "$BASE_PROPS" ]]; then
  base_prefix="$(sed -n 's:.*<VersionPrefix>\(.*\)</VersionPrefix>.*:\1:p' "$BASE_PROPS" | head -1)"
  base_suffix="$(sed -n 's:.*<VersionSuffix>\(.*\)</VersionSuffix>.*:\1:p' "$BASE_PROPS" | head -1)"
  if [[ -n "$base_prefix" ]]; then
    expected="$base_prefix${base_suffix:+-$base_suffix}.*"
    # The literal committed above, not the runtime value: an override on the command line is a
    # deliberate act and must stay possible, while a default left behind in the file is the defect.
    for var in FDW_VERSION REFERENCE_VERSION; do
      declared="$(sed -n "s/^$var=\"\\\${$var:-\\(.*\\)}\"$/\1/p" "${BASH_SOURCE[0]}" | head -1)"
      if [[ -n "$declared" && "$declared" != "$expected" ]]; then
        echo "stale version default: $var defaults to '$declared' but this repo's base is '$expected'" >&2
        echo "  a stale default restores retired packages and hides API drift from this gate" >&2
        exit 1
      fi
    done

    # A template can also pin the base itself, through a symbol whose replaces is FDW_VERSION.
    # Every template here takes its versions from Directory.Packages.props instead, so no such
    # symbol should exist - but one reintroduced would go stale at the next bump without failing,
    # since retired packages still resolve and the template still compiles. Keyed on the replaces
    # target rather than position: a template carries several defaults and matching the wrong one
    # passes by accident.
    for tmpl in "$REPO"/*/.template.config/template.json; do
      [[ -f "$tmpl" ]] || continue
      pinned="$(python3 -c "
import json,sys
s = json.load(open(sys.argv[1])).get('symbols') or {}
print(next((x.get('defaultValue','') for x in s.values()
            if x.get('replaces') == 'FDW_VERSION'), ''))" "$tmpl")"
      [[ -z "$pinned" ]] && continue
      if [[ "$pinned" != "$expected" ]]; then
        echo "stale version default: $(basename "$(dirname "$(dirname "$tmpl")")") pins '$pinned' but this repo's base is '$expected'" >&2
        echo "  a template that pins its own base goes stale without failing - take the version from Directory.Packages.props" >&2
        exit 1
      fi
    done
  fi
fi

echo "== pack"
dotnet pack "$REPO/FdwReferenceTemplates.csproj" -c Release -o "$WORK/pkg" --nologo -v quiet

echo "== install into an isolated store"
dotnet new install "$WORK"/pkg/FdwReference.Templates.*.nupkg >/dev/null

# Why a Directory.Packages.props rather than versions in each template csproj: the templates emit
# versionless <PackageReference>, because they are generated INTO repos that manage versions
# centrally. Pinning versions in the template would go stale the first time the base moves. This
# reproduces the host repo's central management so the generated code restores the same way it
# will in its real home.
cat > "$WORK/Directory.Packages.props" <<PROPSEOF
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <!-- Why: without this, a floating PackageVersion is NU1011. Every repo in the workspace sets
         it, because consumers float the base rather than pinning it. -->
    <CentralPackageFloatingVersionsEnabled>true</CentralPackageFloatingVersionsEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Fdw.Abstractions" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Results" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Collections" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Services.Abstractions" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Services" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Services.Data" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Services.Data.Abstractions" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Services.Registration" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Services.SecretManagers" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Services.TokenManagers" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Services.Authentication" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Services.Authentication.Abstractions" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Services.Logging" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Messages" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.MessageLogging.Abstractions" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Services.Connections" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Services.Connections.Abstractions" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.UI" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Web.Clients" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Web.Http.Authentication" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Configuration.Abstractions" Version="$FDW_VERSION" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.10" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.10" />
    <PackageVersion Include="Fdw.Services.Notifications" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Services.Notifications.Abstractions" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Web.RestEndpoints" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.MessageLogging.SourceGenerators" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Collections.SourceGenerators" Version="$FDW_VERSION" />
    <PackageVersion Include="Fdw.Configuration.SourceGenerators" Version="$FDW_VERSION" />
    <PackageVersion Include="FdwReferenceEndpoints" Version="$REFERENCE_VERSION" />
    <PackageVersion Include="FdwReferenceConnections.MsSql" Version="$REFERENCE_VERSION" />
    <PackageVersion Include="FdwReferenceConnections.MsSql.Registration" Version="$REFERENCE_VERSION" />
    <PackageVersion Include="FdwReferenceConnections.FileSystem.Registration" Version="$REFERENCE_VERSION" />
    <PackageVersion Include="FdwReferenceSecretManagers.EnvironmentVariable.Registration" Version="$REFERENCE_VERSION" />
    <PackageVersion Include="FdwReferenceSecretManagers.EnvironmentVariable" Version="$REFERENCE_VERSION" />
    <PackageVersion Include="FdwReferenceAuth.Endpoints" Version="$REFERENCE_VERSION" />
    <PackageVersion Include="FastEndpoints" Version="8.1.0" />
    <!-- Pinned to match reference-servicetypes' own Directory.Packages.props: MSBuildWorkspace
         arrives transitively through FdwReferenceAuth.Endpoints' Roslyn connection, and the generated
         host excludes runtime assets from both the same way FdwReferenceAuth.Endpoints itself does. -->
    <PackageVersion Include="Microsoft.Build.Framework" Version="18.6.3" />
    <PackageVersion Include="Microsoft.NET.StringTools" Version="18.6.3" />
  </ItemGroup>
</Project>
PROPSEOF

FAILED=0

echo "== generate: endpoint group"
dotnet new fdw-ref-endpoint-group -o "$WORK/Widget.Endpoints" -n Widget >/dev/null
echo "== build: endpoint group"
dotnet build "$WORK/Widget.Endpoints/Widget.Endpoints" -c Release --nologo -v minimal --configfile "$NUGET_CONFIG" || FAILED=1

echo "== generate: api host"
dotnet new fdw-ref-api-host -o "$WORK/WidgetApi" -n WidgetApi >/dev/null
echo "== build: api host"
dotnet build "$WORK/WidgetApi/WidgetApi" -c Release --nologo -v minimal --configfile "$NUGET_CONFIG" || FAILED=1

echo "== generate: auth step"
dotnet new fdw-ref-auth-step -o "$WORK/WidgetStep" -n WidgetStep >/dev/null
echo "== build: auth step"
dotnet build "$WORK/WidgetStep/WidgetStep" -c Release --nologo -v minimal --configfile "$NUGET_CONFIG" || FAILED=1

echo "== generate: service domain"
dotnet new fdw-ref-service-domain -o "$WORK/WidgetDomain" -n WidgetDomain >/dev/null
echo "== build: service domain"
dotnet build "$WORK/WidgetDomain/WidgetDomain" -c Release --nologo -v minimal --configfile "$NUGET_CONFIG" || FAILED=1

echo

echo "== generate: domain"
dotnet new fdw-ref-domain -o "$WORK/WidgetFull" -n WidgetFull --resource widgets --permission widget >/dev/null
echo "== build: domain"
dotnet build "$WORK/WidgetFull/WidgetFull.Endpoints" -c Release --nologo -v minimal --configfile "$NUGET_CONFIG" || FAILED=1

echo "== generate: connection"
dotnet new fdw-ref-connection -o "$WORK/WidgetConn" -n WidgetConn >/dev/null
echo "== build: connection"
dotnet build "$WORK/WidgetConn/WidgetConn.Connection" -c Release --nologo -v minimal --configfile "$NUGET_CONFIG" || FAILED=1

echo "== generate: authentication"
dotnet new fdw-ref-authentication -o "$WORK/WidgetAuth" -n WidgetAuth >/dev/null
echo "== build: authentication"
dotnet build "$WORK/WidgetAuth" -c Release --nologo -v minimal --configfile "$NUGET_CONFIG" || FAILED=1

echo "== generate: implementation"
dotnet new fdw-ref-implementation -o "$WORK/WidgetImpl" --ImplName Widget --ServiceName Notification --ServiceNamePlural Notifications >/dev/null
echo "== build: implementation"
dotnet build "$WORK/WidgetImpl/ReferenceNotifications.Widget" -c Release --nologo -v minimal --configfile "$NUGET_CONFIG" || FAILED=1

# The item templates emit one file each, which cannot compile alone - generation is the whole
# contract. A template that fails to generate is the defect; anything past that is the caller's file.
echo "== generate: item templates"
for item in $(cd "$REPO/items" && ls); do
  cfg="$REPO/items/$item/.template.config/template.json"
  short="$(python3 -c "import json,sys;print(json.load(open(sys.argv[1])).get('shortName',''))" "$cfg")"
  [[ -z "$short" ]] && continue
  # Required symbols differ per template; read them rather than hardcoding a flag list that
  # goes stale the first time a template gains one.
  mapfile -t opts < <(python3 -c "
import json,sys
d=json.load(open(sys.argv[1]))
for k,v in (d.get('symbols') or {}).items():
    if v.get('isRequired'):
        print('--' + k); print('Sample')
" "$cfg")
  if ! dotnet new "$short" -o "$WORK/item-$short" "${opts[@]}" >/dev/null 2>&1; then
    echo "   $short FAILED to generate" >&2
    FAILED=1
  fi
done
if [[ "$FAILED" == "0" ]]; then
  echo "PASS — every template generated code that compiles."
else
  echo "FAIL — at least one template generated code that does not compile." >&2
  exit 1
fi
