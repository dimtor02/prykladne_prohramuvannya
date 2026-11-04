"""Local Kestrel profile/header/log regression. API port 5080 must be free.
Run after Release build/tests with SeedUsers__Password set outside Git.
Raw logs stay outside the repository; only selected, checked evidence is exported.
"""
import contextlib
import datetime
import http.cookiejar
import json
import os
from pathlib import Path
import re
import socket
import subprocess
import tempfile
import time
import urllib.error
import urllib.request

PROJECT = Path(__file__).resolve().parents[2]
OUTPUT = Path(os.environ.get('LAB06_OUTPUT_DIR', PROJECT / 'docs/evidence/lr06'))
PRIVATE = Path(os.environ.get('LAB06_PRIVATE_DIR', Path(tempfile.gettempdir()) / 'securelab-lr06'))
PASSWORD = os.environ['SeedUsers__Password']
FLAGS = subprocess.CREATE_NO_WINDOW if os.name == 'nt' else 0
OUTPUT.mkdir(parents=True, exist_ok=True)
PRIVATE.mkdir(parents=True, exist_ok=True)


def read_log(path):
    raw = path.read_bytes()
    try:
        return raw.decode('utf-8')
    except UnicodeDecodeError:
        return raw.decode('cp1251')


def client():
    return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))


def request(c, path, method='GET', body=None, headers=None):
    h = {'Accept': 'application/json', **(headers or {})}
    if body is not None:
        h['Content-Type'] = 'application/json'
    req = urllib.request.Request('http://localhost:5080' + path, method=method, headers=h,
                                 data=json.dumps(body).encode() if body is not None else None)
    try:
        response = c.open(req, timeout=15)
    except urllib.error.HTTPError as e:
        response = e
    text = response.read().decode()
    try:
        data = json.loads(text)
    except ValueError:
        data = text
    return response.status, {k.lower(): v for k, v in response.headers.items()}, data


@contextlib.contextmanager
def api(profile, broken=False):
    with socket.socket() as s:
        if s.connect_ex(('127.0.0.1', 5080)) == 0:
            raise RuntimeError('Stop the existing API on port 5080 before this script.')
    env = dict(os.environ, ASPNETCORE_ENVIRONMENT=profile, ASPNETCORE_URLS='http://localhost:5080',
               Logging__Console__IncludeScopes='true')
    if profile == 'Production':
        # Documented local study configuration, never a deployment credential.
        port = 54399 if broken else 54329
        env['ConnectionStrings__SecureLab'] = (
            f'Host=127.0.0.1;Port={port};Database=securelab;Username=securelab;'
            'Password=local-study-password;Include Error Detail=false;Timeout=2')
    else:
        env.pop('ConnectionStrings__SecureLab', None)
    log_path = PRIVATE / f'api-{profile}-{broken}.log'
    with log_path.open('w', encoding='utf-8') as log:
        process = subprocess.Popen(['dotnet', 'bin/Release/net10.0/SecureLab.Api.dll'],
            cwd=PROJECT / 'src/SecureLab.Api', env=env, stdout=log, stderr=log, creationflags=FLAGS)
    try:
        for _ in range(80):
            if process.poll() is not None:
                raise RuntimeError('API exited; inspect private local log.')
            try:
                if request(client(), '/')[0] == 200:
                    break
            except OSError:
                time.sleep(.25)
        else:
            raise RuntimeError('API readiness timeout')
        yield log_path
    finally:
        process.terminate()
        process.wait(timeout=15)


def safe_observation(label, response):
    status, headers, body = response
    return {'case': label, 'status': status, 'headers': {k: v for k, v in headers.items()
        if k in ['content-type', 'content-security-policy', 'x-content-type-options',
                 'referrer-policy', 'cache-control', 'access-control-allow-origin']},
        'problem': body if status >= 400 and isinstance(body, dict) else None}


def assert_headers(response):
    h = response[1]
    assert h['x-content-type-options'] == 'nosniff'
    assert h['referrer-policy'] == 'no-referrer'
    assert "script-src 'self'" in h['content-security-policy']


