# F-01. Відсутня antiforgery-перевірка cookie mutations

**Статус:** Fixed у перевіреному робочому дереві; Git commit очікується.

**Актив:** право користувача керувати власним Incident і cookie-сеансом.

**Компонент:** create/update/delete/triage/logout та браузерні виклики.

**Пріоритет:** P2.

## Передумови і межі огляду

Початковий commit `e8b62cf0665bbff5a6247a53db5751663a9c2e84`. Локальні API `localhost:5080`, PostgreSQL `127.0.0.1:54329`, Development, Alice/Reporter. Seed password передано через environment поза Git. Reset: `dotnet run --project src/SecureLab.Api -- --reset-database`, лише після зупинки API та звірення target.

## Відтворення

1. Увійти Alice, зберегти auth cookie у пам’яті cookie-aware HTTP-клієнта.
2. Створити окремий Low Incident коректним POST без antiforgery header: фактично `201`.
3. Прочитати опис, відправити PUT із новим описом без токена, прочитати знову.
4. Фактично PUT дав `204`, опис змінився з `Controlled local baseline record` на `Changed without a CSRF token`.
5. Видалити власний тимчасовий запис. Значення credentials не експортувати.

Expected security property: cookie-authenticated mutation без request token відхиляється до зміни стану. Фактичний результат до fix не відповідав цій властивості.

## Вплив і ймовірність

**Статус перевірки:** локально спостережено відсутність перевірки токена та зміну стану через прямий HTTP. Експлуатацію з довільного зовнішнього сайту не заявлено.

Cookie сама не підтверджує намір користувача. Реальна browser exploitability залежить від SameSite, типу запиту, origin/site та CORS; наявні Lax і exact CORS звужують умови. Ризик важливий для всього cookie-периметра, але не описується як доведений універсальний cross-site exploit. Виправлено першим, оскільки захищається зміна даних.

## Першопричина

У baseline не були зареєстровані antiforgery services, token route та перевірка на JSON/DELETE endpoints. RequireAuthorization перевіряв сесію та права, проте не token/cookie пару. CORS і SameSite не є цією перевіркою.

## Виправлення

- [AntiforgeryFilter](../../src/SecureLab.Api/Security/AntiforgeryFilter.cs): стандартний `IAntiforgery.ValidateRequestAsync`, точний `400 application/problem+json`, type `urn:securelab:csrf`.
- [Program](../../src/SecureLab.Api/Program.cs): `AddAntiforgery`, authenticated `GET /api/auth/csrf-token` через `GetAndStoreTokens`; teaching CORS не відкриває токен.
- Filter явно підключено до всіх п’яти cookie mutations, logout — до SignOutAsync. Login/register винесені в окреме scope-рішення.
- [csrf.js](../../src/SecureLab.Api/Client/csrf.js): same-origin, lazy token на поточній сторінці, пам’ять без storage, скидання після login/logout, без автоматичного повторення mutation.

## Retest

`dotnet test SecureLab.sln -c Release --no-build --no-restore`:

- 10 випадків `REG_CSRF_Perimeter_RejectsMissingOrInvalidToken_WithoutStateChange`: missing/invalid для create, PUT, DELETE, triage, logout → точний `400`; DB snapshot і session не змінені.
- `REG_CSRF_01_RealSession_DenialStateRead_Token_AllowedUpdate_StateRead`: відмова → повторний GET незмінний → token → PUT `204` → повторний GET зі зміненим описом.
- Старий токен після Alice logout/Bob login та request token без cookie відхилені.

## Positive regression

Валідні create/update/delete/triage/logout проходять; старі T/A тести отримують токен явно й досягають власної validation/authorization логіки. У Chrome перевірено форми register/login/create/logout, перехід auth → browser, F5, зміну користувача та обидва create flows. 141/141 integration tests пройдено.

## Залишковий ризик

Antiforgery не усуває XSS, IDOR, login-CSRF або конкурентні зміни. Authenticated token route не є anonymous bootstrap. Повторна mutation після відмови можлива лише як окрема дія користувача; клієнт не відтворює її автоматично.

## Доказ

[До fix](../evidence/lr06/baseline-probes.json), [тести](../evidence/lr06/test-summary.json), [browser flow](../evidence/lr06/browser-csrf.json), [інструкція повторення](../report-lr06.md#команди-повторення). Fixed commit буде встановлений за фактичним Git-виводом користувача.
