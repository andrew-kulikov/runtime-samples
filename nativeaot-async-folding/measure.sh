#!/usr/bin/env bash
# measure.sh [N...]: for each N, generates N async methods and publishes the sample with method body folding
# on (the .NET 10 Release default) and off. ILC_TOOLS_PATH points to a custom ILC (e.g. a local runtime build).
set -euo pipefail
cd "$(dirname "$0")"
RID="${RID:-$(dotnet --info | awk '/^ RID:/ {print $2; exit}')}"
extra=()
[ -n "${ILC_TOOLS_PATH:-}" ] && extra+=("-p:IlcToolsPath=$ILC_TOOLS_PATH")

publish() {
    local label="$1"; shift
    rm -rf AsyncFolding/bin AsyncFolding/obj
    dotnet restore AsyncFolding/AsyncFolding.csproj -r "$RID" -v:q > /dev/null
    local start end
    start=$(date +%s)
    dotnet publish AsyncFolding/AsyncFolding.csproj -c Release -r "$RID" --no-restore -v:q -nologo ${extra[@]+"${extra[@]}"} "$@" > "publish-$label.log" 2>&1
    end=$(date +%s)
    local exe="AsyncFolding/bin/Release/net10.0/$RID/publish/AsyncFolding"
    printf '%-22s %5ss  %10s bytes  %s\n' "$label" "$((end - start))" "$(stat -f %z "$exe" 2>/dev/null || stat -c %s "$exe")" "$("$exe")"
}

for n in "${@:-4000}"; do
    python3 ./generate_async.py -n "$n" > /dev/null
    publish "n=$n fold=default"
    publish "n=$n fold=false" -p:IlcFoldIdenticalMethodBodies=false
done
