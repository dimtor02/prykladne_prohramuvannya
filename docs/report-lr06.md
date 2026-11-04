# Security review SecureLab, ЛР6

**Рівень:** «Добре». **Baseline:** 2-A.
**Студент:** Торонченко, КБ-41. **Початок огляду:** `e8b62cf0665bbff5a6247a53db5751663a9c2e84`, тег `v0.5.0`.

**Статус:** реалізація й перевірки робочого дерева виконані; commit/merge/tag/push ще не підтверджені. Звіт лише Markdown за прямою вказівкою користувача; PDF, передбачений методичкою, не створюється.

## 1. Executive summary

Перевірено локальний SecureLab як систему browser → API → PostgreSQL із cookie authentication, конфігурацією, залежностями та журналами. На початковому стані пройшли 124 тести. Відтворено й виправлено дві знахідки: відсутність CSRF-перевірки cookie mutations і втрату захисних заголовків на unexpected 500. Додано standard antiforgery flow, спільний клієнтський helper, search rate limit та явний audit event успішного triage. Поточний Release build має 0 warnings/errors; **141 тест пройдено, 0 failed/skipped**. Окремо пройшли реальні HTTP/Chrome перевірки CSRF, XSS, CORS, CSP і profile/log review. NuGet не повідомив про відомі advisories для фактичного direct/transitive graph. Це локальний доказ, а не сертифікація production: TLS/HSTS, distributed quotas, login-CSRF, public search/summary та concurrency залишаються явно обмеженими або неперевіреними.

## 2. Scope та exclusions

Оглянуто `src/SecureLab.Api`, клієнтські сторінки, tests, конфігурацію Docker/EF/Identity, README/docs і поточний tracked перелік, наданий власником. У репозиторії 147 paths на момент експорту: 129 текстових файлів перевірено regex і контекстним оглядом; 18 PNG не входять до текстового/OCR-пошуку. Нові source/test файли ЛР6 оглянуто додатково. Результат не поширюється на history, stash, інші clones, зовнішні CI або production deployment. Усі облікові записи й business data навчальні. Зовнішніх атак не проводили.

Login/register не покладаються на чинну auth cookie та не включені до authenticated antiforgery validation; login-CSRF/account creation ризик описано окремо. Logout включений. Redis і advanced extension не входять до обраного рівня; погодження розширення не підмінялося власним PASS.

## 3. System overview і trust boundaries

[Модель загроз](threat-model.md) містить схему, активи, суб’єктів, entry points, контролі та зв’язок кожної межі з тестами. Основний ресурс — Incident, дочірні IncidentComment та IncidentStatusHistory. Наявні Reporter/Analyst/Administrator roles взято з власного baseline, а не з нового варіанта.

Trust boundaries: browser/API, API/PostgreSQL, repository/NuGet/build, environment/configuration, API/logs. Authentication cookie встановлює principal; owner/role/status не приймаються з довільних client headers. Principal і antiforgery-пара вирішують різні задачі; business authorization збережена після CSRF filter.

## 4. Methodology та test environment

| Параметр | Фактичне значення |
|---|---|
| Review start | `e8b62cf0665bbff5a6247a53db5751663a9c2e84`; підтверджено користувацьким git rev-parse |
| Гілка | `lab/6-hardening-review`; на початку чиста, створена власником від v0.5.0 |
| SDK | 10.0.401; global.json: 10.0.100, latestFeature, allowPrerelease=false |
| API / БД | localhost:5080 / PostgreSQL localhost:54329; Docker service Healthy |
| Profiles | Development та окремий Production; останній через DLL без launchSettings override |
| HTTP client | Python urllib CookieJar; справжні Identity cookies; жодних fake production principals |
| Integration client | WebApplicationFactory/TestServer, HTTPS BaseAddress для Secure-cookie behavior |
| Browser | Chrome 156.0.8078.80 через Playwright; реальні DOM/fetch/preflight |
| Suite | 04.11.2026 20:48:47–20:49:24 +02:00; 141 passed / 0 failed / 0 skipped |
| Git fixed / accepted state | Ще не підтверджений; поточний результат належить робочому дереву, не неіснуючому final commit |

Стартові restore locked-mode, Release build, Development reset/seed та 124 baseline tests завершилися exit 0. До fix збережено власні HTTP-спостереження. Нові сценарії перевіряють стан ресурсу після відмови й дозволеної зміни, а не лише HTTP status. Успадковані assertions не послаблювалися: tests, які мають дійти до validation/authorization, отримують токен явно. `CookieClient.LoginAsync` не додає його глобально, негативні CSRF clients залишаються без токена.

