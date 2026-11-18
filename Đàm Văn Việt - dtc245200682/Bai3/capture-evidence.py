"""Ảnh chụp trang log thật bằng Edge headless; không mô phỏng kết quả chương trình."""
import html
import os
from pathlib import Path
import subprocess
import textwrap

root = Path(__file__).resolve().parent
browser = Path(os.environ.get('ProgramFiles(x86)', 'C:/Program Files (x86)')) / 'Microsoft/Edge/Application/msedge.exe'
if not browser.exists():
    raise SystemExit('Can Microsoft Edge de chup trang log; log TXT van xem truc tiep duoc.')
(root / 'anh').mkdir(exist_ok=True)
pages = root / '.artifacts/evidence-pages'
pages.mkdir(parents=True, exist_ok=True)
names = ['build-PingSignalR','build-RealtimeSubmissionSystem','buoc2-negotiate','buoc4-echo',
    'buoc5-An','buoc5-Binh','buoc6-noilai','buoc7-loi1','buoc7-loi2','buoc7-loi3',
    'edusubmit-requests','edusubmit-SV001','bai-tap2-3','thi-nghiem4-sai-ten','thi-nghiem5-watch-muon']
for name in names:
    raw = (root / 'evidence' / (name + '.txt')).read_text(encoding='utf-8')
    wrapped = '\n'.join(textwrap.fill(line, 112, subsequent_indent='    ',
        replace_whitespace=False, drop_whitespace=False) if line else '' for line in raw.splitlines())
    page = pages / (name + '.html')
    page.write_text('<!doctype html><html lang="vi"><meta charset="utf-8">'
        '<style>body{margin:0;padding:26px;background:#111827;color:#f3f4f6;font:17px Consolas,monospace}'
        'h1{font:22px Segoe UI;color:#a7f3d0}p{font:15px Segoe UI;color:#cbd5e1}pre{line-height:1.55;white-space:pre-wrap}</style>'
        '<h1>Bài 3 · Đàm Văn Việt · dtc245200682</h1>'
        '<p>Ảnh chụp trình duyệt hiển thị log chạy thật. Nguồn: evidence/' + html.escape(name) + '.txt</p>'
        '<pre>' + html.escape(wrapped) + '</pre></html>', encoding='utf-8')
    height = max(400, 165 + 27 * len(wrapped.splitlines()))
    command = [str(browser), '--headless=new', '--disable-gpu', '--no-first-run', '--hide-scrollbars',
        '--allow-file-access-from-files', '--user-data-dir=' + str(root / '.artifacts/edge-profile'),
        '--screenshot=' + str(root / 'anh' / (name + '.png')), '--window-size=1360,' + str(height), page.as_uri()]
    result = subprocess.run(command, capture_output=True, timeout=30)
    assert result.returncode == 0 and (root / 'anh' / (name + '.png')).exists(), result.stderr
    print('Captured ' + name, flush=True)
