# ЛР4 — фінальні автоматичні докази

Фактичний завершений прогін: 2026-10-20T21:46:46.7606784+03:00 — 2026-10-20T21:47:13.1885559+03:00.
Release build: 0 помилок, 0 попереджень. Перед tests локальний API зупинено, reset Development завершився успішно.
Suite: 113 passed, 0 failed, 0 not executed; 33 authorization cases.
Середовище: Windows; SDK 10.0.401; target net10.0; локальний Docker PostgreSQL 18.4, loopback port 54329.
Джерело: tmp/lr04-tests/lr04-final.trx поза Git. Нижче лише дозволений ITestOutput потрібних сценаріїв; cookie/login body не включено.

## Build

```text
SecureLab.Api -> C:\Users\dimto\Desktop\uni_4_1\Прикладні технології програмування (лаб Бабенко Ю.М.)\lab-01-starter\starter-project\src\SecureLab.Api\bin\Release\net10.0\SecureLab.Api.dll
  SecureLab.Api.Tests -> C:\Users\dimto\Desktop\uni_4_1\Прикладні технології програмування (лаб Бабенко Ю.М.)\lab-01-starter\starter-project\tests\SecureLab.Api.Tests\bin\Release\net10.0\SecureLab.Api.Tests.dll

Сборка успешно завершена.
    Предупреждений: 0
    Ошибок: 0

Прошло времени 00:00:01.34
```

## HTTP authorization scenarios

### SecureLab.Api.Tests.AuthorizationSecurityTests.A01_AnonymousProtectedRequests_Return401WithoutRedirect(method: "DELETE", path: "/api/incidents/20000000-0000-0000-0000-00000000000"···)

Outcome: Passed.

```text
A-01 DELETE /api/incidents/20000000-0000-0000-0000-000000000001: 401, no redirect, seed unchanged
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A01_AnonymousProtectedRequests_Return401WithoutRedirect(method: "GET", path: "/api/incident-comments/30000000-0000-0000-0000-000"···)

Outcome: Passed.

```text
A-01 GET /api/incident-comments/30000000-0000-0000-0000-000000000001: 401, no redirect, seed unchanged
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A01_AnonymousProtectedRequests_Return401WithoutRedirect(method: "GET", path: "/api/incidents")

Outcome: Passed.

```text
A-01 GET /api/incidents: 401, no redirect, seed unchanged
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A01_AnonymousProtectedRequests_Return401WithoutRedirect(method: "GET", path: "/api/incidents/20000000-0000-0000-0000-00000000000"···)

Outcome: Passed.

```text
A-01 GET /api/incidents/20000000-0000-0000-0000-000000000001: 401, no redirect, seed unchanged
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A01_AnonymousProtectedRequests_Return401WithoutRedirect(method: "POST", path: "/api/incidents/20000000-0000-0000-0000-00000000000"···)

Outcome: Passed.

```text
A-01 POST /api/incidents/20000000-0000-0000-0000-000000000003/triage: 401, no redirect, seed unchanged
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A01_AnonymousProtectedRequests_Return401WithoutRedirect(method: "PUT", path: "/api/incidents/20000000-0000-0000-0000-00000000000"···)

Outcome: Passed.

```text
A-01 PUT /api/incidents/20000000-0000-0000-0000-000000000001: 401, no redirect, seed unchanged
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A02_A03_A03R_DetailsFollowOwnerAndRoleMatrix(user: "admin", id: "20000000-0000-0000-0000-000000000001", expected: 404)

Outcome: Passed.

```text
A-02/A-03/A-03R admin GET 20000000-0000-0000-0000-000000000001: 404; forged headers have no authority
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A02_A03_A03R_DetailsFollowOwnerAndRoleMatrix(user: "alice", id: "20000000-0000-0000-0000-000000000001", expected: 200)

Outcome: Passed.

```text
A-02/A-03/A-03R alice GET 20000000-0000-0000-0000-000000000001: 200; forged headers have no authority
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A02_A03_A03R_DetailsFollowOwnerAndRoleMatrix(user: "alice", id: "20000000-0000-0000-0000-000000000002", expected: 404)

Outcome: Passed.