Під час розробки один новий Production-тест виявив зайву CORS metadata за відсутнього middleware; виправлено condition за environment, після чого весь suite пройшов. Первинний невдалий run не видається за PASS. Окремі три тести навчального AntiforgeryDemo також пройдено; вони не включені до 141 тесту власного проєкту.

## 5. Findings summary

| ID | Актив / вплив | Ймовірність і передумови | Пріоритет | Статус / retest |
|---|---|---|---|---|
| F-01 | Намір власника змінити Incident / сеанс не підтверджувався request token | Auth cookie; прямим HTTP доведена відсутність перевірки, browser exploitability залежить від CORS/SameSite/типу запиту | P2, першим | Fixed у working tree; REG-CSRF-01 + 10 perimeter cases |
| F-02 | Політика захисних headers зникала на 500 | Контрольована недоступність БД, публічний login; safe JSON не містить відбитого HTML | P3, другим | Fixed у working tree; Production 500 + header/trace retest |

Обидві знахідки доведені на власному проєкті до fix. Кількість не збільшується за рахунок контролів, які від початку працювали. Третю знахідку поза checklist або рівень «Відмінно» не заявлено.

## 6. Detailed findings

- [F-01: відсутня antiforgery-перевірка](findings/F-01-missing-antiforgery.md).
- [F-02: заголовки error path](findings/F-02-error-response-headers.md).

Картки містять preconditions, reproduction, impact/likelihood, root cause, fix, retest, positive regression, residual risk і посилання на власні докази. Початковий JSON — [baseline-probes.json](evidence/lr06/baseline-probes.json).

## 7. Regression results

| Успадкований / новий ID | Перевірка й негативний результат | Позитивний результат / доказ |
|---|---|---|
| T-01/T-02, IncidentCreationTests | Invalid DTO → domain 400 з очікуваним errors field і відсутнім записом; CSRF token наявний | Valid create 201, server owner/id/status; [141-test summary](evidence/lr06/test-summary.json) |
| T-04/T-05, S-02, IncidentSearchTests | Контрольний SQLi input не повертає unrelated records; invalid sort → 400 | Легітимні запити, literal metacharacters, стабільний порядок/Take50 |
| A-01–A-07, AuthenticationSecurityTests | Anonymous mutations → 401; wrong/unknown login узгоджені | Реєстрація, login, профіль, logout, flags; старі ID збережені |
| A-02–A-12, AuthorizationSecurityTests | Foreign/nonexistent → 404; Reporter triage → 403 без зміни стану | Owner PUT/DELETE, Morgan New → Triaged, history actor; [live HTTP](evidence/lr06/legacy-http-run.txt) |
| REG-XSS-01 | Description/comment marker не виконується; Bob не читає foreign record | Plain complex text, DOM children=0, Alice/Morgan read; [fixed.json](evidence/lr06/browser-lr05/fixed.json), 11 browser cases |
| REG-CORS-01 | Origin 4174 response unreadable; заборонений preflight не допускає actual GET | 4173 дозволений; cookie/session 200, anonymous 401; [cors-fixed.json](evidence/lr06/browser-lr05/cors-fixed.json), 9 cases |
| REG-CSRF-01 | Missing token → 400, повторний GET незмінний | Get token → PUT 204 → повторний GET змінений; [browser-csrf.json](evidence/lr06/browser-csrf.json) і HardeningSecurityTests |
| REG-CSRF perimeter | 5 routes × missing/invalid = 10 випадків; DB snapshot незмінний, logout не завершив session | Valid pair, principal binding, no-cookie rejection; xUnit |
| LoginRateLimit_* | 10/60s default → 429; підроблений forwarding header quota не скидає | 3/3s test window → recovery; login 204, health не обмежений |
| REG_RATE_SEARCH_* | 3 запити одного principal → наступний 429, друга Alice cookie ділить quota; anonymous spoofed header не обходить quota | Bob independent, Alice після 3.5s відновлюється; detail authz лишається 200/404 |
| Headers/CSP | Unexpected 500 має CSP/nosniff/referrer; external image і foreign embedding blocked | 4 сторінки без JS/CSP regression; [csp.json](evidence/lr06/browser-lr05/csp.json), 6 cases |
| Runtime/log/audit | Reporter transition denied, no state change; safe 500 зі trace | Morgan success/history; profile recovery; [runtime-checks.json](evidence/lr06/runtime-checks.json) |

