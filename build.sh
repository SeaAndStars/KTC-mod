#!/bin/bash
# Kingdom Enhanced Mod - Build Script for Linux / Steam Deck
set -e

SKIP_MONO=false
# Optional BepInEx plugins root; when empty, only builds without deploying.
PLUGINS_PATH=""
while [[ $# -gt 0 ]]; do
    case "$1" in
        --skip-mono) SKIP_MONO=true; shift ;;
        --plugins-path)
            if [[ $# -lt 2 ]]; then
                echo "Usage: $0 [--skip-mono] [--plugins-path <path>]"
                exit 1
            fi
            PLUGINS_PATH="$2"
            shift 2
            ;;
        *) echo "Usage: $0 [--skip-mono] [--plugins-path <path>]"; exit 1 ;;
    esac
done

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CSPROJ="$SCRIPT_DIR/KingdomEnhanced/KingdomEnhanced.csproj"
FAILED=false

# Deploys a build output and the full localization directory to the local BepInEx plugins directory.
deploy_build_output() {
    local configuration="$1"

    if [[ -z "$PLUGINS_PATH" ]]; then
        return
    fi

    local output_dir="$SCRIPT_DIR/KingdomEnhanced/bin/$configuration"
    local dll_source="$output_dir/KingdomEnhanced.dll"
    local localization_source="$SCRIPT_DIR/KingdomEnhanced/Localization"
    local plugin_dir="$PLUGINS_PATH/KingdomEnhanced"
    local localization_dir="$plugin_dir/Localization"

    if [[ ! -f "$dll_source" ]]; then
        echo "Build DLL not found: $dll_source" >&2
        exit 1
    fi
    if [[ ! -d "$localization_source" ]]; then
        echo "Localization directory not found: $localization_source" >&2
        exit 1
    fi

    mkdir -p "$localization_dir"
    cp "$dll_source" "$plugin_dir/"
    cp -R "$localization_source"/. "$localization_dir/"
    echo "Deployed $configuration to: $plugin_dir"
}

echo -e "\033[36m========================================\033[0m"
echo -e "\033[36m  Kingdom Enhanced - Build All Configs\033[0m"
echo -e "\033[36m========================================\033[0m"

echo -e "\n\033[33m[1/2] Building BIE6_IL2CPP...\033[0m"
dotnet build "$CSPROJ" -c BIE6_IL2CPP
if [ $? -ne 0 ]; then
    echo -e "\033[31mBIE6_IL2CPP build FAILED!\033[0m"
    FAILED=true
else
    echo -e "\033[32mBIE6_IL2CPP build succeeded.\033[0m"
    deploy_build_output "BIE6_IL2CPP"
fi

if [ "$SKIP_MONO" = false ]; then
    echo -e "\n\033[33m[2/2] Building BIE6_Mono...\033[0m"
    dotnet build "$CSPROJ" -c BIE6_Mono
    if [ $? -ne 0 ]; then
        echo -e "\033[31mBIE6_Mono build FAILED!\033[0m"
        FAILED=true
    else
        echo -e "\033[32mBIE6_Mono build succeeded.\033[0m"
        deploy_build_output "BIE6_Mono"
    fi
fi

if [ "$FAILED" = true ]; then
    echo -e "\n\033[31mBuild completed with errors.\033[0m"
    exit 1
else
    echo -e "\n\033[32mAll builds succeeded!\033[0m"
fi
