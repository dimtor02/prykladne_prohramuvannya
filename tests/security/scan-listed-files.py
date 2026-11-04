"""Scan a user-exported tracked path list; never invokes Git or prints file content.
Usage: python tests/security/scan-listed-files.py PATH_LIST OUTPUT_JSON
Review each reported path/line in context before classifying it. A match is not a secret.
"""
import hashlib
import json
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[2]
names = Path(sys.argv[1]).read_text(encoding='utf-8-sig').splitlines()
pattern = re.compile(r'password|passwd|secret|api.?key', re.IGNORECASE)
result = {'pattern': pattern.pattern, 'listedFiles': len(names), 'textFiles': 0, 'skippedBinary': [], 'files': [], 'matches': []}
for name in names:
    path = (root / name).resolve()
    if not path.is_relative_to(root) or not path.is_file():
        raise ValueError('Missing or outside-project path: ' + name)
    raw = path.read_bytes()
    result['files'].append({'path': name, 'sha256': hashlib.sha256(raw).hexdigest()})
    if b'\0' in raw:
        result['skippedBinary'].append(name)
        continue
    try:
        text = raw.decode('utf-8-sig')
    except UnicodeDecodeError:
        result['skippedBinary'].append(name)
        continue
    result['textFiles'] += 1
    for line, value in enumerate(text.splitlines(), 1):
        if pattern.search(value):
            result['matches'].append({'path': name, 'line': line})
Path(sys.argv[2]).write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
print(f"Listed {result['listedFiles']}; text {result['textFiles']}; binary {len(result['skippedBinary'])}; matches {len(result['matches'])}. Review required.")