Після suite окремо пройшли runtime та браузерні сценарії. Новий браузерний harness містить 5 case groups: register, auth-page F5/create, REG-CSRF-01, browser-page navigation/F5/create, logout/new principal. Наявність request header збережено як boolean без значення токена. Результати не є перевіркою майбутнього merge commit — її потрібно повторити після прийняття Git-стану.

## 8. Hardening controls

### Периметр mutations

Зіставлено source routes, фактичний Development OpenAPI та product client calls. OpenAPI містить усі 7 наведених routes; параметр `{id}` у JSON відповідає source constraint `{id:guid}`. GET/HEAD/OPTIONS не змінюють business state; token GET лише видає службову antiforgery cookie/token.

| ID | Method + route | Джерело / розбіжність | Зміна стану | Cookie auth / browser | Antiforgery | Regression |
|---|---|---|---|---|---|---|
| SC-01 | POST /api/incidents | Lab02Endpoints, OpenAPI, auth/browser forms | Створення Incident | так / так | explicit filter | REG-CSRF perimeter/create, T-01/02 |
| SC-02 | PUT /api/incidents/{id} | Source/OpenAPI; DISCREPANCY: окремої UI edit кнопки немає, API лишається доступним | Назва/опис | так / так | explicit filter | REG-CSRF-01, A-08a |
| SC-03 | DELETE /api/incidents/{id} | Source/OpenAPI; DISCREPANCY: UI кнопки немає | Видалення запису/children | так / так | explicit filter | perimeter/delete, OwnerCanDeleteOwnFixture_* |
| SC-04 | POST /api/incidents/{id}/triage | Source/OpenAPI; DISCREPANCY: UI moderation кнопки немає | New → Triaged + history/audit | так / так | explicit filter | perimeter/triage, A-06/A-07 |
| SC-05 | POST /api/auth/logout | Source/OpenAPI, auth UI | Завершення cookie-сеансу | так / так | explicit перед SignOutAsync | perimeter/logout, A-06 logout |
| SC-06 | POST /api/auth/login | Source/OpenAPI, auth UI | Видача auth cookie | ні / так | absent, explicit exclusion | S-01/login tests, browser login |
| SC-07 | POST /api/auth/register | Source/OpenAPI, auth UI | Створення account | ні / так | absent, explicit exclusion | T-01/T-02 registration, browser register |

Розбіжності API/UI узгоджено цією документацією: відсутність кнопки не вилучає route з периметра. PATCH, batch, upload або адміністративних mutation routes у фактичному baseline немає. Усі cookies, що авторизують зміну, перевіряються standard antiforgery; login/register не називаються автоматично захищеними. Для їхнього подальшого захисту потрібний anonymous bootstrap і окреме рішення про scope.

### Antiforgery contract

`GET /api/auth/csrf-token` вимагає login, повертає request token у JSON та за потреби `SecureLab.Antiforgery` cookie. Header — `X-CSRF-TOKEN`. Cookie: HttpOnly, SameSite=Strict, SecurePolicy SameAsRequest у Development / Always у Production. Відповідь no-cache/no-store. Token route не відкривається teaching CORS. Filter викликає ValidateRequestAsync до handler; для missing/invalid пари valid-shape mutation повертає `400`, type `urn:securelab:csrf`.

Спільний `csrf.js` підключений перед page script на auth і browser pages; на index mutations немає. Lazy issuance працює після navigation/F5; після успішного login/logout token очищується, для нового principal видається знову. Local/session storage порожні, auth cookie не копіюється у JS. Автоматичного повторення POST/PUT/DELETE після 4xx немає. Invalid request binding може завершитися framework 400 раніше за filter, але не виконує mutation.

### Control → ризик → фактична перевірка → межа

