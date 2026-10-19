# ЛР3: перевірені безпечні результати

20.10.2026, локальна синтетична БД, Windows, Release/net10.0.

## Вхідний стан

Після scaffold lab-03-start-v1 виконано два повні reset і suite після кожного. В обох TRX: total=55, passed=55, failed=0, skipped=0. Пароль увів студент через прихований prompt; він не входить до доказів.

## Незалежні перевірки власних змін

Успішна збірка: 0 попереджень, 0 помилок. Вибраний набір: **15 passed, 0 failed, 0 skipped**.

- A-01: GET /api/me та POST /api/incidents без cookie, з/без X-Demo-UserId Bob, повернули 401 application/problem+json без Location/Bearer challenge; кількість інцидентів незмінна.
- T-01: 201, Location /api/me, лише id/userName/displayName; невідомі role/id/passwordHash не змінили серверний id і роль Reporter. Вхід створеним користувачем успішний.
- Password не обрізається: стандартний verifier прийняв оригінал із пробілами та відхилив Trim-варіант. Значення пароля та hash не друкувалися.
- Duplicate: повтор, збіг нормалізованого username та збіг нормалізованого email дали 409; один збережений акаунт.
- T-02: усі 11 випадків missing/long/bad userName, bad/long email, blank/long/control displayName, short/long/weak password дали безпечний 400 із правильним ключем, user не створено.
- Rate limit/default: на реальному login перші 10 спроб дали 401, одинадцята — 429.

Це історія проміжного етапу; вона не підміняє [повний успішний фінальний suite](lr03-test-results.md) та [фактичний browser flow](lr03-browser.md), виконані пізніше.

## Перший повний suite та виправлення тестової конфігурації

Перший повний прогін: **79 passed, 1 failed, 0 skipped, total 80**. Невдалий CookieFlags_HttpsAndProduction не дістав connection string до раннього читання налаштувань у Program. Решта 79 випадків пройшли; другий reset і реальний HTTP flow не виконувалися, оскільки runner правильно зупинився на першій помилці.

Виправлення: Production-тест передає налаштування через ранні host settings. Реєструє окремий випадковий synthetic account, а після перевірки видаляє лише його. Пароль генерується в пам’яті, seed-пароль студента не змінюється.

Окремий повтор CookieFlags_HttpsAndProduction: **1 passed, 0 failed**. Реальні Set-Cookie attributes для Development HTTPS і Production HTTPS: Secure, HttpOnly, SameSite=Lax, Path=/, session без Expires/Max-Age. Значення cookie не друкувалися; наступний GET /api/me у кожному host дав 200. Після цього виправлення повторено два reset і весь suite: **80/80 успішних після кожного**; завершено HTTP і browser flow (посилання вище).
