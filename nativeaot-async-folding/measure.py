#!/usr/bin/env python3
"""For each N, generates N async methods and publishes the sample with method body folding on (the .NET 10 Release
default) and off, printing the publish wall time, the executable size and its output.

    python3 measure.py [N ...] [--rid RID] [--ilc-tools-path PATH]

--ilc-tools-path points to a custom ILC, e.g. <runtime>/artifacts/bin/coreclr/<rid>.Release/ilc-published/.
"""
import argparse
import os
import shutil
import subprocess
import sys
import time

ROOT = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIR = os.path.join(ROOT, "AsyncFolding")
PROJECT = os.path.join(PROJECT_DIR, "AsyncFolding.csproj")


def host_rid():
    info = subprocess.run(["dotnet", "--info"], capture_output=True, text=True, check=True).stdout
    for line in info.splitlines():
        if line.strip().startswith("RID:"):
            return line.split(":", 1)[1].strip()
    sys.exit("could not determine the RID from 'dotnet --info', pass --rid")


def publish(label, rid, extra_args):
    for d in ("bin", "obj"):
        shutil.rmtree(os.path.join(PROJECT_DIR, d), ignore_errors=True)
    subprocess.run(["dotnet", "restore", PROJECT, "-r", rid, "-v:q"], check=True, stdout=subprocess.DEVNULL)

    log_path = os.path.join(ROOT, f"publish-{label.replace(' ', '_').replace('=', '-')}.log")
    start = time.monotonic()
    with open(log_path, "w") as log:
        result = subprocess.run(
            ["dotnet", "publish", PROJECT, "-c", "Release", "-r", rid, "--no-restore", "-v:q", "-nologo", *extra_args],
            stdout=log, stderr=subprocess.STDOUT)
    elapsed = time.monotonic() - start
    if result.returncode != 0:
        sys.exit(f"{label}: publish failed, see {log_path}")

    exe = os.path.join(PROJECT_DIR, "bin", "Release", "net10.0", rid, "publish",
                       "AsyncFolding.exe" if rid.startswith("win") else "AsyncFolding")
    output = subprocess.run([exe], capture_output=True, text=True, check=True).stdout.strip()
    print(f"{label:<22} {elapsed:6.0f}s  {os.path.getsize(exe):>10} bytes  {output}", flush=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("counts", nargs="*", type=int, default=[4000], help="numbers of async methods to measure")
    parser.add_argument("--rid", default=None, help="runtime identifier (default: the host RID)")
    parser.add_argument("--ilc-tools-path", default=None, help="directory of a custom ILC to use")
    args = parser.parse_args()

    rid = args.rid or host_rid()
    extra = [f"-p:IlcToolsPath={args.ilc_tools_path}"] if args.ilc_tools_path else []

    for n in args.counts:
        subprocess.run([sys.executable, os.path.join(ROOT, "generate_async.py"), "-n", str(n)],
                       check=True, stdout=subprocess.DEVNULL)
        publish(f"n={n} fold=default", rid, extra)
        publish(f"n={n} fold=false", rid, extra + ["-p:IlcFoldIdenticalMethodBodies=false"])


if __name__ == "__main__":
    main()
