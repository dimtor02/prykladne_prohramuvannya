# Матриця доступу ЛР4 — baseline 2-A, рівень «Добре»

Головна action: `GET /api/incidents/{id}`. Subject — тільки `NameIdentifier` із перевіреного Identity principal; ролі — з цього principal. Body/query/header не є джерелом прав.

| Дія | Анонім | Reporter — власник | Reporter — не власник | Analyst (Morgan) | Administrator без інших ролей | Реалізація / доказ |
|---|---|---|---|---|---|---|
| List `GET /api/incidents` | 401 | Лише власні | Чужі відсутні | Усі для аналізу | Порожній список | `ScopeRead`; A-01/A-05 |
| Details `GET /api/incidents/{id}` | 401 | 200 | 404 | 200 для аналізу | 404 | `ScopeRead`; A-02/A-03/A-03R |
| Create `POST /api/incidents` | 401 | 201 за валідного DTO | Не можна призначити іншого owner | 201 від свого імені | 201 від свого імені | Успадкований ЛР3 `MapLab03Identity`; `IncidentCreationTests` |
| Update content `PUT /api/incidents/{id}` | 401 | 204: лише title/description | 404; стан незмінний | 403; content не змінюється | 404 | `Decide(UpdateContent)`; A-04/A-08a |
| Delete `DELETE /api/incidents/{id}` | 401 | 204; діти видаляються каскадно | 404; стан незмінний | 403; стан незмінний | 404 | `Decide(Delete)`; A-04, owner-delete/Analyst-mutation tests |
| Triage `POST /api/incidents/{id}/triage` | 401 | 403 навіть для власного New | 404 | 204 лише New → Triaged; інший status → 409 | 404 | `Decide(Triage)`; A-04/A-06/A-07/A-09 |
| Зміна severity/status через regular PUT | 401 | Ігноруються, 204 за валідного content | 404 | 403 | 404 | DTO містить лише title/description; A-08a |
| Direct child `GET /api/incident-comments/{id}` | 401 | 200 лише public comment власного parent | 404 навіть за відомого child id | 200 після перевірки parent | 404 | Parent `ScopeRead` + IsInternal; A-01/A-10/A-12 |
| Internal comment (direct і details) | 401 | 404 direct; відсутній у details | 404 direct; parent недоступний | Видимий після дозволу читання parent | 404 direct; parent недоступний | `IsAnalyst`; A-12 |

Administrator не успадковує автоматично Reporter/Analyst. Protected create успадкований із ЛР3 і дозволений будь-якому authenticated principal; owner — цей principal. Create сам по собі не надає права read/update: там потрібна роль із таблиці. Для кількох ролей діють перевірки Reporter/Analyst у policy.

## Політика відмов та контекст

- **401** — немає authentication; JSON Problem Details, без HTML redirect.
- **404** — ресурс не існує або його не видно; однакові status, Content-Type і тіло без correlation fields для details/update/delete/triage/child.
- **403** — ресурс видно, але немає права на дію: owner-Reporter → triage або Analyst → update/delete.
- **409** — Analyst має право triage, але status уже не New; повтор не змінює status/history.
- **400** — невалідний дозволений DTO. A-08a: невідомі поля regular PUT ігноруються; A-08b не застосовується.
- Owner/id/status/createdAtUtc призначає сервер. PUT зберігає owner/status/severity/occurredAtUtc/createdAtUtc; updatedAtUtc оновлює сервер. Triage задає New → Triaged та actor із principal; body не визначає перехід.

Після reset Alice має інциденти 1, 3, 5; Bob — 2, 4. Morgan читає всі п'ять. Internal comment-3 (`30000000-0000-0000-0000-000000000003`) належить інциденту Alice; comment-2 — інциденту Bob.

## Межа рівня та залишковий ризик

`search` і `severity-summary` успадковані та не scoped за subject. A-11 і виправлення цих read paths належать до «Відмінно»; тут не виконані. Через них залишаються доступними чужі дані/агрегати для authenticated users: повний захист усього API не заявляється. Batch/export не реалізовані. CSRF для cookie mutation — навчальний борг до ЛР6. Optimistic concurrency не додано; окремого сценарію викладача не надано.

Policy: `IncidentAccessPolicy.cs`; відмови: `IncidentAccessResults.cs`; read scoping: `IncidentQueries.cs`; mutations/child: `IncidentMutationEndpoints.cs`. UI лише відображає рішення сервера через safe text sink.
