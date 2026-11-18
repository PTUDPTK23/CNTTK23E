import json, sqlite3, uuid
from datetime import datetime, timezone
from http.server import BaseHTTPRequestHandler, HTTPServer
from urllib.parse import urlparse

DB = "parcel_tracking.db"
STATUSES = {"CREATED", "PICKED_UP", "IN_TRANSIT", "OUT_FOR_DELIVERY", "DELIVERED", "FAILED"}
now = lambda: datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

def init_db():
    c = sqlite3.connect(DB)
    c.execute("CREATE TABLE IF NOT EXISTS parcels(tracking_code TEXT PRIMARY KEY, sender_name TEXT, receiver_name TEXT, origin_hub TEXT, dest_hub TEXT, current_status TEXT, created_at TEXT)")
    c.execute("CREATE TABLE IF NOT EXISTS tracking_events(event_id TEXT PRIMARY KEY, tracking_code TEXT, hub_code TEXT, status TEXT, note TEXT, event_time TEXT)")
    if c.execute("SELECT COUNT(*) FROM parcels").fetchone()[0] == 0:
        ts = now()
        c.execute("INSERT INTO parcels VALUES (?,?,?,?,?,?,?)", ("VN20260000852", "Nguyen Anh Son", "Tran Van B", "HUB-TN01", "HUB-HCM01", "IN_TRANSIT", ts))
        c.execute("INSERT INTO tracking_events VALUES (?,?,?,?,?,?)", (str(uuid.uuid4()), "VN20260000852", "HUB-TN01", "CREATED", "Tao van don", ts))
    c.commit(); c.close()

class Handler(BaseHTTPRequestHandler):
    def send_json(self, code, data):
        b = json.dumps(data, ensure_ascii=False, indent=2).encode()
        self.send_response(code); self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(b))); self.end_headers(); self.wfile.write(b)

    def do_GET(self):
        p = [x for x in urlparse(self.path).path.split("/") if x]
        c = sqlite3.connect(DB); c.row_factory = sqlite3.Row
        if p == ["api", "parcels"]:
            rows = [dict(r) for r in c.execute("SELECT * FROM parcels").fetchall()]
            c.close(); return self.send_json(200, {"count": len(rows), "items": rows})
        if len(p) == 3 and p[:2] == ["api", "parcels"]:
            row = c.execute("SELECT * FROM parcels WHERE tracking_code=?", (p[2],)).fetchone()
            if not row: c.close(); return self.send_json(404, {"error": "Khong tim thay van don"})
            evs = [dict(e) for e in c.execute("SELECT * FROM tracking_events WHERE tracking_code=?", (p[2],)).fetchall()]
            c.close(); d = dict(row); d["history"] = evs; return self.send_json(200, d)
        c.close(); self.send_json(404, {"error": "Not found"})

    def do_POST(self):
        p = [x for x in urlparse(self.path).path.split("/") if x]
        try: data = json.loads(self.rfile.read(int(self.headers.get("Content-Length", 0))).decode())
        except Exception: return self.send_json(400, {"error": "JSON khong hop le"})
        c = sqlite3.connect(DB); ts = now()
        if p == ["api", "parcels"]:
            req = ["sender_name", "receiver_name", "origin_hub", "dest_hub"]
            if any(not data.get(k) for k in req): c.close(); return self.send_json(400, {"error": "Thieu truong bat buoc"})
            code = data.get("tracking_code") or f"VN{uuid.uuid4().hex[:8].upper()}"
            c.execute("INSERT OR REPLACE INTO parcels VALUES (?,?,?,?,?,?,?)", (code, data["sender_name"], data["receiver_name"], data["origin_hub"], data["dest_hub"], "CREATED", ts))
            c.execute("INSERT INTO tracking_events VALUES (?,?,?,?,?,?)", (str(uuid.uuid4()), code, data["origin_hub"], "CREATED", "Khoi tao don", ts))
            c.commit(); c.close(); return self.send_json(201, {"tracking_code": code, "status": "CREATED"})
        if len(p) == 4 and p[:2] == ["api", "parcels"] and p[3] == "events":
            if data.get("status") not in STATUSES or not data.get("hub_code"): c.close(); return self.send_json(400, {"error": "Du lieu khong hop le"})
            if not c.execute("SELECT 1 FROM parcels WHERE tracking_code=?", (p[2],)).fetchone(): c.close(); return self.send_json(404, {"error": "Khong tim thay van don"})
            c.execute("INSERT OR IGNORE INTO tracking_events VALUES (?,?,?,?,?,?)", (str(uuid.uuid4()), p[2], data["hub_code"], data["status"], data.get("note", ""), ts))
            c.execute("UPDATE parcels SET current_status=? WHERE tracking_code=?", (data["status"], p[2]))
            c.commit(); c.close(); return self.send_json(201, {"tracking_code": p[2], "status": data["status"]})
        c.close(); self.send_json(404, {"error": "Not found"})

if __name__ == "__main__":
    init_db(); print("Running at http://localhost:8080")
    HTTPServer(("0.0.0.0", 8080), Handler).serve_forever()