def main():
    proof = {'utc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'profiles': {}, 'observations': []}
    logs = []
    secret_values = [PASSWORD, 'NotARealPassword1!']
    with api('Production', broken=True) as log:
        error = request(client(), '/api/auth/login', 'POST', {'userName': 'alice', 'password': 'NotARealPassword1!'})
        assert error[0] == 500
        assert error[1]['content-type'].startswith('application/problem+json')
        assert_headers(error)
        assert set(error[2]) == {'type', 'title', 'status', 'traceId'}
        proof['observations'].append(safe_observation('Production unexpected 500', error))
    logs.append(log)
    lines = read_log(log).splitlines()
    at = next(i for i, line in enumerate(lines) if 'fail: Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware' in line)
    excerpt = lines[at:at + 3]
    trace = error[2]['traceId'].split('-')[1]
    assert any('TraceId:' + trace in line for line in excerpt)
    assert 'Hosting environment: Production' in '\n'.join(lines)
    proof['errorCorrelation'] = {'responseTraceId': error[2]['traceId'], 'logTraceId': trace, 'excerpt': excerpt}
    with api('Production') as log:
        c = client()
        for name, path in [('static', '/'), ('health-restored', '/health'), ('scalar', '/scalar/v1'), ('openapi', '/openapi/v1.json')]:
            r = request(c, path)
            assert_headers(r)
            proof['observations'].append(safe_observation('Production ' + name, r))
            if name == 'scalar':
                assert r[0] == 200 and 'text/html' in r[1]['content-type'] and 'Scalar' not in r[2]
            if name == 'openapi':
                assert r[0] == 404
        proof['profiles']['Production'] = {'health': 200, 'scalar': 'SPA fallback, not API reference', 'openapi': 404, 'teachingCors': 'absent'}
    logs.append(log)
    with api('Development') as log:
        c = client()
        assert request(c, '/api/auth/login', 'POST', {'userName': 'alice', 'password': 'NotARealPassword1!'})[0] == 401
        assert request(c, '/api/auth/login', 'POST', {'userName': 'alice', 'password': PASSWORD})[0] == 204
        assert request(c, '/api/me')[0] == 200
        issued = request(c, '/api/auth/csrf-token')
        assert issued[0] == 200
        token = issued[2]['token']
        secret_values.append(token)
        h = {'X-CSRF-TOKEN': token}
        proof['observations'].append(safe_observation('Development token (value omitted)', issued))
        invalid = request(c, '/api/incidents', 'POST', {'title': '', 'description': ''}, h)
        assert invalid[0] == 400 and 'title' in invalid[2]['errors']
        proof['observations'].append(safe_observation('Development domain validation', invalid))
        body = {'title': 'LR06 runtime ' + str(time.time_ns()), 'description': 'Synthetic runtime log review',
                'severity': 'Low', 'occurredAtUtc': datetime.datetime.now(datetime.timezone.utc).isoformat()}
        made = request(c, '/api/incidents', 'POST', body, h)
        assert made[0] == 201
        path = '/api/incidents/' + made[2]['id']
        before = request(c, path)[2]
        denied = request(c, path + '/triage', 'POST', headers=h)
        assert denied[0] == 403 and request(c, path)[2] == before
        proof['observations'].append(safe_observation('Reporter triage denied; unchanged', denied))
        morgan = client()
        assert request(morgan, '/api/auth/login', 'POST', {'userName': 'morgan', 'password': PASSWORD})[0] == 204
        mt = request(morgan, '/api/auth/csrf-token')[2]['token']
        secret_values.append(mt)
        transition = request(morgan, path + '/triage', 'POST', headers={'X-CSRF-TOKEN': mt})
        assert transition[0] == 204
        after = request(c, path)[2]
        assert after['status'] == 'Triaged'
        proof['audit'] = {'resource': made[2]['id'], 'deniedStatus': 403, 'deniedStateUnchanged': True,
                          'allowedStatus': 204, 'oldStatus': before['status'], 'newStatus': after['status']}
        assert request(c, path, 'DELETE', headers=h)[0] == 204
        api_doc = request(c, '/openapi/v1.json')
        assert api_doc[0] == 200
        proof['openapiMutations'] = sorted(method.upper() + ' ' + path for path, methods in api_doc[2]['paths'].items()
                                         for method in methods if method in ['post', 'put', 'patch', 'delete'])
        proof['profiles']['Development'] = {'health': request(c, '/health')[0], 'openapi': 200, 'normalLogin': 204}
    logs.append(log)
    all_text = '\n'.join(read_log(p) for p in logs)
    checks = {'seedPassword': all_text.count(PASSWORD), 'wrongPassword': all_text.count('NotARealPassword1!'),
              'issuedTokens': sum(all_text.count(s) for s in secret_values[2:]),
              'cookieHeaders': len(re.findall(r'(?i)cookie:|set-cookie', all_text)),
              'csrfHeader': all_text.lower().count('x-csrf-token')}
    assert all(v == 0 for v in checks.values()), 'Sensitive data found in private logs; do not export logs.'
    audit = [line.strip() for line in all_text.splitlines() if 'incident.transition subject=' in line or 'authorization.denied subject=' in line]
    assert any('result=success' in line for line in audit) and any('result=denied' in line for line in audit)
    proof['logReview'] = {'scenarios': ['successful login', 'failed login', 'denied action', 'validation error', 'unexpected error', 'privileged transition'],
                          'sink': 'three local Kestrel console logs, unredacted originals checked outside repository',
                          'sensitiveValueMatches': checks, 'auditExcerpt': audit}
    (OUTPUT / 'runtime-checks.json').write_text(json.dumps(proof, ensure_ascii=False, indent=2), encoding='utf-8')
    print('PASS: Kestrel Production 500/headers/trace; profile recovery; Development audit/log review; OpenAPI perimeter.')


if __name__ == '__main__':
    main()
