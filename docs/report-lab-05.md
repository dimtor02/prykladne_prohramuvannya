# Лабораторна робота №5. Безпека браузера: XSS, DOM і межі CORS

Автор: Торонченко Дмитро, КБ-41. Дата: 04.11.2026.
Спільний сценарій 2-A «Трекер інцидентів». Рівень: **«Добре»**.
Формат звіту Markdown погоджений студентом. Базовий стан — власний v0.4.0.

## Мета і середовище

Простежити недовірений збережений текст до DOM, відтворити локальний нешкідливий
stored XSS, виправити обидва sinks та дослідити CORS/preflight/cookies, CSP і
browser headers. Formatted HTML для Description і comment text не дозволений.

Windows, .NET SDK 10.0.401, PostgreSQL 18.4 у Docker, Chrome 156.0.8078.80.
API і штатний клієнт: localhost:5080; probes: localhost:4173 та localhost:4174.
Досліди виконані лише на власному loopback-стенді зі штучними акаунтами.

## DEL-01: результат роботи

[Карта двох потоків](source-to-sink.md) і [доказовий комплект](browser-security-evidence.md)
містять source, request, validation/mapping, storage, JSON response, client transform
і exact sink, а також REG-XSS-01 та REG-CORS-01 з командами повторення.

| До | Першопричина | Виправлення | Retest | Залишковий ризик |
|---|---|---|---|---|
| Description marker виконувався в Alice/Morgan | Збережений Description → innerHTML | renderPlainText → textContent | Той самий запис без CSP: лише текст, marker null; з enforcement також PASS | Інші contexts потребують окремих правил |
| Public comment marker виконувався при читанні seed | comments[].text → paragraph.innerHTML | Той самий safe helper | Текст збігається з JSON; без img і дублювання після повторного load | Seed є моделлю введення, не реальним POST comments |
| Обидва cross-origin probes не читали response | Policy не було | Exact Development allowlist 4173 | 4173 читає 200, 4174 блокується; direct HTTP отримує обидва 200 | CORS не є авторизацією або CSRF-захистом |
| Для cookie response бракувало ACAC | include без AllowCredentials | AllowCredentials лише для точного 4173 у Development | Alice читає список з 4173; 4174 не читає; без входу читається 401 | localhost-порти не моделюють усі cross-site випадки |
| Browser controls не обмежували ресурси CSP | CSP ще не налаштована | Enforcement self policy, object-src none, frame-ancestors self | Основні сторінки працюють; img з 4173 і cross-origin frame заблоковані | Development Scalar має окремий виняток |

Короткий safe diff для двох sinks:

```diff
-document.querySelector("#description").innerHTML = current.description;
+renderPlainText(document.querySelector("#description"), current.description);
-paragraph.innerHTML = comment.text;
+renderPlainText(paragraph, comment.text);
```

Helper створює текстовий контекст: `element.textContent = text ?? ""`.
Дані в БД не вирізалися, не HTML-encode-илися на вході й не фільтрувалися regex.
Пряме порівняння `db-before.json` та `db-after.json` дало рівність для тих самих IDs.
API зберіг marker; змінився лише спосіб побудови DOM.

Позитивна регресія охоплює апостроф, подвійні лапки, `< >`, Unicode,
`<b>жирний</b> текст` і рівно 4000 символів Description. Morgan читає дозволений
ресурс, Bob отримує 404. Public comment …0005 видимий Alice/Morgan;
успадкований internal comment …0003 прихований від Reporter.

## Перевірки

- Release build: 0 errors / 0 warnings.
- Повний suite після фінальних source changes: **124 passed, 0 failed, 0 skipped**;
  завершено 04.11.2026 о 18:31:18 +02:00 (Київ). 113 успадкованих і 11 нових cases.
- Нові інтеграційні cases перевіряють simple CORS, allowed/disallowed preflight,
  protected anonymous 401 з правильними CORS headers, headers чотирьох сторінок,
  відсутність навчальної policy у Production.
- Реальний Chrome/Playwright: fixed, cors-fixed і csp — PASS після suite.
  Це DOM/network перевірки, не лише пошук слова innerHTML у файлі.
- Direct curl: HTTP 200 з Origin 4173 і 4174; ACAO=4173/ACAC=true тільки для 4173.
- [Фінальний suite](evidence/lr05/final-tests.txt), [counters](evidence/lr05/test-summary.json),
  [browser DOM](evidence/lr05/fixed.json), [CORS Network](evidence/lr05/cors-fixed.json),
  [CSP resources/violations](evidence/lr05/csp.json), [curl headers](evidence/lr05/direct-http.txt).
- [Відтворення](../tests/browser/README.md): пароль задається локально через оточення;
  reset очищує навчальні записи, тому suite запускають до створення browser fixtures.

Автоматизація є допоміжною до обраного «Добре». Увесь рівень «Відмінно», зокрема
послідовні report-only/enforcement докази, тут не заявляється.

## CORS, CSP та межі інших контролів

XSS виник через інтерпретацію дозволених plain-text даних як HTML у браузері.
Авторизація ЛР4 правильно дозволила Morgan перегляд, але не визначала DOM context.
Після fix той самий збережений рядок став текстом.

CORS оголошує, чи може код сторінки іншого origin читати response.
На 4174 simple request, зокрема з cookie, доходив до API, хоча браузер response
не відкрив. Для preflighted request спочатку перевіряються origin/method/header;
після невдалого OPTIONS actual GET не надсилався. OPTIONS 204 без allow headers
не є успіхом. Browser-initiated preflight зафіксовано через CDP Network.

