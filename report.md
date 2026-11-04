# Лабораторна робота № 1 — SecureLab

Запуск, дослідження та невелике розширення готової вебсистеми.
Цей Markdown-звіт призначено для перегляду разом із кодом на GitHub.
Word-звіт студент готує окремо. Скриншоти та локальні журнали до
репозиторію не додаються.

## 1. Ідентифікація проєкту

- Спільний baseline: 2-A «Трекер інцидентів».
- Рівень виконання: «Добре».
- Робоча гілка: `lab/1-system`.
- Основна гілка: `main`.
- Репозиторій: <https://github.com/dimtor02/lab1>.
- Фінальний тег: `v0.1.0` — ще потрібно створити після злиття та перевірок.
- Hash перевіреного фінального commit: внести після завершення Git-кроків.

## 2. Компоненти та досліджений маршрут

Браузерний клієнт написано на HTML/CSS/JavaScript. Сервер — ASP.NET Core
на C#, доступ до PostgreSQL — EF Core/Npgsql. БД працює локально в Docker Compose.

Досліджено GET деталей інциденту
`/api/incidents/20000000-0000-0000-0000-000000000003`:

```text
натискання картки → loadIncidentDetails(id) у Client/app.js
→ GET /api/incidents/{id}
→ IncidentEndpoints.GetDetailsAsync
→ IncidentQueries.GetDetailsAsync
→ SecureLabDbContext.Incidents → PostgreSQL incidents
→ IncidentDetailsResponse → JSON → renderIncidentDetails → textContent
```

Студент самостійно зіставив Network Headers/Response з read-only SELECT
у PostgreSQL. ID, назва, description, Low та New збіглися. На сторінці
опис із буквальним `<script>` відображався як текст.
Для відсутнього UUID отримано 404 Not Found, application/problem+json
та traceId конкретного запиту. Рішення про 404 приймає endpoint,
коли query повертає null.

Межі довіри:

| Межа | Дані та контроль |
|---|---|
| Браузер → API | Клієнт контролює id/status; сервер перевіряє формат UUID та allowlist статусів |
| API → PostgreSQL | Значення передаються параметризованим LINQ/EF Core запитом |
| БД/API → DOM | DTO обмежує поля, textContent/createTextNode показують дані як текст |

Докладна карта: [docs/architecture.md](docs/architecture.md).

## 3. Реалізоване розширення

Додано GET `/api/incidents/severity-summary` з необов'язковим query
parameter `status`. Request body відсутній. Успіх — 200 application/json,
масив об'єктів із лише `severity` (string) та `count` (integer).

Обрана остаточна політика: завжди всі чотири рівні у порядку
Low → Medium → High → Critical. Рівні без інцидентів мають count: 0.
Фільтр застосовується перед GroupBy. PostgreSQL виконує Count;
ToListAsync отримує лише агрегат, після чого код доповнює його нулями.
Схему БД, entities та seed для розширення не змінено.

Allowlist status: New, Triaged, InProgress, Resolved, Closed, з точним
написанням. Unknown, числове та порожнє значення дають 400
Validation Problem Details з помилкою поля status. Відсутність параметра
означає всі статуси.

Клієнт має кнопку підсумку, стани завантаження, нульового результату та
безпечної помилки. Рядки створюються через DOM API та textContent.
Загальний лічильник — сума count, а не кількість категорій.
Для суми 0 показано «Даних немає. Інцидентів: 0.» і всі рядки з нулями.
Структурований log містить Status і GroupCount без чутливих даних.

## 4. Фактичні перевірки

Передумови: локальна PostgreSQL healthy, навчальний seed, API у Development.

| ID | Запит/дія | Фактичний результат |
|---|---|---|
| До зміни | Summary у початковій копії | 501 Problem Details |
| T-01 | GET /health | 200, status ready |
| T-02 | GET /api/incidents?status=Triaged | 200, один інцидент Medium/Triaged |
| T-03 | GET /api/incidents?status=Resolved | 200, [] |
| T-04 | Details для невідомого UUID | 404 Problem Details |
| T-05 | List status=Unknown | 400, errors.status |
| T-06 | Summary без status | 200; Low:1, Medium:1, High:1, Critical:0 |
| T-06a | Summary status=Triaged | 200; Low:0, Medium:1, High:0, Critical:0 |
| T-06b | Summary status=Resolved | 200; усі чотири count:0 |
| T-06c | Summary status=Unknown | 400, errors.status |
| T-07 | Browser-flow підсумку | Перевірено успіх, loading, empty та fixed safe error у попередній версії; особисту UI-перевірку фінальних нульових груп ще потрібно підтвердити |
| Reset | Відновлення навчальної БД | Студент виконав reset; успішне очищення й seed, після запуску API відновлено три інциденти та підсумок |

Старі скриншоти підсумку показують попередню політику лише наявних груп.
Їх збережено як історичні матеріали; фактичний поточний контракт із
нульовими групами перевірено HTTP-запитами та інтеграційними тестами.

## 5. Відтворюваність та Git

Інструкція запуску: [README.md](README.md).
Ручні запити: [tests/http/incidents.http](tests/http/incidents.http).
Автоматизовані перевірки:

```powershell
dotnet test tests/SecureLab.Api.Tests/SecureLab.Api.Tests.csproj --configuration Release
```

Останній запуск поточного коду: 10 успішних, 0 невдалих, 0 пропущених.
Усі чотири baseline-тести та шість доданих summary-випадків пройшли.
Студент також самостійно виконав попередній запуск тестів із 10 успішними.

Переглянуто diff і список tracked paths: зміни стосуються коду, тестів
та документації; локальні скриншоти, журнали й build-артефакти поза commit.
Збереження у Git, merge у main, перевірки після merge та тег ще належить
завершити. Приватність remote та доступ викладача потрібно підтвердити.

## 6. Висновок

Реалізовано наскрізний підсумок інцидентів із перевіреним status,
мінімальним response DTO, SQL-агрегацією та безпечним DOM-виводом.
Досліджено details-маршрут від браузера до таблиці PostgreSQL і назад.
Технічні API-перевірки поточного коду успішні; до фінального подання
потрібно завершити зазначені UI- та Git-пункти й записати фінальний hash.

## 7. Використання генеративного ШІ

Використано OpenAI Codex для code assistance
