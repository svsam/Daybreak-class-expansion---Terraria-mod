#!/usr/bin/env python3
"""Validate release-critical repository content without loading the mod."""

from __future__ import annotations

import argparse
import re
import struct
import subprocess
import sys
import zlib
from pathlib import Path

import yaml
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
EXPECTED_ITEMS = (
    "GoldSpear",
    "NightsSpine",
    "BloodSpine",
    "Hellrend",
    "Mightpiercer",
    "GeminiGaze",
    "Frightsteel",
    "FlowerSpike",
    "Tepoztopilli",
    "MonarchsSpear",
)


class TmodFormatError(ValueError):
    """Raised when a .tmod file cannot be parsed safely."""


class TmodReader:
    def __init__(self, data: bytes) -> None:
        self.data = data
        self.offset = 0

    def read(self, length: int) -> bytes:
        if length < 0 or self.offset + length > len(self.data):
            raise TmodFormatError("unexpected end of file")
        result = self.data[self.offset : self.offset + length]
        self.offset += length
        return result

    def read_int32(self) -> int:
        return struct.unpack("<i", self.read(4))[0]

    def read_uint32(self) -> int:
        return struct.unpack("<I", self.read(4))[0]

    def read_7bit_int(self) -> int:
        value = 0
        for shift in range(0, 35, 7):
            current = self.read(1)[0]
            value |= (current & 0x7F) << shift
            if current & 0x80 == 0:
                return value
        raise TmodFormatError("invalid 7-bit encoded integer")

    def read_string(self) -> str:
        length = self.read_7bit_int()
        try:
            return self.read(length).decode("utf-8")
        except UnicodeDecodeError as exc:
            raise TmodFormatError(f"invalid UTF-8 string: {exc}") from exc


def read_tmod(path: Path) -> tuple[str, str, dict[str, bytes]]:
    reader = TmodReader(path.read_bytes())
    if reader.read(4) != b"TMOD":
        raise TmodFormatError("missing TMOD signature")

    reader.read_string()  # tModLoader build version
    reader.read(20)  # SHA-1 payload hash
    reader.read(256)  # signature slot
    reader.read_uint32()  # payload length
    mod_name = reader.read_string()
    mod_version = reader.read_string()
    entry_count = reader.read_int32()
    if entry_count < 0 or entry_count > 10_000:
        raise TmodFormatError(f"implausible entry count: {entry_count}")

    entry_table: list[tuple[str, int, int]] = []
    for _ in range(entry_count):
        name = reader.read_string().replace("\\", "/")
        raw_length = reader.read_int32()
        stored_length = reader.read_int32()
        if raw_length < 0 or stored_length < 0:
            raise TmodFormatError(f"negative length for {name!r}")
        entry_table.append((name, raw_length, stored_length))

    entries: dict[str, bytes] = {}
    for name, raw_length, stored_length in entry_table:
        if name in entries:
            raise TmodFormatError(f"duplicate entry: {name}")
        stored = reader.read(stored_length)
        try:
            payload = stored if stored_length == raw_length else zlib.decompress(stored, -zlib.MAX_WBITS)
        except zlib.error as exc:
            raise TmodFormatError(f"invalid DEFLATE payload for {name}: {exc}") from exc
        if len(payload) != raw_length:
            raise TmodFormatError(
                f"length mismatch for {name}: expected {raw_length}, got {len(payload)}"
            )
        entries[name] = payload

    if reader.offset != len(reader.data):
        raise TmodFormatError(f"{len(reader.data) - reader.offset} trailing byte(s)")
    return mod_name, mod_version, entries


def fail(message: str, failures: list[str]) -> None:
    failures.append(message)