```text
A-02/A-03/A-03R alice GET 20000000-0000-0000-0000-000000000002: 404; forged headers have no authority
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A02_A03_A03R_DetailsFollowOwnerAndRoleMatrix(user: "bob", id: "20000000-0000-0000-0000-000000000001", expected: 404)

Outcome: Passed.

```text
A-02/A-03/A-03R bob GET 20000000-0000-0000-0000-000000000001: 404; forged headers have no authority
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A02_A03_A03R_DetailsFollowOwnerAndRoleMatrix(user: "bob", id: "20000000-0000-0000-0000-000000000002", expected: 200)

Outcome: Passed.

```text
A-02/A-03/A-03R bob GET 20000000-0000-0000-0000-000000000002: 200; forged headers have no authority
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A02_A03_A03R_DetailsFollowOwnerAndRoleMatrix(user: "morgan", id: "20000000-0000-0000-0000-000000000001", expected: 200)

Outcome: Passed.

```text
A-02/A-03/A-03R morgan GET 20000000-0000-0000-0000-000000000001: 200; forged headers have no authority
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A02_A03_A03R_DetailsFollowOwnerAndRoleMatrix(user: "morgan", id: "20000000-0000-0000-0000-000000000002", expected: 200)

Outcome: Passed.

```text
A-02/A-03/A-03R morgan GET 20000000-0000-0000-0000-000000000002: 200; forged headers have no authority
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A04_NonOwnerMutation_Returns404_AndLeavesWholeResourceUnchanged(method: "DELETE")

Outcome: Passed.

```text
A-04 Alice DELETE Bob: 404; all scalar fields/comments/history unchanged
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A04_NonOwnerMutation_Returns404_AndLeavesWholeResourceUnchanged(method: "POST")

Outcome: Passed.

```text
A-04 Alice POST Bob: 404; all scalar fields/comments/history unchanged
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A04_NonOwnerMutation_Returns404_AndLeavesWholeResourceUnchanged(method: "PUT")

Outcome: Passed.

```text
A-04 Alice PUT Bob: 404; all scalar fields/comments/history unchanged
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A05_ListContainsExactlyTheAllowedSet(user: "admin")

Outcome: Passed.

```text
A-05 admin: exact authorized set, 0 rows
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A05_ListContainsExactlyTheAllowedSet(user: "alice")

Outcome: Passed.

```text
A-05 alice: exact authorized set, 3 rows
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A05_ListContainsExactlyTheAllowedSet(user: "bob")

Outcome: Passed.

```text
A-05 bob: exact authorized set, 2 rows
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A05_ListContainsExactlyTheAllowedSet(user: "morgan")

Outcome: Passed.

```text
A-05 morgan: exact authorized set, 5 rows
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A06_ReporterTriage_Returns403Forbidden_AndDoesNotChangeStatus

Outcome: Passed.

```text
A-06 Reporter Alice triage own New: 403 Forbidden; status/content/history unchanged
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A07_AnalystCanOnlyTriageNew_AndHistoryUsesVerifiedActor

Outcome: Passed.

```text
A-07 Morgan: New->Triaged 204; forged Closed ignored; actor Morgan; repeat 409 unchanged
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A08a_OwnerCanUpdateContent_ButForgedOwnerRoleStatusSeverityAreIgnored

Outcome: Passed.

```text
A-08a PUT 204; content changed; owner/role/status/severity/id/times preserved; triage still 403
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A09_DeniedPrivilegeAudit_ContainsOnlySafeFields_AndMatchingResponseTrace

Outcome: Passed.

```text
A-09 safe structured event, response trace matches: {"EventName":"authorization.denied","SubjectId":"10000000-0000-0000-0000-000000000001","Action":"incident.triage","ResourceId":"20000000-0000-0000-0000-000000000003","Result":"denied","OccurredAtUtc":"2026-10-20T18:46:56.1818915+00:00","TraceId":"00-b99523397dc560fc96075207e9547ecb-9e9cd1a89e6e7e89-00","{OriginalFormat}":"{EventName} subject={SubjectId} action={Action} resource={ResourceId} result={Result} occurredAtUtc={OccurredAtUtc:O} traceId={TraceId}"}
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A10_ForeignAndUnknownIds_HaveIdentical404Responses(method: "CHILD")

Outcome: Passed.

