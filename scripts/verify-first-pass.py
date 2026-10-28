"""Read-only checks for the recorded research build; Python standard library only.

This checks input identity and literal values, not gameplay or native-code semantics.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import struct


def sha256(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def read_float(binary, address):
    pe = struct.unpack_from("<I", binary, 0x3C)[0]
    require(binary[pe:pe + 4] == b"PE\0\0", "Not a PE binary")
    optional = pe + 24
    require(struct.unpack_from("<H", binary, optional)[0] == 0x20B,
            "Expected a PE32+ binary")
    image_base = struct.unpack_from("<Q", binary, optional + 24)[0]
    rva = address - image_base
    sections = optional + struct.unpack_from("<H", binary, pe + 20)[0]
    for index in range(struct.unpack_from("<H", binary, pe + 6)[0]):
        virtual_size, start, raw_size, raw = struct.unpack_from(
            "<IIII", binary, sections + index * 40 + 8)
        if start <= rva and rva + 4 <= start + min(virtual_size, raw_size):
            return struct.unpack_from("<f", binary, raw + rva - start)[0]
    raise ValueError(f"Address {address:#x} has no file-backed float")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game-dir", required=True, type=Path)
    parser.add_argument("--dump-dir", type=Path)
    parser.add_argument("--localization-json", type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    evidence = json.loads((root / "data/first-pass-evidence.json").read_text("utf-8"))
    descriptions = json.loads((root / "data/first-pass-descriptions.json").read_text("utf-8"))

    # Refuse stale addresses before reading any values from a different build.
    for record in evidence["gameFiles"]:
        path = args.game_dir / record["path"]
        require(sha256(path) == record["sha256"], f"Game build mismatch: {path}")

    binary = (args.game_dir / "GameAssembly.dll").read_bytes()
    values = {}
    for record in evidence["floatConstants"]:
        value = read_float(binary, int(record["address"], 16))
        require(math.isclose(value, record["expected"], rel_tol=1e-6),
                f"Unexpected constant: {record['name']}")
        values[record["name"]] = value

    if args.dump_dir:
        for record in evidence["nativeDumps"]:
            path = args.dump_dir / record["file"]
            require(sha256(path) == record["sha256"], f"Dump mismatch: {path}")

    require(len(descriptions["abilities"]) == 3, "Expected three sample descriptions")
    keys = [row["term"] for row in descriptions["abilities"]]
    require(len(set(keys)) == len(keys), "Duplicate description key")
    by_id = {row["skillType"]: row["values"] for row in descriptions["abilities"]}
    bindings = [
        (0, "attackDamageMultiplier", "Lion damage multiplier"),
        (0, "movementSpeedMultiplier", "Lion speed and Bear damage multiplier"),
        (0, "durationSeconds", "Lion buff duration seconds"),
        (3, "directImpactDamageMultiplier", "Power Throw direct-impact multiplier"),
        (3, "readyDurationSeconds", "Eagle ready duration seconds"),
        (7, "baseHealthMultiplier", "Bear base-health multiplier"),
        (7, "movementSpeedMultiplier", "Bear movement multiplier"),
        (7, "bluntWeaponDamageMultiplier", "Lion speed and Bear damage multiplier"),
        (7, "minimumBaseStunSeconds", "Bear minimum base stun seconds"),
    ]
    for skill, field, constant in bindings:
        require(math.isclose(by_id[skill][field], values[constant], rel_tol=1e-6),
                f"Description value disagrees with binary: {skill}/{field}")
    if args.localization_json:
        source = json.loads(args.localization_json.read_text("utf-8"))["m_Structure"]["mSource"]
        english = next(i for i, item in enumerate(source["mLanguages"]) if item["Code"] == "en")
        terms = {item["Term"]: item["Languages"][english] for item in source["mTerms"]}
        for row in descriptions["abilities"]:
            require(terms[row["term"]] == row["original"], f"Source text mismatch: {row['term']}")

    print(json.dumps({"status": "passed", "steamBuildId": evidence["steamBuildId"],
                      "gameHashesChecked": len(evidence["gameFiles"]),
                      "constants": values, "sampleDescriptions": len(keys),
                      "nativeDumpsChecked": bool(args.dump_dir),
                      "sourceDescriptionsChecked": bool(args.localization_json),
                      "gameplayTested": False}, indent=2))


if __name__ == "__main__":
    main()