def git_output(*args: str) -> str:
    result = subprocess.run(
        ["git", *args],
        cwd=ROOT,
        check=True,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    return result.stdout


def validate_manifest(failures: list[str]) -> None:
    manifest_path = ROOT / "DESIGN_MANIFEST.yaml"
    try:
        manifest = yaml.safe_load(manifest_path.read_text(encoding="utf-8"))
    except Exception as exc:  # pragma: no cover - reported as validation output
        fail(f"DESIGN_MANIFEST.yaml does not parse: {exc}", failures)
        return

    weapons = manifest.get("weapons") if isinstance(manifest, dict) else None
    if not isinstance(weapons, list):
        fail("DESIGN_MANIFEST.yaml must contain a weapons list", failures)
        return

    if manifest.get("status") != "approved_for_implementation":
        fail("DESIGN_MANIFEST.yaml is not approved for implementation", failures)
    if manifest.get("illustrated_weapon_count") != len(EXPECTED_ITEMS):
        fail("manifest illustrated_weapon_count must be 10", failures)

    names = tuple(weapon.get("internal_name") for weapon in weapons)
    if names != EXPECTED_ITEMS:
        fail(f"manifest weapon order/names differ: {names}", failures)

    for weapon in weapons:
        name = weapon.get("internal_name", "<unnamed>")
        balance = weapon.get("balance", {})
        if balance.get("use_animation_ticks") != balance.get("use_time_ticks"):
            fail(f"{name} useAnimation must equal useTime", failures)
        item_png = weapon.get("art", {}).get("item_png")
        if not isinstance(item_png, str) or not (ROOT / item_png).is_file():
            fail(f"{name} has a missing item_png: {item_png!r}", failures)

    def walk(value: object, location: str) -> None:
        if isinstance(value, dict):
            for key, child in value.items():
                walk(child, f"{location}.{key}")
        elif isinstance(value, list):
            for index, child in enumerate(value):
                walk(child, f"{location}[{index}]")
        elif isinstance(value, str) and "TODO" in value.upper():
            fail(f"unresolved TODO at {location}: {value}", failures)

    walk(manifest, "manifest")


def validate_icons(failures: list[str]) -> None:
    icon_dir = ROOT / "Content" / "Items" / "Weapons" / "Spears"
    actual = {path.stem for path in icon_dir.glob("*.png")}
    expected = set(EXPECTED_ITEMS)
    if actual != expected:
        fail(f"item icon set differs: expected {sorted(expected)}, got {sorted(actual)}", failures)

    for name in EXPECTED_ITEMS:
        path = icon_dir / f"{name}.png"
        if not path.is_file():
            continue
        with Image.open(path) as image:
            if image.size != (40, 40):
                fail(f"{path.relative_to(ROOT)} is {image.size}, expected 40x40", failures)
            if "A" not in image.getbands():
                fail(f"{path.relative_to(ROOT)} has no alpha channel", failures)
                continue
            alpha_min, alpha_max = image.getchannel("A").getextrema()
            if alpha_min != 0 or alpha_max == 0:
                fail(
                    f"{path.relative_to(ROOT)} must contain transparent and visible pixels",
                    failures,
                )


def scan_sensitive_text(text: str, label: str, failures: list[str]) -> None:
    checks = (
        (r"[A-Za-z]:[\\/]+Users[\\/]+(?!runneradmin(?:[\\/]|$))", "absolute user path"),
        (r"[A-Za-z]:[\\/]+SteamLibrary[\\/]", "absolute Steam-library path"),
        (r"-----BEGIN (?:RSA |EC |DSA |OPENSSH )?PRIVATE KEY-----", "private key"),
        (r"\bAKIA[0-9A-Z]{16}\b", "AWS access key"),
        (r"\bgh[pousr]_[A-Za-z0-9]{30,}\b", "GitHub token"),
        (r"\bsk-(?:proj-)?[A-Za-z0-9_-]{24,}\b", "API token"),
    )
    for pattern, description in checks:
        if re.search(pattern, text, flags=re.IGNORECASE):
            fail(f"{label} contains a possible {description}", failures)

    emails = {
        match.group(0).lower()
        for match in re.finditer(
            r"\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b",
            text,
            flags=re.IGNORECASE,
        )
    }
    personal_emails = sorted(
        email
        for email in emails
        if email != "noreply@github.com"
        and not email.endswith("@users.noreply.github.com")
    )
    if personal_emails:
        fail(f"{label} contains non-noreply email address(es): {personal_emails}", failures)


def validate_tracked_text(failures: list[str]) -> None:
    binary_suffixes = {".png", ".dll", ".pdb", ".tmod"}
    for relative in git_output(
        "ls-files", "--cached", "--others", "--exclude-standard"
    ).splitlines():
        path = ROOT / relative
        if not path.is_file() or path.suffix.lower() in binary_suffixes:
            continue
        scan_sensitive_text(
            path.read_text(encoding="utf-8", errors="replace"),
            relative,
            failures,
        )


def validate_tmod(path: Path, failures: list[str]) -> None:
    if not path.is_file():
        fail(f"missing .tmod package: {path}", failures)
        return

    try:
        mod_name, _, entries = read_tmod(path)
    except (OSError, TmodFormatError) as exc:
        fail(f"cannot inspect {path}: {exc}", failures)
        return

    if mod_name != "Spears":
        fail(f"package mod name is {mod_name!r}, expected 'Spears'", failures)

    forbidden_exact = {
        "DESIGN_MANIFEST.yaml",
        "ASSET_PROVENANCE.md",
        "Spears.csproj",
    }
    forbidden_prefixes = (
        "spearsart/",
        "tools/",
        "Properties/",
        "bin/",
        "obj/",
        "artifacts/",
        "TestResults/",
    )
    forbidden_suffixes = (".pdb", ".tmod", ".binlog", ".trx")

    for name, payload in entries.items():
        normalized = name.replace("\\", "/")
        if (
            normalized in forbidden_exact
            or normalized.startswith(forbidden_prefixes)
            or normalized.lower().endswith(forbidden_suffixes)
        ):
            fail(f"package contains forbidden entry: {normalized}", failures)

        text = payload.decode("latin-1", errors="replace")
        scan_sensitive_text(text, f"package entry {normalized}", failures)

    # tModLoader 2026.06 serializes modSource/eacPath and copies the .tmod
    # unchanged when publishing, even when DebugType=None prevents the PDB
    # itself from being packaged. The package-wide sensitive-data scan above
    # therefore enforces a neutral staging root without rejecting those keys.

    for required in ("Spears.dll", "Info", "LICENSE.txt", "THIRD_PARTY_NOTICES.txt"):
        if required not in entries:
            fail(f"package is missing required entry: {required}", failures)


def validate_release_files(failures: list[str]) -> None:
    required = (
        "LICENSE.txt",
        "THIRD_PARTY_NOTICES.txt",
        "ASSET_PROVENANCE.md",
        "README.md",
    )
    for relative in required:
        if not (ROOT / relative).is_file():
            fail(f"missing release file: {relative}", failures)

    for relative in ("Content/Items/Spear1.cs", "Content/Items/Spear1.png"):
        if (ROOT / relative).exists():
            fail(f"template content still exists: {relative}", failures)

    build_text = (ROOT / "build.txt").read_text(encoding="utf-8")
    for required_text in (
        "side = Both",
        "includeSource = true",
        "spearsart/*",
        "DESIGN_MANIFEST.yaml",
        "ASSET_PROVENANCE.md",
        "Properties/*",
        "tools/*",
        "Spears.csproj",
    ):
        if required_text not in build_text:
            fail(f"build.txt is missing {required_text!r}", failures)


def validate_visual_effect_contract(failures: list[str]) -> None:
    helper_path = ROOT / "Content" / "Common" / "SpearVisualEffects.cs"
    helper_text = helper_path.read_text(encoding="utf-8")
    if "projectile.numUpdates == -1" not in helper_text:
        fail("visual effects must be emitted only on the final projectile update", failures)

    for path in (ROOT / "Content").rglob("*.cs"):
        text = path.read_text(encoding="utf-8")
        relative = path.relative_to(ROOT).as_posix()
        if path != helper_path and "Lighting.AddLight(" in text:
            fail(f"{relative} bypasses the shared spear light budget", failures)
        if "DustID.TintableDustLighted" in text:
            fail(f"{relative} uses persistent light-emitting dust", failures)


def validate_history(failures: list[str]) -> None:
    history = git_output(
        "log", "--all", "--format=fuller", "-p", "--", ".", ":!*.png"
    )
    scan_sensitive_text(history, "reachable Git history", failures)

    historical_paths = git_output("log", "--all", "--format=", "--name-only")
    if re.search(r"(?:^|/)(?:bin|obj)/", historical_paths, flags=re.MULTILINE):
        fail("reachable Git history contains bin/ or obj/ content", failures)

    git_directory = Path(git_output("rev-parse", "--git-dir").strip())
    if not git_directory.is_absolute():
        git_directory = ROOT / git_directory
    logs_directory = git_directory / "logs"
    if logs_directory.is_dir():
        for log_path in logs_directory.rglob("*"):
            if log_path.is_file():
                scan_sensitive_text(
                    log_path.read_text(encoding="utf-8", errors="replace"),
                    f"Git reflog {log_path.relative_to(git_directory)}",
                    failures,
                )

    fsck = subprocess.run(
        ["git", "fsck", "--full", "--no-reflogs", "--unreachable"],
        cwd=ROOT,
        check=False,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    unreachable = "\n".join(part for part in (fsck.stdout, fsck.stderr) if part).strip()
    if fsck.returncode != 0:
        fail(f"git fsck failed: {unreachable}", failures)
    elif unreachable:
        fail("Git object database still contains unreachable objects", failures)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--history",
        action="store_true",
        help="also scan all reachable Git history (expected to fail before the planned rewrite)",
    )
    parser.add_argument(
        "--tmod",
        type=Path,
        help="also parse and inspect the specified built .tmod package",
    )
    args = parser.parse_args()

    failures: list[str] = []
    validate_manifest(failures)
    validate_icons(failures)
    validate_tracked_text(failures)
    validate_release_files(failures)
    validate_visual_effect_contract(failures)
    if args.tmod:
        package_path = args.tmod if args.tmod.is_absolute() else ROOT / args.tmod
        validate_tmod(package_path.resolve(), failures)
    if args.history:
        validate_history(failures)

    if failures:
        print("Repository validation failed:", file=sys.stderr)
        for failure in failures:
            print(f"- {failure}", file=sys.stderr)
        return 1

    print("Repository validation passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
