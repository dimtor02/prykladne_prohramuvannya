# ЛР3: фінальна технічна перевірка

20.10.2026, Windows, Release/net10.0, власна локальна синтетична БД.

Build: 0 warnings, 0 errors. Два reset успішні. Паролі, cookie values, hashes, connection strings та повні logs не включено.

- Після reset 1: total=80, passed=80, failed=0, skipped=0.
- Після reset 2: total=80, passed=80, failed=0, skipped=0.

## Власні auth/security докази

25 власних випадків у кожному повному suite; решта — 54 перевірки ЛР2 і 1 scaffold mechanics.

### A01_AnonymousProfileAndCreate_Return401_EvenWithForgedHeader(forgeHeader: False)

- EVIDENCE A-01/me: HTTP 401, application/problem+json, no redirect/bearer challenge
- EVIDENCE A-01/create: HTTP 401, application/problem+json, no redirect/bearer challenge

### A01_AnonymousProfileAndCreate_Return401_EvenWithForgedHeader(forgeHeader: True)

- EVIDENCE A-01/me: HTTP 401, application/problem+json, no redirect/bearer challenge
- EVIDENCE A-01/create: HTTP 401, application/problem+json, no redirect/bearer challenge

### A02_LoginThenMe_ReturnsOnlySafeAliceProfile

- EVIDENCE A-02: login 204; me 200; Alice id; exactly id/userName/displayName

### A03_ForeignHeaderCannotReplaceAuthenticatedAlice

- EVIDENCE A-03: Bob header with Alice cookie; unchanged Alice profile

### A04_A05_BobOwnsNewIncident_WithOrWithoutForgedAliceOwner(forgeOwner: False)

- EVIDENCE A-04: HTTP 201; stored owner=Bob; forged header ignored

### A04_A05_BobOwnsNewIncident_WithOrWithoutForgedAliceOwner(forgeOwner: True)

- EVIDENCE A-05: HTTP 201; stored owner=Bob; forged header ignored

### A06_LogoutDeletesMainCookie_AndSameClientLosesAccess

- EVIDENCE A-06: HTTP 401, application/problem+json, no redirect/bearer challenge
- EVIDENCE A-06/create: HTTP 401, application/problem+json, no redirect/bearer challenge

### A07_UnknownUserAndWrongPassword_HaveIdenticalStableProblemShape

- EVIDENCE A-07/unknown: HTTP 401, application/problem+json, no redirect/bearer challenge
- EVIDENCE A-07/wrong: HTTP 401, application/problem+json, no redirect/bearer challenge

### CookieFlags_HttpsAndProduction_UseSecureHttpOnlyLaxSessionCookie

- EVIDENCE Cookie flags: Development HTTPS and Production HTTPS; Secure, HttpOnly, SameSite=Lax, Path=/, session cookie

### LoginRateLimit_DefaultPolicy_AllowsTenAttemptsThen429

- EVIDENCE Default quota: HTTP 429, application/problem+json, no redirect/bearer challenge
- EVIDENCE Rate limit/default: configured 10 attempts/60s; attempts 1-10=401, 11=429

### LoginRateLimit_TransportIpQuota_RecoversAndIgnoresForwardedHeader

- EVIDENCE Rate limit: HTTP 429, application/problem+json, no redirect/bearer challenge
- EVIDENCE Rate limit: real login policy, isolated test options 3/3s; 401x3 -> 429; another client/name/forwarded header shares quota; health 200; new host independent; after window login 204

### S01_AuthenticationEvents_ContainNoCredentialsOrRequestBody

- EVIDENCE S-01/events: failure, success, logout; only event/result/subject/trace in auth messages; no password/cookie/hash/token/raw body/user name

### T01_Duplicate_OverpostingAndPasswordWhitespace_RespectRegistrationContract

- EVIDENCE Duplicate/both: HTTP 409, application/problem+json, no redirect/bearer challenge
- EVIDENCE Duplicate/name-case: HTTP 409, application/problem+json, no redirect/bearer challenge
- EVIDENCE Duplicate/email-case: HTTP 409, application/problem+json, no redirect/bearer challenge
- EVIDENCE T-01/T-03: safe 201; Reporter only; server id; standard password verifier; password whitespace preserved; duplicates 409

### T02_InvalidRegister_ReturnsSafe400_WithoutCreatingUser(scenario: "bad-email", field: "email")

- EVIDENCE T-02/bad-email: HTTP 400, application/problem+json, no redirect/bearer challenge

### T02_InvalidRegister_ReturnsSafe400_WithoutCreatingUser(scenario: "bad-name", field: "userName")

- EVIDENCE T-02/bad-name: HTTP 400, application/problem+json, no redirect/bearer challenge

### T02_InvalidRegister_ReturnsSafe400_WithoutCreatingUser(scenario: "blank-display", field: "displayName")

- EVIDENCE T-02/blank-display: HTTP 400, application/problem+json, no redirect/bearer challenge

### T02_InvalidRegister_ReturnsSafe400_WithoutCreatingUser(scenario: "control-display", field: "displayName")

- EVIDENCE T-02/control-display: HTTP 400, application/problem+json, no redirect/bearer challenge

### T02_InvalidRegister_ReturnsSafe400_WithoutCreatingUser(scenario: "long-display", field: "displayName")

- EVIDENCE T-02/long-display: HTTP 400, application/problem+json, no redirect/bearer challenge

### T02_InvalidRegister_ReturnsSafe400_WithoutCreatingUser(scenario: "long-email", field: "email")

- EVIDENCE T-02/long-email: HTTP 400, application/problem+json, no redirect/bearer challenge

### T02_InvalidRegister_ReturnsSafe400_WithoutCreatingUser(scenario: "long-name", field: "userName")

- EVIDENCE T-02/long-name: HTTP 400, application/problem+json, no redirect/bearer challenge

### T02_InvalidRegister_ReturnsSafe400_WithoutCreatingUser(scenario: "long-password", field: "password")

- EVIDENCE T-02/long-password: HTTP 400, application/problem+json, no redirect/bearer challenge

### T02_InvalidRegister_ReturnsSafe400_WithoutCreatingUser(scenario: "missing", field: "userName")

- EVIDENCE T-02/missing: HTTP 400, application/problem+json, no redirect/bearer challenge

### T02_InvalidRegister_ReturnsSafe400_WithoutCreatingUser(scenario: "short-password", field: "password")

- EVIDENCE T-02/short-password: HTTP 400, application/problem+json, no redirect/bearer challenge

### T02_InvalidRegister_ReturnsSafe400_WithoutCreatingUser(scenario: "weak-password", field: "password")

- EVIDENCE T-02/weak-password: HTTP 400, application/problem+json, no redirect/bearer challenge

### T03_SeedIdentityModel_KeepsGuidsRolesOwnersAndStandardSaltedHashes

- EVIDENCE T-03/seed: four stable GUIDs/roles; Alice/Bob incident owners preserved; single Identity store; PasswordHash column, no Password column; four distinct salted representations; standard verifier accepts seed password; no hash output

## Реальний HTTP flow

- Anonymous GET /api/me with Bob header: 401
- Alice login: 204; cookie retained only in client memory
- GET /api/me with Alice cookie and Bob header: 200, safe Alice profile
- POST /api/incidents without owner: 201; details ownerDisplayName equals authenticated Alice
- Logout: 204; same HTTP client GET /api/me afterwards: 401
- No password, hash, Cookie or Set-Cookie values included in this evidence.

Після HTTP-сценарію видалено рівно один власний тестовий запис (DELETE 1); API зупинено runner-ом. Браузерний сценарій оформлюється окремо.
