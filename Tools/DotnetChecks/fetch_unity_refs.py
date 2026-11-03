#!/usr/bin/env python3
"""Download Unity reference assemblies from NuGet into Tools/DotnetChecks/.unity-refs (gitignored)."""
import io
import shutil
import urllib.request
import zipfile
from pathlib import Path

DEST = Path(__file__).resolve().parent / ".unity-refs"
PACKAGES = {
    # (package id, version, folder inside the package, destination subfolder)
    "engine": ("unityengine.modules", "2021.3.33", "lib/netstandard2.0/", "engine"),
    "editor": ("unity3d.sdk", "2021.1.14.1", "lib/UnityEditor.dll", "editor"),
}


def main() -> None:
    for pkg_id, version, inner, sub in PACKAGES.values():
        out = DEST / sub
        if out.exists() and any(out.iterdir()):
            print(f"{sub}: already present")
            continue
        url = f"https://api.nuget.org/v3-flatcontainer/{pkg_id}/{version}/{pkg_id}.{version}.nupkg"
        print(f"{sub}: downloading {url}")
        data = urllib.request.urlopen(url, timeout=120).read()
        out.mkdir(parents=True, exist_ok=True)
        with zipfile.ZipFile(io.BytesIO(data)) as zf:
            for name in zf.namelist():
                if name.endswith(".dll") and (name.startswith(inner) if inner.endswith("/") else name == inner):
                    with zf.open(name) as src, open(out / Path(name).name, "wb") as dst:
                        shutil.copyfileobj(src, dst)
        print(f"{sub}: {len(list(out.glob('*.dll')))} assemblies")


if __name__ == "__main__":
    main()
