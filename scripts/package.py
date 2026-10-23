"""Validate the DalamudPackager output and create a versioned custom-repository release."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import zipfile


def package(build: Path, output: Path, repository: str, tag: str) -> None:
    manifest = json.loads((build / "BakaPlugin.json").read_text(encoding="utf-8-sig"))
    version = manifest["AssemblyVersion"]
    if tag != f"v{version}":
        raise ValueError(f"Tag {tag} does not match built assembly version {version}")
    if manifest["InternalName"] != "BakaPlugin" or manifest["DalamudApiLevel"] != 15:
        raise ValueError("Unexpected plugin identity or API level")
    source_zip = build / "BakaPlugin" / "latest.zip"
    with zipfile.ZipFile(source_zip) as archive:
        if archive.testzip() is not None:
            raise ValueError("Corrupt plugin archive")
        names = set(archive.namelist())
        if not {"BakaPlugin.dll", "BakaPlugin.json"}.issubset(names):
            raise ValueError(f"Missing root-level plugin files: {names}")
        packaged_manifest = json.loads(archive.read("BakaPlugin.json"))
        if packaged_manifest["AssemblyVersion"] != version:
            raise ValueError("Packaged manifest version differs from build")
        if archive.read("BakaPlugin.dll") != (build / "BakaPlugin.dll").read_bytes():
            raise ValueError("Packaged DLL differs from build")
    output.mkdir(parents=True, exist_ok=True)
    target = output / "BakaPlugin.zip"
    shutil.copyfile(source_zip, target)
    url = f"https://github.com/{repository}/releases/download/{tag}/BakaPlugin.zip"
    manifest.update(DownloadLinkInstall=url, DownloadLinkUpdate=url, DownloadLinkTesting=url, IsHide=False)
    (output / "repo.json").write_text(json.dumps([manifest], indent=2) + "\n", encoding="utf-8")
    digest = hashlib.sha256(target.read_bytes()).hexdigest()
    (output / "SHA256SUMS").write_text(f"{digest}  BakaPlugin.zip\n", encoding="utf-8")
    print(f"Validated {manifest['Name']} {version}, API {manifest['DalamudApiLevel']}: {target}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--build", type=Path, default=Path("bin/Release"))
    parser.add_argument("--output", type=Path, default=Path("dist"))
    parser.add_argument("--repository", default="cloudesh/baka_plugin")
    parser.add_argument("--tag", required=True)
    args = parser.parse_args()
    package(args.build, args.output, args.repository, args.tag)