```text
A-10 CHILD foreign={"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"\u0406\u043D\u0446\u0438\u0434\u0435\u043D\u0442 \u043D\u0435 \u0437\u043D\u0430\u0439\u0434\u0435\u043D\u043E","status":404}; unknown={"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"\u0406\u043D\u0446\u0438\u0434\u0435\u043D\u0442 \u043D\u0435 \u0437\u043D\u0430\u0439\u0434\u0435\u043D\u043E","status":404}; Content-Type=application/problem+json
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A10_ForeignAndUnknownIds_HaveIdentical404Responses(method: "DELETE")

Outcome: Passed.

```text
A-10 DELETE foreign={"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"\u0406\u043D\u0446\u0438\u0434\u0435\u043D\u0442 \u043D\u0435 \u0437\u043D\u0430\u0439\u0434\u0435\u043D\u043E","status":404}; unknown={"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"\u0406\u043D\u0446\u0438\u0434\u0435\u043D\u0442 \u043D\u0435 \u0437\u043D\u0430\u0439\u0434\u0435\u043D\u043E","status":404}; Content-Type=application/problem+json
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A10_ForeignAndUnknownIds_HaveIdentical404Responses(method: "GET")

Outcome: Passed.

```text
A-10 GET foreign={"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"\u0406\u043D\u0446\u0438\u0434\u0435\u043D\u0442 \u043D\u0435 \u0437\u043D\u0430\u0439\u0434\u0435\u043D\u043E","status":404}; unknown={"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"\u0406\u043D\u0446\u0438\u0434\u0435\u043D\u0442 \u043D\u0435 \u0437\u043D\u0430\u0439\u0434\u0435\u043D\u043E","status":404}; Content-Type=application/problem+json
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A10_ForeignAndUnknownIds_HaveIdentical404Responses(method: "POST")

Outcome: Passed.

```text
A-10 POST foreign={"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"\u0406\u043D\u0446\u0438\u0434\u0435\u043D\u0442 \u043D\u0435 \u0437\u043D\u0430\u0439\u0434\u0435\u043D\u043E","status":404}; unknown={"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"\u0406\u043D\u0446\u0438\u0434\u0435\u043D\u0442 \u043D\u0435 \u0437\u043D\u0430\u0439\u0434\u0435\u043D\u043E","status":404}; Content-Type=application/problem+json
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A10_ForeignAndUnknownIds_HaveIdentical404Responses(method: "PUT")

Outcome: Passed.

```text
A-10 PUT foreign={"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"\u0406\u043D\u0446\u0438\u0434\u0435\u043D\u0442 \u043D\u0435 \u0437\u043D\u0430\u0439\u0434\u0435\u043D\u043E","status":404}; unknown={"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"\u0406\u043D\u0446\u0438\u0434\u0435\u043D\u0442 \u043D\u0435 \u0437\u043D\u0430\u0439\u0434\u0435\u043D\u043E","status":404}; Content-Type=application/problem+json
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.A12_ChildChecksParent_AndInternalCommentIsHiddenFromReporterOwner

Outcome: Passed.

```text
A-12 Reporter public own 200; foreign/internal 404; internal absent from details; Analyst internal/details/foreign child 200
```

### SecureLab.Api.Tests.AuthorizationSecurityTests.AnalystReadPermissionDoesNotGrantContentMutation(method: "DELETE")

Outcome: Passed.

### SecureLab.Api.Tests.AuthorizationSecurityTests.AnalystReadPermissionDoesNotGrantContentMutation(method: "PUT")

Outcome: Passed.

### SecureLab.Api.Tests.AuthorizationSecurityTests.OwnerCanDeleteOwnFixture_AndChildrenCascade

Outcome: Passed.

## Інтерпретація

A-10 містить обидві фактичні відповіді foreign/unknown для кожного маршруту та Content-Type. Перед порівнянням вилучено лише traceId/requestId; status кожної відповіді перевірено як 404.
A-09 містить фактичний structured event authorization.denied. TraceId збігається з Problem Details відповідного запиту; у події немає body, cookie чи token.
A-04/A-06 порівнюють повторно прочитаний повний snapshot ресурсу; A-07 читає збережений status і history з БД. Власні mutation fixtures прибираються у finally.

## Початкові докази

До виправлення: 80/80 baseline tests; 1/1 PoC відтворив read/mutation/privilege bypass. Див. lr04-vulnerable.md і commit 85ca4ac267633d128a9ac8dd9ca97242d1adc4d5.

## Live HTTP retest після фінального reset/tests

```text
LIVE PASS: health/root 200; anonymous 401; Alice own 200; foreign GET/PUT/DELETE 404;
Reporter triage 403 unchanged; scoped list; Analyst read 200; parent/internal checks; equivalent 404.
```

Script завершився exit 0; cookie лише в пам’яті, password із environment, виконано logout обох clients. Seed не змінювався.
