# F-02. Втрата захисних заголовків на unexpected 500

**Статус:** Fixed у перевіреному робочому дереві; Git commit очікується.

**Актив:** узгоджена політика безпеки HTTP-відповідей клієнту.

**Компонент:** Program.cs, порядок middleware / response headers.

**Пріоритет:** P3.

## Передумови і межі огляду

Початковий commit `e8b62cf0665bbff5a6247a53db5751663a9c2e84`; Kestrel у Production, HTTP localhost:5080. Локальна контрольована недоступність БД: у первинному доказі порт 54328, у переносимому retest — порт 54399 з методички. Обидва — loopback, без зміни compose/даних БД. Login із вигаданим password дістається сховища до видачі cookie.

## Відтворення

1. Зупинити звичайний API.
2. Запустити Production без launch profile з connection override на недосяжний локальний порт.
3. Надіслати коректно сформований `POST /api/auth/login` із синтетичними credentials до вичерпання login quota.
4. Фактично: `500 application/problem+json`, безпечні type/title/status/traceId, але немає CSP, X-Content-Type-Options, Referrer-Policy, які присутні на звичайному `200`.
5. Зупинити broken profile, повернути нормальну БД, перевірити health/login.

## Вплив і ймовірність

**Статус перевірки:** спостережено локально на реальній HTTP-відповіді Kestrel.

Захисна політика залежала від шляху виконання. Для цього JSON error без відбиття недовіреного HTML прямий вплив обмежений; XSS або витік stack trace не доведені. P3 після F-01. Це підказаний checklist дефект, придатний для рівня «Добре», а не самостійна третя знахідка рівня «Відмінно».

## Першопричина

Middleware встановлював заголовки до `await next`, усередині exception handler. Обробка необробленого винятку очищувала відповідь, і заголовки губилися.

## Виправлення

[Program.cs](../../src/SecureLab.Api/Program.cs): middleware охоплює exception handler, реєструє `Response.OnStarting` та встановлює CSP/nosniff/referrer безпосередньо перед надсиланням відповіді. Наявний виняток для Development Scalar збережено. CSP клієнта не послаблена.

## Retest

`python tests/http/lab06-runtime.py` → exit 0. На контрольованому Production `500` присутні `nosniff`, `no-referrer` і початкова CSP. У тілі лише type/title/status/traceId. Середній сегмент W3C traceId відповідає TraceId у записі ExceptionHandlerMiddleware; у доказ внесено лише три безпечні рядки.

## Positive regression

Перевірено static HTML, успішний API JSON, token response, domain `400`, authorization/resource `404` і `500`. `Production_HeadersOnFiveResponseKinds_AndResetForbidden` пройдено. У Chrome чотири клієнтські сторінки працюють під CSP; зовнішнє зображення та cross-origin embedding заблоковані. Після broken profile нормальний health знову `200`.

## Залишковий ризик

Заголовки не замінюють правильний Content-Type, safe DOM, authorization, transport security. Production перевірено локально; TLS termination і HSTS на справжньому deployment не тестувалися.

## Доказ

[Baseline 500 без заголовків](../evidence/lr06/baseline-probes.json), [retest + trace + відновлення](../evidence/lr06/runtime-checks.json), [CSP у Chrome](../evidence/lr06/browser-lr05/csp.json), [інструкція](../report-lr06.md#команди-повторення).
