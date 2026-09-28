#!/usr/bin/env just --justfile
# ==========================================
# DEFAULT
# ==========================================

# Show all available commands
@default:
    just --list

plugin := "plugin/ChatFade.csproj"

dev_plugins := env_var('HOME') / ".xlcore/devPlugins/ChatFade"
# Dalamud's SDK needs the game's assemblies; XIVLauncher.Core already has them.
dalamud_home := env_var('HOME') / ".xlcore/dalamud/Hooks/dev"
# NixOS has no system dotnet — pull the SDK from nixpkgs for the duration.
dotnet := "nix shell nixpkgs#dotnet-sdk_10 -c env DALAMUD_HOME=" + dalamud_home + " DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 dotnet"

# ==========================================
# PLUGIN
# ==========================================

restore:
    {{dotnet}} restore {{plugin}}

# Debug build
build-plugin: restore
    {{dotnet}} build {{plugin}} --configuration Debug --no-restore

# Release build (what CI ships)
build-release: restore
    {{dotnet}} build {{plugin}} --configuration Release --no-restore

# Build and drop the plugin into XIVLauncher's devPlugins for /xlplugins dev mode
install: build-plugin
    rm -rf {{dev_plugins}}
    mkdir -p {{dev_plugins}}
    # The whole build output, not a hand-picked subset: Dalamud identifies a dev
    # plugin by the ChatFade.json manifest next to the DLL, and needs the
    # .deps.json to resolve assemblies.
    cp -r plugin/bin/Debug/. {{dev_plugins}}/
    @echo "Installed to {{dev_plugins}}."
    @echo "Now: /xlplugins -> Dev Tools -> reload, or restart the game."

# ==========================================
# QUALITY
# ==========================================

fmt:
    {{dotnet}} format style {{plugin}}
    {{dotnet}} format analyzers {{plugin}}

fmt-check:
    {{dotnet}} format style {{plugin}} --verify-no-changes
    {{dotnet}} format analyzers {{plugin}} --verify-no-changes

# Everything CI runs
check: fmt-check build-plugin
    prek run --all-files

# Install the git hooks
hooks:
    prek install --install-hooks
    prek install --hook-type commit-msg

clean:
    rm -rf dist plugin/bin plugin/obj ChatFade.zip pack/