CSRF стосується небажаної дії із credentials користувача. Він потребує окремого
контролю на cookie mutations; CORS не замінює antiforgery. Цю частину відкладено
до ЛР6, як передбачено курсом. Authorization вирішує, чи principal може виконати
операцію над конкретним ресурсом; ці перевірки залишилися серверними.

CSP обмежує джерела ресурсів і виконання коду, але не робить innerHTML безпечним.
Safe DOM доведено окремо до її ввімкнення. Основний клієнт використовує власні
JS/CSS/API, тому self достатньо; probes не додавалися до connect-src.
Frame-ancestors self зберігає попередній same-origin helper ЛР4 і блокує інші origins.
No-referrer відповідає клієнту без потреби передавання referrer. Nosniff вимагає
додатково правильного Content-Type. Development Scalar — окремий явно описаний
виняток, а не підстава дозволяти unsafe-inline основному клієнту.

## DEL-02: Git і подання

| Поле | Значення / стан |
|---|---|
| Scaffold release | lab-05-start-v1 |
| Base / entry_parent | 0416fb1378cec41bc08d67d34d814e0fc498d087 |
| Vulnerable commit | 3f7f726d6b6b2de26172497ce2fa77beffe11a7d |
| Робоча гілка | lab/5-browser-xss |
| Fixed commit |
| v0.5.0 | Не створено; дозволено лише після перевірки fixed commit |
| main | Перенесення verified fixed state ще не виконано |
| Git-подання | Погоджений приватний remote або перевірений bundle; ще не виконано |

До comment PoC додано лише seed fixture з методички у робоче дерево поверх
vulnerable commit. Чистий scaffold commit його не містить: для повторення
другого flow треба додати ту саму seed-зміну без safe DOM/CORS/CSP змін.

Користувач виконує всі Git-команди сам. Codex не перемикає гілки, не створює
commits/tags і не виконує push. Після fixed commit фактичний hash буде внесено
до доказів окремою документаційною зміною без пересування вже перевіреного тегу.

## DEL-03: контрольні відповіді

1. **Чому БД не робить value довіреним?** Вона зберігає текст користувача;
   подальший HTML parser усе одно може інтерпретувати цей текст як код/розмітку.
2. **Source/transformation/sink у нашому flow?** Textarea → JSON → Trim і length
   validation → incidents.description → response.description → innerHTML до fix;
   після fix — textContent. Повна карта містить також коментар.
3. **Stored/reflected/DOM-based?** Stored зберігається й виконується під час пізнішого
   читання; reflected повертається з поточного request; DOM-based описує небезпечне
   клієнтське перетворення. Наш stored flow має клієнтський DOM sink.
4. **Чому блокування script недостатнє?** Канонічний marker використовує img/onerror,
   а небезпеку визначає HTML context, не одне слово.
5. **Коли textContent або sanitizer?** Plain text відображаємо textContent;
   підтримуваний sanitizer потрібний лише за явної вимоги дозволеного formatted HTML.
6. **Чому не regex sanitizer?** HTML parsing, вкладені contexts, attributes і URL
   мають складні правила; регулярний вираз не є повним HTML parser і policy engine.
7. **Origin?** Scheme + host + port. Path/query не входять. localhost:4173 та :5080
   різні origins через port, хоча це може бути один site.
8. **Хто і що надсилає в preflight?** Браузер автоматично надсилає OPTIONS з Origin,
   Access-Control-Request-Method та Access-Control-Request-Headers. У досліді GET
   і x-lab-cors-probe; ручний OPTIONS не підміняє browser evidence.
9. **Хто оголошує і застосовує CORS?** API повертає Access-Control-* headers,
   браузер перевіряє їх перед відкриттям response коду сторінки.
10. **Чому curl читає?** Він не виконує браузерну SOP/CORS enforcement, хоча сервер
    застосовує до нього звичайну authentication/authorization.
11. **Чому CORS не authorization/CSRF?** Він керує читанням response за origin,
    не правом користувача на об'єкт і не загальним блокуванням небажаних cookie-дій.
12. **Чому не wildcard із credentials?** Credentialed response потребує конкретного
    дозволеного origin та ACAC=true; дозвіл усім не є обмеженим trust boundary.
13. **Межа CSP?** Вона обмежує джерела/виконання; не виправляє unsafe sink.
    Безпечний DOM перевірено без CSP, а потім ще й під enforcement.
14. **Nosniff/referrer/frame-ancestors?** Зменшують MIME sniffing, витік referrer
    та небажане embedding відповідно; не замінюють коректний MIME, safe DOM і access policy.

## Межі перевірки і залишковий ризик

Локально перевірені лише наведені flows та матриця обраного рівня. Успадкований
search доступний без входу та без owner scope; severity-summary агрегує всі записи.
Їхнє виправлення не включено до ЛР5. CSRF, загальна concurrency/version policy,
production deployment, повний dependency audit і cross-site cookie matrix не виконані.
Початковий прогноз CORS окремою таблицею до першої зміни не збережено;
таблиця DEL-01 чесно містить фактичні спостереження. Докази не підміняються прогнозом.

## Декларація використання ШІ

Використано OpenAI Codex: аналіз методички й коду, Code assistance