| Control | Ризик | Перевірка | Межа |
|---|---|---|---|
| Safe DOM + CSP enforcement | Виконання введеного тексту / чужих ресурсів | REG-XSS-01, CSP browser 6 cases | CSP не замінює textContent; policy не послаблена |
| nosniff / no-referrer | MIME confusion / referrer disclosure | Header matrix нижче, F-02 retest | Потрібний правильний Content-Type; secrets не можна класти в URL |
| frame-ancestors 'self' | Чуже embedding/clickjacking | Foreign frame blocked; власний lr04 iframe працює | Same-origin embedding свідомо потрібне успадкованій probe page |
| Exact Development CORS | Читання cross-origin response | 4173 allowed, 4174 denied, actual browser preflight | Не authentication і не CSRF; localhost ports не доводять SameSite behavior |
| Authentication cookie | Сеанс і credential handling | HttpOnly/Lax/Secure tests; реальний login/logout | HTTP localhost не доводить TLS |
| Antiforgery | Cookie mutation без валідної пари | Missing/invalid denial + незмінність + valid success | Не захищає від XSS/IDOR/login-CSRF |
| Login / search quotas | Повторне зловживання | 429, Retry-After, partitions/recovery | In-memory, shared IP/NAT, не authorization |
| Safe ProblemDetails/trace | Disclosure внутрішніх details | Production 500 + logger trace match | Захист логів і deployment залишаються окремими задачами |

### Headers: фактичні класи відповідей

| Клас / route | Profile і client | Status | Перевірені значення |
|---|---|---|---|
| Static GET / | Production Kestrel + TestServer | 200 | nosniff, no-referrer, CSP |
| API GET /api/me | Production HTTPS TestServer | 200 | nosniff, no-referrer, CSP |
| GET /api/auth/csrf-token | Production HTTPS TestServer; Development Kestrel | 200 | ті самі + no-cache/no-store |
| Domain validation POST /api/incidents | Production HTTPS TestServer з valid CSRF | 400, errors.title | nosniff, no-referrer, CSP |
| Resource GET /api/incidents/{unknown} | Production HTTPS TestServer | 404 | nosniff, no-referrer, CSP |
| Unexpected login error | Production Kestrel, недосяжна БД | 500 | nosniff, no-referrer, CSP; тільки type/title/status/traceId |

Повна CSP: `default-src 'self'; script-src 'self'; style-src 'self'; connect-src 'self'; img-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'self'`. Для Scalar у Development збережено окремий виняток. Token Set-Cookie та додатковий X-Frame-Options при першій видачі не видаються за загальний header усіх responses.

### Rate limiting

Login збережено: 10 requests / 60s за transport IP, queue=0, 429 і Retry-After. Search: 60 requests / 60s, queue=0; authenticated partition `user:<verified NameIdentifier>`, anonymous — `ip:<RemoteIpAddress>` або фіксований unknown. Не довіряємо query client, X-Demo-UserId чи X-Forwarded-For. Порядок: routing → Development CORS → authentication → authorization → rate limiter. Тестові PostConfigure 3/3s та 1/60s ізольовані від runtime defaults. `SearchRateLimitPolicy.OnRejected` не використовує login-specific повідомлення/подію.

### Development / Production і транспорт

| Властивість | Development | Production |
|---|---|---|
| Конфігурація БД | Явна навчальна Development configuration | Переданий environment connection override |
| Reset | Дозволений лише при AllowReset=true | Заборонений, перевірено exception до зміни БД |
| OpenAPI / Scalar | OpenAPI JSON доступний; Scalar dev-only | OpenAPI 404; Scalar path повертає SPA HTML, не API reference |
| Teaching CORS | Exact origin localhost:4173 | Відсутній |
| Cookie SecurePolicy | SameAsRequest | Always |
| Error handler | Safe ProblemDetails | Safe ProblemDetails, без stack trace у HTTP |

У launchSettings лише HTTP localhost:5080; UseHttpsRedirection/UseHsts відсутні. Reverse proxy не налаштований. Це межа локального стенда, а не доказ production transport safety. HSTS на loopback не додавали заради header; справжній HTTPS/TLS termination/HSTS потребує окремої deployment-перевірки.

## 9. Dependency and secret audit

### Залежності

SDK 10.0.401; 04.11.2026 виконано `dotnet restore SecureLab.sln` та `dotnet package list --vulnerable --include-transitive`, обидві команди exit 0. Джерело `https://api.nuget.org/v3/index.json`; фактичний вивід для обох проєктів повідомляє відсутність known vulnerable packages. Рішення ґрунтується на тексті audit і lockfile graph, не тільки exit code. [Повний безпечний вивід](evidence/lr06/dependency-audit.txt).

