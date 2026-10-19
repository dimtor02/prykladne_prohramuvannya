# Карта архітектури ЛР 1

## Компоненти та відповідальність

| Компонент | Файли | Відповідальність |
|---|---|---|
| Браузер, Vanilla JavaScript | `Client/index.html`, `Client/app.js` | Подія форми, HTTP fetch, безпечний DOM |
| ASP.NET Core Presentation | `Presentation/Endpoints/IncidentEndpoints.cs` | Маршрутизація, allowlist status, HTTP 200/400/404 |
| Application | `Application/Incidents/IncidentQueries.cs` | LINQ-фільтр, агрегація, окрема проєкція DTO, структуроване журналювання |
| EF Core/Npgsql | `Data/SecureLabDbContext.cs`, `Data/Entities/Incident.cs` | Відображення Incident на таблицю incidents, виконання SQL |
| PostgreSQL у Docker Compose | `infra/compose.yaml` | Зберігання навчальних даних |

Шляхи Client/Presentation/Application/Data наведено від `src/SecureLab.Api/`.
Це один трирівневий моноліт, а не окремі сервери для кожного каталогу.

## Досліджуваний готовий маршрут details

```text
картка інциденту → loadIncidentDetails(id)
→ GET /api/incidents/20000000-0000-0000-0000-000000000003
→ IncidentEndpoints.GetDetailsAsync
→ IncidentQueries.GetDetailsAsync
→ Incidents.AsNoTracking().Where(id).Select(IncidentDetailsResponse)
→ SingleOrDefaultAsync → PostgreSQL incidents
→ Results.Ok(dto) → JSON → renderIncidentDetails
→ textContent / createTextNode
```

Маршрут `/{id:guid}` перевіряє формат UUID. Якщо UUID коректний, але query
повертає null, endpoint повертає 404 Problem Details. Це відсутній ресурс;
400 означає некоректний параметр. Details DTO має публічне ім'я автора,
але не ownerUserId чи email. Коментарі відбираються лише публічні.
Network підтверджує HTTP; відповідність таблиці перевіряють окремим read-only SQL.

## Новий маршрут summary

```text
summary-form → loadSeveritySummary()
→ GET /api/incidents/severity-summary[?status=Triaged]
→ IncidentEndpoints.GetSeveritySummaryAsync → allowlist
→ IncidentQueries.GetSeveritySummaryAsync
→ Incidents.AsNoTracking → optional Where(Status) → GroupBy(Severity)
→ Select(Severity, Count) → ToListAsync(cancellationToken)
→ доповнення нулями в явному порядку рівнів → IncidentSeveritySummaryResponse
→ Results.Ok → JSON → li.textContent
```

Контракт: GET без body, успіх 200 application/json, масив об'єктів із лише
`severity` (string) і `count` (integer). Політика: повний перелік рівнів; відсутні групи доповнюються count: 0.
Порожній набір дає Low/Medium/High/Critical із нулями. Порядок:
Low → Medium → High → Critical, незалежно від текстового зберігання enum.
Без status враховуються всі інциденти. Allowlist: New, Triaged, InProgress,
Resolved, Closed; написання точне, порожнє/числове/Unknown дає 400
Validation Problem Details з errors.status. Готовий list-flow не змінено.

Порядок та доповнення нулями формуються після отримання лише агрегату, а не всієї таблиці.
Шаблон структурованого log має параметри Status і GroupCount; описів,
credentials чи внутрішніх полів у ньому немає.
UI показує завантаження, результат, окремий порожній стан і фіксовану
безпечну помилку. Під час запиту кнопка вимкнена; finally відновлює її.

## Межі довіри

| Межа | Недовірені дані | Контроль |
|---|---|---|
| Користувач → браузер | Значення select можна змінити | UI допомагає вибору, але не замінює серверну перевірку |
| Браузер → API | URL, path id, query status, headers | :guid, allowlist status, HTTP 400/404; клієнт можна обійти |
| API → PostgreSQL | status та id з HTTP | Типізований LINQ/EF Core, параметризовані значення, відсутність складання SQL рядками |
| БД → API → DOM | Раніше збережені title/description/comment | Мінімальні DTO, textContent/createTextNode; дані не стають HTML |

Це навчальний baseline без автентифікації; summary не додає авторизацію.

## Конфігурація та повернення seed

- `global.json`: мінімум 10.0.100, latestFeature, без preview; локально обрано 10.0.401.
- `appsettings.json` і `appsettings.Development.json`: налаштування API та Development bootstrap.
- `ConnectionStrings__SecureLab` перевизначає connection string поза Git.
- `infra/compose.yaml` і `infra/.env.example`: локальна PostgreSQL та published port.
- `Properties/launchSettings.json`: профіль http, Development і адреса API.

Порт підключення API має відповідати published port Compose. Конфігурація
не повинна містити реальні секрети у Git; `.env` ігнорується, `.env.example` дозволений.
Seed: три інциденти Low/Medium/High по одному, Critical: 0; Triaged має
Medium: 1 і решту нулів; Resolved має чотири нульові групи.
Клієнт рахує загальну кількість інцидентів через суму count. Для всіх нулів
показує окреме повідомлення «Даних немає», залишаючи рядки з нулями.
Схему БД та seed у ЛР 1 не змінено; migration не потрібна.

Відновлення лише відомої локальної навчальної БД у Development:

```powershell
dotnet run --project src/SecureLab.Api -- --reset-database
```

Команда очищує навчальні таблиці: спочатку зупиніть API та перевірте ціль БД.
Після reset повторіть list Triaged і summary. Фактичні результати запуску,
HTTP, SQL, browser-flow і тестів належать зовнішньому звіту, а не припущенням цієї карти.
