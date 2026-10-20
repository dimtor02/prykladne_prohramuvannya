"""Read-only/denied local retest. Cookie stays in memory; password comes from environment."""
import http.cookiejar
import json
import os
import urllib.error
import urllib.request

HOST = "http://localhost:5080"
ALICE = "20000000-0000-0000-0000-000000000001"
BOB = "20000000-0000-0000-0000-000000000002"
NEW = "20000000-0000-0000-0000-000000000003"


def client():
    return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))


def send(opener, path, method="GET", body=None):
    request = urllib.request.Request(
        HOST + path, method=method,
        data=None if body is None else json.dumps(body).encode("utf-8"),
        headers={"Accept": "application/json", "Content-Type": "application/json"},
    )
    try:
        response = opener.open(request, timeout=15)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        text = response.read().decode("utf-8")
        return response.status, json.loads(text) if text and "json" in response.headers.get("Content-Type", "") else None


def main():
    password = os.environ.get("SeedUsers__Password")
    if not password:
        raise SystemExit("Set SeedUsers__Password outside Git before running this script.")
    anonymous, alice, morgan = client(), client(), client()
    assert send(anonymous, "/health") == (200, {"status": "ready"})
    assert send(anonymous, "/")[0] == 200
    assert send(anonymous, f"/api/incidents/{ALICE}")[0] == 401
    for user, opener in [("alice", alice), ("morgan", morgan)]:
        assert send(opener, "/api/auth/login", "POST", {"userName": user, "password": password})[0] == 204
    own = send(alice, f"/api/incidents/{ALICE}")
    assert own[0] == 200 and own[1]["id"] == ALICE
    assert send(alice, f"/api/incidents/{BOB}")[0] == 404
    unknown = send(alice, "/api/incidents/ffffffff-ffff-ffff-ffff-ffffffffffff")
    foreign = send(alice, f"/api/incidents/{BOB}")
    for response in (foreign, unknown):
        assert response[0] == 404
        response[1].pop("traceId", None)
        response[1].pop("requestId", None)
    assert foreign == unknown
    before = send(alice, f"/api/incidents/{NEW}")
    assert send(alice, f"/api/incidents/{NEW}/triage", "POST", {"role": "Analyst"})[0] == 403
    assert send(alice, f"/api/incidents/{NEW}") == before
    assert send(alice, f"/api/incidents/{BOB}", "PUT", {"title": "forged", "description": "forged"})[0] == 404
    assert send(alice, f"/api/incidents/{BOB}", "DELETE")[0] == 404
    assert all(item["id"] != BOB for item in send(alice, "/api/incidents")[1])
    assert send(morgan, f"/api/incidents/{ALICE}")[0] == 200
    assert send(alice, "/api/incident-comments/30000000-0000-0000-0000-000000000002")[0] == 404
    assert send(alice, "/api/incident-comments/30000000-0000-0000-0000-000000000003")[0] == 404
    assert send(morgan, "/api/incident-comments/30000000-0000-0000-0000-000000000003")[0] == 200
    print("LIVE PASS: health/root 200; anonymous 401; Alice own 200; foreign GET/PUT/DELETE 404;")
    print("Reporter triage 403 unchanged; scoped list; Analyst read 200; parent/internal checks; equivalent 404.")
    for opener in (alice, morgan):
        assert send(opener, "/api/auth/logout", "POST")[0] == 204


if __name__ == "__main__":
    main()
