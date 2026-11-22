import time, json, requests

BASE_URL = "http://localhost:5000"
PING_URL = "http://localhost:5200"

print("--- Testing EduSubmit & PingSignalR Endpoints ---")

# 1. GET / -> 200
r1 = requests.get(f"{BASE_URL}/")
print(f"Ca 1: GET / -> {r1.status_code} ({r1.json()})")
assert r1.status_code == 200

# 2. GET /api/assignments -> 200
r2 = requests.get(f"{BASE_URL}/api/assignments")
print(f"Ca 2: GET /api/assignments -> {r2.status_code} (found {len(r2.json())} assignments)")
assert r2.status_code == 200

# 3. POST /api/assignments/1/submissions -> 201
payload = {
    "studentCode": "SV001",
    "repositoryUrl": "https://example.com/sv001/signalr"
}
r3 = requests.post(f"{BASE_URL}/api/assignments/1/submissions", json=payload)
sub_data = r3.json()
sub_id = sub_data["id"]
print(f"Ca 3: POST /api/assignments/1/submissions -> {r3.status_code} (ID: {sub_id}, Status: {sub_data['status']}, UpdatedAt: {sub_data['updatedAt']})")
assert r3.status_code == 201

# 4. GET /api/submissions/{id} -> 200 immediately, then processing, then graded
r4 = requests.get(f"{BASE_URL}/api/submissions/{sub_id}")
print(f"Ca 4a: GET /api/submissions/{sub_id} (t=0s) -> {r4.status_code} (Status: {r4.json()['status']})")
assert r4.status_code == 200

time.sleep(1.5)
r4b = requests.get(f"{BASE_URL}/api/submissions/{sub_id}")
print(f"Ca 4b: GET /api/submissions/{sub_id} (t=1.5s) -> {r4b.status_code} (Status: {r4b.json()['status']})")

time.sleep(2.0)
r4c = requests.get(f"{BASE_URL}/api/submissions/{sub_id}")
print(f"Ca 4c: GET /api/submissions/{sub_id} (t=3.5s) -> {r4c.status_code} (Status: {r4c.json()['status']})")
assert r4c.json()['status'] == 'graded'

# 5. POST missing data -> 400
r5 = requests.post(f"{BASE_URL}/api/assignments/1/submissions", json={"studentCode": "", "repositoryUrl": ""})
print(f"Ca 5: POST missing studentCode -> {r5.status_code} ({r5.json()})")
assert r5.status_code == 400

# 6. POST invalid assignment -> 404
r6 = requests.post(f"{BASE_URL}/api/assignments/999/submissions", json=payload)
print(f"Ca 6: POST assignment 999 -> {r6.status_code} ({r6.json()})")
assert r6.status_code == 404

# 7. GET invalid submission GUID -> 404
r7 = requests.get(f"{BASE_URL}/api/submissions/00000000-0000-0000-0000-000000000000")
print(f"Ca 7: GET 00000000-... -> {r7.status_code} ({r7.json()})")
assert r7.status_code == 404

# 8. POST /hubs/submissions/negotiate -> 200
r8 = requests.post(f"{BASE_URL}/hubs/submissions/negotiate?negotiateVersion=1")
print(f"Ca 8: POST /hubs/submissions/negotiate -> {r8.status_code} (connectionId: {r8.json().get('connectionId')})")
assert r8.status_code == 200

# 9. POST /hub/submissions/negotiate -> 404
r9 = requests.post(f"{BASE_URL}/hub/submissions/negotiate?negotiateVersion=1")
print(f"Ca 9: POST /hub/submissions/negotiate (wrong path) -> {r9.status_code}")
assert r9.status_code == 404

print("\n>>> ALL 9 REQUESTS.HTTP TEST CASES PASSED SUCCESSFULLY! <<<")