| Частина graph | Версії / обсяг | Advisory / рішення |
|---|---|---|
| API direct | Identity.EF, OpenApi, EF Core/Design/Relational 10.0.10; Microsoft.OpenApi 2.7.5; Npgsql.EF 10.0.3; Scalar 2.16.16 | 8 direct, 22 transitive; known advisories не повідомлені; залишити версії |
| Tests direct | coverlet 6.0.4; Mvc.Testing 10.0.10; Test.Sdk 17.14.1; xunit 2.9.3; runner 3.1.4 | 5 direct, 56 transitive; known advisories не повідомлені; залишити версії |
| Update / suppression | Не застосовано | Немає підстав для blind major update, NuGetAuditSuppress або широкого NU19xx suppression |

Межа: known advisories цього джерела на дату перевірки. Unknown vulnerabilities, повна досяжність усіх library APIs та supply-chain компрометація не доведені відсутніми. За появи advisory власник перевіряє graph (`dotnet nuget why` для конкретного пакета), умови, досяжність, update/mitigation і повторює regression.

### Секрети й конфігурація

[153 контекстні рішення](evidence/lr06/secret-match-review.md), [машинний scan + SHA-256](evidence/lr06/secret-scan.json), [вхідний tracked список](evidence/lr06/tracked-file-list.txt). Використано `password|passwd|secret|api.?key` case-insensitive. Кожен match класифіковано: назви полів/ключів/пакетів, environment/runtime значення, синтетичні тестові markers або accepted study-only configuration. Реальних збережених credentials у перевіреному тексті не виявлено. 18 PNG не сканувалися OCR; висновок обмежений зазначеними методами.

`.env.example` містить лише порт; `.gitignore` зберігає safe template й виключає локальну конфігурацію/ключі/logs. Відомі credentials у Development config, compose та design-time factory узгоджені й дозволені лише для loopback study DB за README. Вони не вилучені механічно. Для реального leak потрібні revoke/rotation та перевірка доступних копій; видалення current line не відкликає credential. Історію Git не переписували.

### Logs та audit trail

Шість сценаріїв перевірено у локальних неочищених console logs поза Git: successful login, failed login, denied action, validation error, unexpected error, privileged transition. У первинних sink-файлах пораховано точні seed/wrong-password/видані request token значення, cookie header patterns та X-CSRF-TOKEN: **0 збігів у кожній категорії**. Потім експортовано лише короткі безпечні excerpts; видалення секретів із копії не використовувалось як доказ їхньої відсутності.

Reporter triage: `403`, повний прочитаний ресурс незмінний; audit `subject/action/resource/result=denied/time/trace`. Morgan: `204`, New → Triaged, history має verified actor; після успішного SaveChanges додано `incident.transition ... result=success`. History є persistent transactional trail, console event — додатковий diagnostic/audit view. Full user input не конкатенується в повідомлення; поля події — серверні Guid/enum/time, тому raw-body log injection не додається.

Production response trace `00-647e02784e7b76d5fac551ca54795552-f640dcd3d103246d-00` зіставлено з logger `TraceId:647e02784e7b76d5fac551ca54795552`. [runtime-checks.json](evidence/lr06/runtime-checks.json) містить safe ProblemDetails і три рядки ExceptionHandlerMiddleware, без stack trace/connection string. Межа висновку — ці запущені сценарії та sinks.

## 10. Residual risks

| Ризик / обмеження | Наявне обмеження ризику | Власник / наступна дія |
|---|---|---|
| HTTP local profile, deployment TLS/HSTS не перевірено | Loopback-only навчальний стенд | Власник deployment: HTTPS termination, Secure cookies, redirect/HSTS на цільовому host до публікації |
| Login-CSRF / account creation поза token perimeter | JSON input, cookie policy, вузький CORS; повний захист не заявляється | Власник API: окремий anonymous bootstrap і regression при розширенні scope |
| Public search / unscoped severity summary | Лише штучні дані локального baseline | Власник продукту: визначити допустимість disclosure й узгодити контракт до deployment |
| In-memory quota та NAT | Один API instance; verified principal partition для authenticated search | Власник API: shared quota лише за реальною distributed потребою; Redis тут не додавався |
| Конкурентні triage/active-title updates | Server-side authorization і перевірка поточного state | Власник API: окремі concurrency/transaction тести перед multi-instance usage |
| Regex не виявляє всі секрети, images/history/CI поза перевіркою | Контекстний огляд конфігурації, environment secrets, обмежений експорт логів | Власник repo: адресне розширення огляду за підозри; rotation при реальному витоку |
| Нові dependency advisories після дати аудиту | Lockfiles, повторювана audit команда | Власник repo: повторити audit перед release, оцінити кожен результат |

## 11. Conclusion

