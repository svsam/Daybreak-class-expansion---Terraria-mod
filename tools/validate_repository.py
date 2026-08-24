#!/usr/bin/env python3
"""Validate release-critical repository content without loading the mod."""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
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
    ):
        if required_text not in build_text:
            fail(f"build.txt is missing {required_text!r}", failures)


def validate_history(failures: list[str]) -> None:
    history = git_output(
        "log", "--all", "--format=fuller", "-p", "--", ".", ":!*.png"
    )
    scan_sensitive_text(history, "reachable Git history", failures)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--history",
        action="store_true",
        help="also scan all reachable Git history (expected to fail before the planned rewrite)",
    )
    args = parser.parse_args()

    failures: list[str] = []
    validate_manifest(failures)
    validate_icons(failures)
    validate_tracked_text(failures)
    validate_release_files(failures)
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
