"""Verify curated catalog integrity and input identity; this is not a gameplay test."""
import argparse
import collections
import hashlib
import importlib.util
import json
import math
from pathlib import Path


def check(value, message):
    if not value:
        raise ValueError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--game-dir', type=Path, required=True)
    parser.add_argument('--dump-dir', type=Path)
    parser.add_argument('--localization-json', type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    evidence = json.loads((root / 'data/expanded-evidence.json').read_text('utf-8'))
    data = json.loads((root / 'data/numeric-details.json').read_text('utf-8'))
    coverage = json.loads((root / 'data/coverage.json').read_text('utf-8'))
    for item in evidence['gameFiles']:
        with (args.game_dir / item['path']).open('rb') as source:
            check(hashlib.file_digest(source, 'sha256').hexdigest() == item['sha256'], 'Changed game input: ' + item['path'])
    spec = importlib.util.spec_from_file_location('first_pass', root / 'scripts/verify-first-pass.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    binary = (args.game_dir / 'GameAssembly.dll').read_bytes()
    for item in evidence['floatConstants']:
        value = module.read_float(binary, int(item['address'], 16))
        check(math.isclose(value, item['expected'], rel_tol=1e-6, abs_tol=1e-10), 'Changed native literal: ' + item['address'])
    if args.dump_dir:
        for item in evidence['nativeDumps']:
            check(hashlib.sha256((args.dump_dir / item['file']).read_bytes()).hexdigest() == item['sha256'], 'Changed dump: ' + item['file'])
    keys = set()
    terms = set()
    entries = {}
    for row in data['entries']:
        key = (row['kind'], row['id'], row['severity'])
        check(key not in keys, 'Duplicate typed key: ' + str(key))
        keys.add(key)
        entries[key] = row
        check(row['evidence'] and row['details'] and all(row['details']), 'Unsourced/empty card: ' + str(key))
        check(len('\n'.join(row['details'])) < 1000, 'Card needs shortening: ' + str(key))
        for term in row['terms']:
            check(term not in terms and '_Name' not in term and '_Old' not in term, 'Duplicate or non-active description term: ' + term)
            terms.add(term)
    counts = dict(collections.Counter(row['kind'] for row in data['entries']))
    check(counts == coverage['cardCounts'], 'Coverage totals disagree with embedded data')
    inventory_keys = set()
    for item in coverage['descriptions']:
        key = (item['kind'], item['id'], item['severity'])
        check(key not in inventory_keys, 'Duplicate coverage row: ' + str(key))
        inventory_keys.add(key)
        check((item['state'] == 'card') == (key in keys), 'Coverage classification disagrees: ' + str(key))
        if key in entries:
            check(item['term'] in entries[key]['terms'], 'Coverage term disagrees: ' + item['term'])
        check(item['state'] != 'review', 'Unclassified description: ' + item['term'])
    check(keys <= inventory_keys, 'Card missing from coverage inventory')
    if args.localization_json:
        source = json.loads(args.localization_json.read_text('utf-8'))['m_Structure']['mSource']
        known = {term['Term'] for term in source['mTerms']}
        check(terms <= known, 'Unknown localization keys: ' + repr(terms - known))
    # Independently checked high-risk distinctions, not generic snapshots.
    check('32%' in ' '.join(entries['injury', 901, 2]['details']), 'Pickup disarm severity 2 must be 32%')
    check('+75%' in ' '.join(entries['injury', 510, 3]['details']), 'Melee stamina drawback is 25% per severity')
    check('12' in ' '.join(entries['purchase', 3008, 0]['details']), 'Fired arrow differs from thrown bow')
    check('{backstab}' in ' '.join(entries['status', 8, 0]['details']), 'Backstab must read the setting-dependent getter')
    print(json.dumps({'status': 'passed', 'cards': counts, 'totalCards': len(keys),
                      'coverageRows': len(inventory_keys), 'gameHashes': len(evidence['gameFiles']),
                      'nativeConstants': len(evidence['floatConstants']),
                      'nativeDumps': len(evidence['nativeDumps']) if args.dump_dir else 'not requested',
                      'descriptionKeysVerified': bool(args.localization_json), 'gameplayTested': False}, indent=2))


if __name__ == '__main__':
    main()
