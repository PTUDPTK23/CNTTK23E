"""Xuất đúng các file Git đang theo dõi sang cấu trúc bài nộp; không xuất bin/obj/cache."""
import hashlib
from pathlib import Path
import shutil
import subprocess
import sys
import zipfile

sys.stdout.reconfigure(encoding='utf-8')
root = Path(__file__).resolve().parent
prefix = Path('Đàm Văn Việt - dtc245200682') / 'Bai3'
destination = root / 'submission' / prefix
paths = subprocess.check_output(['git', 'ls-files', '-z'], cwd=root).decode('utf-8').split('\0')
copied = []
for name in filter(None, paths):
    path = Path(name)
    if any(part in ('bin', 'obj', '.git', '.artifacts', 'submission') for part in path.parts):
        raise RuntimeError('File khong duoc phep trong Git: ' + name)
    target = destination / path
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(root / path, target)
    copied.append(path)
archive = root / 'submission' / 'Bai3-dtc245200682.zip'
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as package:
    for path in copied:
        package.write(destination / path, (prefix / path).as_posix())
print('Prepared', len(copied), 'files at', destination)
print('ZIP:', archive)
print('SHA256:', hashlib.sha256(archive.read_bytes()).hexdigest())
