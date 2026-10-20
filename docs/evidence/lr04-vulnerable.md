# ЛР4: фактичний локальний PoC до виправлення

Дата: 2026-10-20. Лише локальна PostgreSQL і синтетичні акаунти baseline 2-A.
Старт: v0.3.0 (3390ea5ef3bb52f6d7758bc14f9fc39ff4c81ec0).
Baseline: Release build без помилок/попереджень; 80 passed, 0 failed, 0 skipped.

Виконано HTTP integration test `VulnerableAuthorizationProofTests.LocalProof_ReadMutationAndPrivilegeBypass`.
Результат: 1 passed, 0 failed. Цей тест доводить наявність дефектів, а не коректний захист.

| Сценарій | Очікувано за майбутньою матрицею | Фактично у vulnerable state |
|---|---|---|
| Alice GET власних details | 200 | 200 |
| Alice GET details Bob через заміну id | Відмова | 200, дані Bob доступні |
| Alice PUT окремого синтетичного інциденту Bob | Відмова, стан незмінний | 204; наступний GET Bob підтвердив зміну description |
| Reporter Alice POST triage інциденту Bob | Відмова, статус незмінний | 204; окреме читання БД підтвердило New → Triaged |

Login Alice/Bob/Morgan використовує справжню Identity cookie. Morgan /api/me → 200;
анонімний /api/me → 401. Authentication не вимикалася.
Для PUT/triage використовувався новий інцидент, створений Bob через API;
у finally він видалений за точним id. Seed-інциденти не змінювалися.
Надіслані forged role/status/isTransitionAllowed не є джерелом прав: причина успіху —
відсутність resource/role decision перед mutation, а не довіра до цих полів.

Першопричина read: `IncidentQueries.GetDetailsAsync` шукає лише за id.
Першопричина mutation/triage: RequireAuthorization підтверджує тільки вхід;
після пошуку за id код одразу змінює entity й викликає SaveChanges без owner/role check.

Vulnerable commit: `85ca4ac267633d128a9ac8dd9ca97242d1adc4d5`.
Hash перевірено в робочому Git-репозиторії `starter-project`; зовнішній `lab-01-starter/.git` є іншим репозиторієм і не є історією цієї роботи.
TRX з повним результатом збережено поза репозиторієм у tmp/lr04-tests/lr04-vulnerable.trx.
Пароль, cookie та повне тіло login до доказу не включено.
