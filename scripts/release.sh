#!/usr/bin/env bash
set -euo pipefail

project_root=$(cd -- "$(dirname -- "$0")/.." && pwd)
publish=false
if [[ ${1:-} == "--publish" ]]; then
    publish=true
elif [[ $# -ne 0 ]]; then
    echo "Usage: scripts/release.sh [--publish]" >&2
    exit 2
fi

version=$(sed -n 's/.*Current = "\([0-9][0-9.]*\)".*/\1/p' \
    "$project_root/src/VersionInfo.cs")
if [[ ! $version =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Could not read a major.minor.patch version from VersionInfo.cs." >&2
    exit 1
fi

csc=${WINE_CSC:-C:\\windows\\Microsoft.NET\\Framework\\v2.0.50727\\csc.exe}
package="$project_root/dist/harness98-v$version"
updates_parent="$project_root/dist/harness98-updates"
stable="$updates_parent/stable"
manifest="$stable/MANIFEST.INI"

recreate_owned_directory() {
    local target=$1
    case "$target" in
        "$project_root/dist/harness98-v"*|"$project_root/dist/harness98-updates"*) ;;
        *) echo "Refusing to recreate unexpected path: $target" >&2; exit 1 ;;
    esac
    if [[ -L $target ]]; then
        echo "Refusing to recreate a symbolic link: $target" >&2
        exit 1
    fi
    if [[ -d $target ]]; then
        find "$target" -mindepth 1 -delete
    elif [[ -e $target ]]; then
        echo "Expected a directory but found another file type: $target" >&2
        exit 1
    fi
    mkdir -p "$target"
}

echo "Building Harness98 $version..."
cd "$project_root"
cp vendor/libcurl-win98/LibCurlNet.dll .
wine "$csc" @UPDATER.RSP
wine "$csc" @LAUNCHER31.RSP
wine "$csc" @GUI31.RSP
wine "$csc" @CLI31.RSP

echo "Running .NET 2.0 compatibility tests..."
wine "$csc" /nologo /target:exe /platform:x86 \
    /out:HARNESS98-TESTS.EXE /main:Tests \
    /reference:vendor/libcurl-win98/LibCurlNet.dll \
    src/*.cs updater/Updater.cs tests/Tests.cs
wine HARNESS98-TESTS.EXE

echo "Assembling fresh package..."
recreate_owned_directory "$package"
mkdir -p "$package/SRC" "$package/GUI" "$package/LAUNCHER" \
    "$package/UPDATER"
cp HARNESS98.EXE H98GUI.EXE H98CLI.EXE HARNESS98-UPDATER.EXE \
    BUILD31.BAT LAUNCHER31.RSP GUI31.RSP CLI31.RSP UPDATER.RSP \
    RUN3.BAT RUNCLI.BAT HARNESS98.CFG V31README.TXT "$package/"
cp src/*.cs "$package/SRC/"
cp gui/*.cs "$package/GUI/"
cp launcher/*.cs "$package/LAUNCHER/"
cp updater/*.cs "$package/UPDATER/"
cp vendor/libcurl-win98/LibCurlNet.dll \
    vendor/libcurl-win98/LibCurlShim.dll \
    vendor/libcurl-win98/libcurl.dll \
    vendor/libcurl-win98/libeay32.dll \
    vendor/libcurl-win98/ssleay32.dll "$package/"
cp dist/openrouter-chat-v2/CACERT.PEM "$package/"
printf '%s\r\n' "$version" > "$package/VERSION.TXT"
if find "$package" -type f \( -iname '*.key' -o -iname 'OPENROUT.KEY' \) \
    -print -quit | grep -q .; then
    echo "Credential file found in release package; refusing to continue." >&2
    exit 1
fi

echo "Generating update manifest..."
recreate_owned_directory "$stable"
printf 'VERSION=%s\n' "$version" > "$manifest"

add_update_file() {
    local name=$1
    local source=$2
    cp "$source" "$stable/$name"
    local digest
    digest=$(sha256sum "$stable/$name" | cut -d' ' -f1)
    printf 'FILE=%s|%s\n' "$name" "$digest" >> "$manifest"
}

# HARNESS98.EXE changed from the old GUI into the permanent launcher in 3.1.2.
# It is included only in that transition release because a running launcher must
# never try to replace itself. Later releases update H98GUI.EXE instead.
if [[ $version == "3.1.2" ]]; then
    add_update_file HARNESS98.EXE "$project_root/HARNESS98.EXE"
fi
add_update_file H98GUI.EXE "$project_root/H98GUI.EXE"
add_update_file H98CLI.EXE "$project_root/H98CLI.EXE"
add_update_file RUN3.BAT "$project_root/RUN3.BAT"
add_update_file RUNCLI.BAT "$project_root/RUNCLI.BAT"
add_update_file V31README.TXT "$project_root/V31README.TXT"
add_update_file LibCurlNet.dll "$project_root/vendor/libcurl-win98/LibCurlNet.dll"
add_update_file LibCurlShim.dll "$project_root/vendor/libcurl-win98/LibCurlShim.dll"
add_update_file libcurl.dll "$project_root/vendor/libcurl-win98/libcurl.dll"
add_update_file libeay32.dll "$project_root/vendor/libcurl-win98/libeay32.dll"
add_update_file ssleay32.dll "$project_root/vendor/libcurl-win98/ssleay32.dll"
add_update_file CACERT.PEM "$project_root/dist/openrouter-chat-v2/CACERT.PEM"

echo "Verifying manifest hashes..."
while IFS='=|' read -r kind name expected; do
    [[ $kind == FILE ]] || continue
    actual=$(sha256sum "$stable/$name" | cut -d' ' -f1)
    if [[ $actual != "$expected" ]]; then
        echo "Hash verification failed for $name." >&2
        exit 1
    fi
done < "$manifest"

if $publish; then
    publish_url=${HARNESS98_PUBLISH_URL:-smb://Sirfrummel@192.168.50.170/retro/to-transfer/}
    publish_root=${publish_url%/}
    command -v kioclient5 >/dev/null || {
        echo "kioclient5 is required for --publish." >&2
        exit 1
    }
    echo "Publishing stable channel and fallback package..."
    if kioclient5 ls "$publish_root/harness98-updates/" >/dev/null 2>&1; then
        kioclient5 --noninteractive rm "$publish_root/harness98-updates/"
    fi
    if kioclient5 ls "$publish_root/harness98-v$version/" >/dev/null 2>&1; then
        kioclient5 --noninteractive rm "$publish_root/harness98-v$version/"
    fi
    kioclient5 --noninteractive --overwrite copy \
        "file://$updates_parent" "$publish_root/"
    kioclient5 --noninteractive --overwrite copy \
        "file://$package" "$publish_root/"
fi

echo "Release $version is ready."
echo "Package: $package"
echo "Manifest: $manifest"
if ! $publish; then
    echo "Nothing was published. Re-run with --publish after review."
fi
