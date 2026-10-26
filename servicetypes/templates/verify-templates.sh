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

# Why the default versions are read from the base file and are never written in this script: a version
# typed here goes stale at the next base bump without failing. The retired packages are still on the
# feed, so every template would compile green against an API that has since moved, and this script
# would report PASS for code that would not build in its real home. Compiling proves a template
# matches SOME published version; reading the base proves it is built against the CURRENT one. The
# script stops, naming the file, when it cannot read the base. Setting FDW_VERSION or
# REFERENCE_VERSION on the command line is still allowed: testing against another base is a
# deliberate act.
BASE_PROPS="$REPO/../Directory.Build.props"
if [[ ! -f "$BASE_PROPS" ]]; then
  echo "no base version file at $BASE_PROPS - the default versions are read from its VersionPrefix and VersionSuffix" >&2
  exit 1
fi
base_prefix="$(sed -n 's:.*<VersionPrefix>\(.*\)</VersionPrefix>.*:\1:p' "$BASE_PROPS" | head -1)"
base_suffix="$(sed -n 's:.*<VersionSuffix>\(.*\)</VersionSuffix>.*:\1:p' "$BASE_PROPS" | head -1)"
if [[ -z "$base_prefix" ]]; then
  echo "no <VersionPrefix> in $BASE_PROPS - the default versions are read from it" >&2
  exit 1
fi
expected="$base_prefix${base_suffix:+-$base_suffix}.*"

FDW_VERSION="${FDW_VERSION:-$expected}"
REFERENCE_VERSION="${REFERENCE_VERSION:-$expected}"
NUGET_CONFIG="${NUGET_CONFIG:-$HOME/.nuget/NuGet/NuGet.Config}"

if [[ ! -f "$NUGET_CONFIG" ]]; then
  echo "no NuGet config at $NUGET_CONFIG - set NUGET_CONFIG to one declaring the Fdw feeds" >&2
  exit 1
fi

# A template can also pin the base itself, through a symbol whose replaces is FDW_VERSION. Every
# template here takes its versions from Directory.Packages.props instead, so no such symbol should
# exist - but one reintroduced would go stale at the next bump without failing, since retired
# packages still resolve and the template still compiles. Keyed on the replaces target rather than
# position: a template carries several defaults and matching the wrong one passes by accident.
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

# Why every mode and operation: the three modes are different files and the five operations are different bases, so one build proves one of fifteen.
for mode in Plain Base Seal; do
  for operation in List Read Create Update Delete; do
    echo "== generate: endpoint group ($mode, $operation)"
    dotnet new fdw-ref-endpoint-group -o "$WORK/Widget.Endpoints-$mode-$operation" -n Widget --Permission widgets --Mode "$mode" --Operation "$operation" >/dev/null
    echo "== build: endpoint group ($mode, $operation)"
    dotnet build "$WORK/Widget.Endpoints-$mode-$operation/Widget.Endpoints" -c Release --nologo -v minimal --configfile "$NUGET_CONFIG" || FAILED=1
  done
done

# A generated endpoint states its right: a route that names none needs no sign-in, and the project still builds clean (FDW-2225).
if grep -rq "AllowAnonymous" "$WORK"/Widget.Endpoints-*/ --include=*.cs; then
  echo "an endpoint group template generated AllowAnonymous" >&2
  FAILED=1
fi

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
dotnet build "$WORK/WidgetImpl/FdwReferenceNotifications.Widget" -c Release --nologo -v minimal --configfile "$NUGET_CONFIG" || FAILED=1

# The item templates emit one file each, which cannot compile alone, so each is generated here and searched
# below; the collection and the option, which can compile as a pair, are built together after this loop. A
# template that fails to generate is the defect; anything past that is the caller's file.
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
# A generated item must not compose onto a phase it declares: that is the error STC001 in FDW's own tree, and
# the analyzer is not packaged, so a project built from the feed never reports it. Calls only: a remark may
# name the methods when it explains who they are for.
if grep -rnE '\b(Append|Prepend)(Configuration|Registration|Initialization)[[:space:]]*\(' "$WORK"/item-*/ --include=*.cs \
   | grep -vE ':[0-9]+:[[:space:]]*//' ; then
  echo "an item template generated code that composes onto a phase func (Append or Prepend); set it with Registration, Initialization or Configuration" >&2
  FAILED=1
fi

# The collection and the option only compile together: the option names the collection's log class and base, and
# neither ships the interface, the service or its interface that they name. Generate both into one project, add
# those three small types, and build it.
echo "== generate: collection and option together"
ITEMS="$WORK/ItemServiceType"
mkdir -p "$ITEMS"
cat > "$ITEMS/ItemServiceType.csproj" <<'ITEMPROJEOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>preview</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Fdw.Abstractions" />
    <PackageReference Include="Fdw.Results" />
    <PackageReference Include="Fdw.Collections" />
    <PackageReference Include="Fdw.Collections.SourceGenerators" />
    <PackageReference Include="Fdw.MessageLogging.Abstractions" />
    <PackageReference Include="Fdw.MessageLogging.SourceGenerators" />
    <PackageReference Include="Fdw.Messages" />
    <PackageReference Include="Fdw.Services.Abstractions" />
    <PackageReference Include="Fdw.Services" />
    <PackageReference Include="Fdw.Services.Logging" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
  </ItemGroup>
</Project>
ITEMPROJEOF
if dotnet new fdw-collection -o "$ITEMS" -n SampleDomain --category Widgets --namespace WidgetItems >/dev/null \
   && dotnet new fdw-option -o "$ITEMS" -n SampleOption --collection SampleDomainTypes --optionName Widget --namespace WidgetItems >/dev/null; then
  cat > "$ITEMS/CallersFile.cs" <<'CALLERSEOF'
namespace WidgetItems;

public interface ISampleDomainType : Fdw.ServiceTypes.IServiceType
{
}

public interface ISampleService
{
}

public sealed class SampleService : ISampleService
{
}
CALLERSEOF
  echo "== build: collection and option together"
  dotnet build "$ITEMS/ItemServiceType.csproj" -c Release --nologo -v minimal --configfile "$NUGET_CONFIG" || FAILED=1
else
  echo "   the collection and option templates FAILED to generate together" >&2
  FAILED=1
fi

if [[ "$FAILED" == "0" ]]; then
  echo "PASS — every template generated code that compiles."
else
  echo "FAIL — at least one template generated code that does not compile." >&2
  exit 1
fi