Технічні перевірки обраного рівня «Добре» пройдено у робочому дереві: дві виправлені own-project findings, повний regression pack, browser flow, profile/error, dependency/configuration/log review та модель загроз. Перевірені вихідні файли перелічено у [manifest](evidence/lr06/verified-source.sha256). Це не заява про завершений захист або прийнятий Git release.

Git операції виконує лише користувач. Попереду — огляд diff, commit, прийнятий merge за процедурою методички, повторна регресія цього стану й annotated `v1.0.0` після PASS. Remote visibility/push/Moodle не вважаються виконаними без підтвердження. Формат PDF свідомо не створено на прохання користувача.

## 12. Evidence appendix

- Старт: [restore](evidence/lr06/baseline-restore.txt), [build](evidence/lr06/baseline-build.txt), [reset summary](evidence/lr06/baseline-reset.txt), [124 tests](evidence/lr06/baseline-tests.txt), [до fix](evidence/lr06/baseline-probes.json).
- Поточний код: [Release build](evidence/lr06/final-build.txt), [141 tests](evidence/lr06/final-tests.txt), [поіменні результати](evidence/lr06/test-summary.json), [SHA-256](evidence/lr06/verified-source.sha256).
- Runtime: [profile/500/headers/OpenAPI/log/audit](evidence/lr06/runtime-checks.json), [команда/result](evidence/lr06/runtime-run.txt).
- Browser: [CSRF/UI](evidence/lr06/browser-csrf.json), [safe text screenshot](evidence/lr06/browser-csrf-safe-text.png), [XSS](evidence/lr06/browser-lr05/fixed.json), [CORS](evidence/lr06/browser-lr05/cors-fixed.json), [CSP](evidence/lr06/browser-lr05/csp.json).
- Audit: [NuGet](evidence/lr06/dependency-audit.txt), [контекстні рішення](evidence/lr06/secret-match-review.md).
- Команди повторення наведено нижче.

### Команди повторення

Усі команди виконуються з кореня `starter-project`. Потрібні .NET 10 stable, Docker, локальна PostgreSQL `127.0.0.1:54329`, Python; для browser harness — Node.js, Playwright і Chrome/Chromium. `SeedUsers__Password` задається поза Git за процедурою README. Перед reset/tests API зупиняють; reset очищує лише перевірену навчальну БД.

```powershell
dotnet restore SecureLab.sln --locked-mode
dotnet build SecureLab.sln -c Release --no-restore
docker compose --env-file infra/.env.example -f infra/compose.yaml up -d --wait
dotnet run --project src/SecureLab.Api -c Release --no-build -- --reset-database
dotnet test SecureLab.sln -c Release --no-build --no-restore
dotnet package list --vulnerable --include-transitive
python tests/http/lab06-runtime.py
```

Runtime script сам запускає й завершує свої Kestrel processes на вільному порту 5080, перевіряє Production/Development, безпечну 500 та logs. Порт 54399 має лишатися недоступним. Raw logs зберігаються поза Git; до evidence експортується очищений JSON.

Для браузерних перевірок окремо запустіть Development API та два probes (кожна команда у власному терміналі):

```powershell
dotnet run --project src/SecureLab.Api -c Release --no-build
python -m http.server 4173 --bind localhost --directory demonstrations/cors-probe
python -m http.server 4174 --bind localhost --directory demonstrations/cors-probe
```

У терміналі harness задайте той самий seed environment. За потреби `LAB05_PLAYWRIGHT_PATH` указує на встановлений Playwright index.js, `LAB05_CHROME_PATH` — на Chrome executable.

```powershell
$env:LAB05_OUTPUT_DIR = Join-Path (Get-Location) 'docs/evidence/lr06/browser-lr05'
$env:LAB06_OUTPUT_DIR = Join-Path (Get-Location) 'docs/evidence/lr06'
node tests/browser/lab06-regression.cjs
node tests/browser/lab05-regression.cjs fixed
node tests/browser/lab05-regression.cjs cors-fixed
node tests/browser/lab05-regression.cjs csp
python tests/http/lab-04-verify.py
```

За вичерпання login quota дочекайтеся наступного вікна, не вимикайте limiter. Browser harness залишає синтетичні fixtures/account; перед наступним чистим suite виконайте свідомий reset після збереження доказів. Git-команди виконує тільки власник; release tag ставиться після перевірки прийнятого commit.

## Декларація використання ШІ

Використано OpenAI Codex: аналіз методички й коду, Code assistance
