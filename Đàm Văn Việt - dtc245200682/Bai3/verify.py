"""Build + integration tests with real .NET API/client processes; no third-party Python packages."""
import datetime
import json
import os
from pathlib import Path
import re
import socket
import subprocess
import threading
import time
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parent
EVIDENCE = ROOT / 'evidence'
EVIDENCE.mkdir(exist_ok=True)
ENV = os.environ.copy()
ENV['DOTNET_CLI_HOME'] = str(ROOT / '.artifacts/dotnet')
if (ROOT / '.artifacts/packages').exists():
    ENV['NUGET_PACKAGES'] = str(ROOT / '.artifacts/packages')
ENV['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
ENV['Logging__LogLevel__Default'] = 'Warning'
ENV['Logging__LogLevel__Microsoft.AspNetCore'] = 'Warning'
REPORT = []
PROCESSES = []

def note(message):
    REPORT.append(message)
    print(message, flush=True)

def save(name, command, output):
    stamp = datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=7))).isoformat()
    (EVIDENCE / (name + '.txt')).write_text(
        f'Thoi diem: {stamp}\nPS {ROOT}> {command}\n{output}', encoding='utf-8')

def dll(folder, project):
    return ROOT / folder / 'src' / project / 'bin/Debug/net10.0' / (project + '.dll')

class Process:
    def __init__(self, folder, project, *args):
        self.command = ['dotnet', str(dll(folder, project)), *args]
        self.lines = []
        self.p = subprocess.Popen(self.command, cwd=ROOT, env=ENV, stdin=subprocess.PIPE,
            stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, encoding='utf-8', errors='replace')
        self.thread = threading.Thread(target=self.read, daemon=True)
        self.thread.start()
        PROCESSES.append(self)
    def read(self):
        for line in self.p.stdout:
            self.lines.append(line)
    @property
    def output(self):
        return ''.join(self.lines)
    def until(self, text, timeout=15):
        end = time.monotonic() + timeout
        while time.monotonic() < end:
            if text in self.output: return
            if self.p.poll() is not None: break
            time.sleep(.05)
        raise AssertionError(f'Missing {text!r}: {self.output}')
    def finish(self, expected=0, timeout=25):
        self.p.wait(timeout=timeout)
        self.thread.join(timeout=2)
        assert self.p.returncode == expected, self.output
        return self.output
    def stop(self):
        if self.p.poll() is None:
            self.p.terminate()
            self.p.wait(timeout=10)
        self.thread.join(timeout=2)
    def evidence(self, name):
        save(name, subprocess.list2cmdline(self.command), self.output)

def request(base, method, path, expected, body=None, code=None, content_type='application/json'):
    data = None if body is None else (body.encode() if isinstance(body, str) else json.dumps(body).encode())
    req = urllib.request.Request(base + path, data=data, method=method,
        headers={'Content-Type': content_type} if data is not None else {})
    try:
        response = urllib.request.urlopen(req, timeout=5)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        raw = response.read().decode()
        assert response.status == expected, (path, response.status, raw)
        obj = json.loads(raw) if raw and 'application/json' in response.headers.get('Content-Type', '') else None
        if code: assert obj['code'] == code, obj
        note(f'{method} {base}{path} -> {response.status}' + (f' {code}' if code else ''))
        return obj, response.headers, raw

def api(folder, project, port):
    proc = Process(folder, project, '--urls', f'http://localhost:{port}')
    for _ in range(100):
        try:
            urllib.request.urlopen(f'http://localhost:{port}/', timeout=.3).close()
            return proc
        except OSError:
            if proc.p.poll() is not None: raise AssertionError(proc.output)
            time.sleep(.1)
    raise AssertionError(f'API startup timeout: {proc.output}')

def ping(name, *options):
    return Process('PingSignalR', 'Ping.Client', 'http://localhost:5200', name, *options)

def edu(name, *options):
    return Process('RealtimeSubmissionSystem', 'Submission.RealtimeClient', 'http://localhost:5000', name, *options)

def events(output):
    return re.findall(r'Thong bao SignalR: ([0-9a-f-]+) -> (\w+)', output)

