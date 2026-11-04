# Власна карта source-to-sink — ЛР5

Рівень: «Добре». Базовий vulnerable commit: `3f7f726d6b6b2de26172497ce2fa77beffe11a7d`.
Перед дослідом коментаря до робочого дерева додано лише виданий seed fixture у DbSeeder;
обидва sinks залишаються без змін. Факт normal flow: `evidence/lr05/normal.json`.
Guid marker-запису й фактичні JSON/DOM зберігаються у `evidence/lr05/vulnerable.json` після досліду.

## Incident.Description

| Крок | Файл та функція/DTO | Поле | Доказ і довіра |
|---|---|---|---|
| Source | Client/browser-lab.html, форма #edit | textarea[name=description] | Контроль користувача Alice, plain text |
| Request | Client/browser-lab.js, submit listener | POST /api/incidents, JSON description | Форма передає JSON.stringify, severity Low, дата UTC; owner не надсилається |
| Validation/mapping | Scaffolding/Lab02Endpoints.cs, CreateIncidentAsync | request.Description → description | Trim; required; до Trim максимум 4000; High/Critical вимагає 40 після Trim. Дужки не заборонені. Авторизація і валідація не роблять текст HTML-довіреним |
| Storage | Data/Entities/Incident.cs; Data/SecureLabDbContext.cs | incidents.description, максимум 4000 | EF Core зберігає текст; для marker без крайніх пробілів значення збігається з input |
| Response | Application/Incidents/IncidentQueries.cs, GetDetailsAsync; IncidentDetailsResponse | GET /api/incidents/{id}, description | ScopeRead перевіряє principal/owner; проєкція тексту без HTML-перетворень |
| Client transform | Client/browser-lab.js, load | response.json() → current.description | JSON декодується у string; JSON escaping не захищає HTML sink |
| Sink до | Client/browser-lab.js, load, #description | innerHTML = current.description | Недовірений збережений текст інтерпретується HTML parser; inline event може виконатися |
| Sink після (перевірено) | Той самий load | Спільний safe helper, textContent | Структуру визначає код; дані стають текстовим вузлом |

Гіпотеза: Alice зберігає Description через protected create; API повертає текст Morgan,
а innerHTML у браузері привілейованого глядача створює img/onerror. Це stored XSS
із DOM sink; stored описує походження, клієнтське перетворення — місце інтерпретації.
Bob за матрицею ЛР4 має отримати 404 і не побачити цей текст.

## IncidentComment.Text

| Крок | Файл та функція/DTO | Поле | Доказ і довіра |
|---|---|---|---|
| Source | Data/DbSeeder.cs, SeedAsync, Lab05Marker | Виданий canonical marker, fixture …0005 | Штучний seed моделює автора коментаря; у продукті текст контролював би користувач |
| Request | Seed/reset, окремого POST comments немає | Incident=aliceIncident, Author=morgan, IsInternal=false | Нового endpoint не додано; існуючі comments збережено |
| Validation/mapping | DbSeeder → EF entity | IncidentComment.Text | Seed fixture не проходить create DTO інциденту; це треба відрізняти від першого flow |
| Storage | SecureLabDbContext, IncidentComment | incident_comments.text | Збережений користувацький текст не стає довіреним HTML |
| Response | IncidentQueries.GetDetailsAsync, IncidentCommentResponse | comments[].text | Parent-authorized read; internal comments фільтруються, тестовий public comment доступний Alice/Morgan |
| Client transform | browser-lab.js, load, for…of | comment.text | Дані з JSON без очищення/HTML encoding |
| Sink до | load, paragraph.innerHTML | #comments > p | HTML parser створює дочірній img; marker діє незалежно від Description |
| Sink після (перевірено) | Той самий спільний helper | paragraph.textContent | Лише текст, replaceChildren прибирає попередній список |

## Інвентаризація власного клієнта

| Місце | Походження | Контекст | Рішення |
|---|---|---|---|
| browser-lab.js description/comments | Збережені значення користувача | HTML | Обидва innerHTML замінити |
| browser-lab.js title/status | Збережена назва / server status | textContent | Безпечний текст |
| browser-lab.js form description.value | Збережений текст | Властивість textarea | Дані не виконуються |
| app.js createTextElement, renderIncidentDetails | Title, description, authorDisplayName, comments, статус | textContent/createTextNode | Безпечні текстові sinks |
| auth-lab.js результати/профіль | Server JSON/errors | Текстові DOM sinks | Перевірити інвентаризацією; не вставляти HTML |
| cors-probe/probe.js output | API body/errors | textContent | Залишити безпечним |
| browser-lab.js fetch URL | Guid input | encodeURIComponent у локальному path | Origin фіксований; немає runtime href/src/code |

Formatted HTML не дозволено. Sanitizer не потрібний; regex/видалення слова script
не усуває HTML-контекст. Source-to-sink fix має зберегти input у БД/API й змінити лише DOM.

## Фактичний retest

Description: marker не виконується; текст API дорівнює DOM textContent, childElementCount=0. Для Alice і Morgan comments[].text збігаються з текстами p, без дочірньої розмітки; повторний load не дублює comments. Bob отримує 404. Докази: [без CSP](evidence/lr05/fixed-without-csp.json), [фінальний enforcement](evidence/lr05/fixed.json), [БД до](evidence/lr05/db-before.json), [БД після](evidence/lr05/db-after.json).