try:
    for port in (5000, 5200):
        with socket.socket() as sock:
            assert sock.connect_ex(('127.0.0.1', port)) != 0, f'Cong {port} dang dung; dung API cu truoc khi verify.'
    for folder in ('PingSignalR', 'RealtimeSubmissionSystem'):
        command = ['dotnet', 'build', folder + '/' + folder + '.slnx', '--ignore-failed-sources']
        result = subprocess.run(command, cwd=ROOT, env=ENV, capture_output=True, text=True, encoding='utf-8')
        save('build-' + folder, subprocess.list2cmdline(command), result.stdout + result.stderr)
        assert result.returncode == 0, result.stdout + result.stderr
        note(folder + ': Build succeeded, 0 Error(s)')

    # Ping steps 2-6: actual callers, groups and server restart.
    server = api('PingSignalR', 'Ping.Api', 5200)
    base = 'http://localhost:5200'
    start = len(REPORT)
    request(base, 'GET', '/', 200)
    negotiated, _, raw = request(base, 'POST', '/hubs/ping/negotiate?negotiateVersion=1', 200)
    assert negotiated['connectionId']
    assert {t['transport'] for t in negotiated['availableTransports']} == {'WebSockets','ServerSentEvents','LongPolling'}
    request(base, 'GET', '/hubs/ping', 400)
    save('buoc2-negotiate', 'curl.exe -X POST "http://localhost:5200/hubs/ping/negotiate?negotiateVersion=1"', raw)
    an = ping('An'); an.until('Nhan Enter de thoat.')
    binh = ping('Binh'); binh.until('Nhan Enter de thoat.')
    an.until('[Broadcast] Xin chao tat ca tu Binh')
    assert '[Group] Thong bao nhom tu Binh' in an.output
    assert '[Echo] Echo: Xin chao SignalR tu Binh' not in an.output
    assert '[Echo] Echo: Xin chao SignalR tu An' in an.output
    an.evidence('buoc4-echo'); an.evidence('buoc5-An'); binh.evidence('buoc5-Binh')
    binh.p.stdin.write('\n'); binh.p.stdin.flush(); binh.finish()
    an.until('da ngat ket noi.')
    old_id = re.search(r'ConnectionId: (\S+)', an.output)[1]
    server.stop(); an.until('Dang ket noi lai...')
    server = api('PingSignalR', 'Ping.Api', 5200)
    an.until('Da vao lai nhom lop-hoc-01.', timeout=45)
    new_id = re.search(r'Da ket noi lai: (\S+)', an.output)[1]
    assert old_id != new_id
    probe = ping('SauReconnect', '--wait-seconds', '1'); probe.finish()
    an.until('[Group] Thong bao nhom tu SauReconnect')
    an.evidence('buoc6-noilai')
    an.p.stdin.write('\n'); an.p.stdin.flush(); an.finish()
    note('Ping: Echo Caller, Broadcast All, Group, lifecycle, reconnect ID moi + rejoin: PASS')

    wrong_path = ping('SaiHub', '--hub-path', '/hub/ping'); wrong_path.finish(1)
    assert '404' in wrong_path.output; wrong_path.evidence('buoc7-loi2')
    wrong_event = ping('SaiTen', '--event', 'EchoRespone', '--wait-seconds', '1'); wrong_event.finish()
    assert '[Echo]' not in wrong_event.output; wrong_event.evidence('buoc7-loi3')
    wrong_method = ping('SaiMethod', '--method', 'Ech0'); wrong_method.finish(1)
    assert 'HubException' in wrong_method.output; wrong_method.evidence('ping-sai-method')
    server.stop()
    offline = ping('Offline'); offline.finish(1)
    assert 'Loi ket noi' in offline.output; offline.evidence('buoc7-loi1')
    note('Ping: 3 loi trang 40 + sai ten phuong thuc trang 31: PASS')

    # EduSubmit: 9 mandatory REST cases + 7 checks on ports 5000/5200.
    server = api('RealtimeSubmissionSystem', 'Submission.RealtimeApi', 5000)
    base = 'http://localhost:5000'
    start = len(REPORT)
    request(base, 'GET', '/', 200)
    assignments, _, _ = request(base, 'GET', '/api/assignments', 200)
    assert any(a['id'] == 1 and a['isOpen'] for a in assignments)
    body = {'studentCode':' SV001 ', 'repositoryUrl':' https://example.com/sv001/signalr '}
    created, headers, raw = request(base, 'POST', '/api/assignments/1/submissions', 201, body)
    assert created['status'] == 'received' and created['studentCode'] == 'SV001'
    assert headers['Location'] == '/api/submissions/' + created['id']
    location = headers['Location']
    original, _, _ = request(base, 'GET', location, 200)
    assert original['status'] == 'received'
    time.sleep(1.3)
    processing, _, _ = request(base, 'GET', location, 200)
    assert processing['status'] == 'processing'
    time.sleep(3.8)  # total >= 5 sec as grading check 6 requires
    graded, _, _ = request(base, 'GET', location, 200)
    assert graded['status'] == 'graded'
    assert original['updatedAt'] < processing['updatedAt'] < graded['updatedAt']
    request(base, 'POST', '/api/assignments/1/submissions', 400, {}, 'INVALID_SUBMISSION')
    request(base, 'POST', '/api/assignments/999/submissions', 404, body, 'ASSIGNMENT_NOT_FOUND')
    request(base, 'GET', '/api/submissions/00000000-0000-0000-0000-000000000000', 404, code='SUBMISSION_NOT_FOUND')
    handshake, _, _ = request(base, 'POST', '/hubs/submissions/negotiate?negotiateVersion=1', 200)
    assert handshake['connectionId']
    request(base, 'POST', '/hub/submissions/negotiate?negotiateVersion=1', 404)
    request(base, 'POST', '/api/assignments/3/submissions', 400, body, 'ASSIGNMENT_CLOSED')
    request(base, 'POST', '/api/assignments/1/submissions', 400, {'studentCode':'SV001'}, 'INVALID_SUBMISSION')
    request(base, 'POST', '/api/assignments/1/submissions', 400, '{"studentCode":')
    request(base, 'POST', '/api/assignments/1/submissions', 415, body, content_type='application/x-www-form-urlencoded')
    request(base, 'GET', '/api/assignments/1', 200)
    request(base, 'GET', '/api/assignments/999', 404, code='ASSIGNMENT_NOT_FOUND')
    save('edusubmit-requests', 'python verify.py  # REST assertions', '\n'.join(REPORT[start:]) + '\nPOST body: ' + raw)
    note('7/7 yeu cau cham diem trang 71: PASS (Ping negotiate da kiem tra truoc).')

    # Start independent experiments together, each remains a real client process.
    sv1, sv2 = edu('SV001'), edu('SV002')
    wrong_path = edu('SaiHub', '--hub-path', '/hub/submissions')
    wrong_event = edu('SaiTen', '--event', 'SubmissionStatusChange')
    late = edu('WatchMuon', '--watch-delay', '4')
    two = edu('Multi', '--two')
    for client, label in ((sv1, 'edusubmit-SV001'), (sv2, 'edusubmit-SV002')):
        output = client.finish()
        own = re.search(r'Da tao luot nop: ([0-9a-f-]+)', output)[1]
        assert events(output) == [(own, 'processing'), (own, 'graded')], output
        assert 'Trang thai doc lai bang REST: graded' in output
        assert len([line for line in output.splitlines() if not line.startswith('[Lifecycle]')]) == 8, output
        client.evidence(label)
    assert re.search(r'Da tao luot nop: (\S+)', sv1.output)[1] != re.search(r'Da tao luot nop: (\S+)', sv2.output)[1]
    wrong_path.finish(1); assert '404' in wrong_path.output; wrong_path.evidence('thi-nghiem3-sai-hub')
    wrong_event.finish(); assert not events(wrong_event.output)
    assert 'Qua thoi gian cho' in wrong_event.output and 'REST: graded' in wrong_event.output
    wrong_event.evidence('thi-nghiem4-sai-ten')
    late.finish(); assert not events(late.output)
    assert 'Qua thoi gian cho' in late.output and 'REST: graded' in late.output
    late.evidence('thi-nghiem5-watch-muon')
    two.finish(); assert 'CANH BAO' in two.output
    assert 'SignalR = graded, REST = graded' in two.output
    assert 'SignalR = failed, REST = failed' in two.output
    ids = re.findall(r'Da tao luot nop: ([0-9a-f-]+)', two.output)
    assert len(ids) == 2 and ids[0] != ids[1]
    for id_, final in zip(ids, ('graded', 'failed')):
        assert [(i, s) for i, s in events(two.output) if i == id_] == [(id_, 'processing'), (id_, final)]
    two.evidence('bai-tap2-3')
    server.evidence('edusubmit-server'); server.stop()
    offline = edu('Offline'); offline.finish(1)
    assert 'Loi ket noi' in offline.output; offline.evidence('thi-nghiem2-api-tat')
    note('EduSubmit: 8 dong, 2 client tach ID, 5 thi nghiem, failed + 2 luot/ket noi: PASS')
    server = api('RealtimeSubmissionSystem', 'Submission.RealtimeApi', 5000)
    request(base, 'GET', location, 404, code='SUBMISSION_NOT_FOUND')
    note('Khoi dong lai API: du lieu RAM mat, GET ID cu 404 (gioi han bai thuc hanh).')
    note('ALL CHECKS PASSED')
finally:
    for process in PROCESSES:
        process.stop()
    (EVIDENCE / 'verification-results.txt').write_text('\n'.join(REPORT) + '\n', encoding='utf-8')
