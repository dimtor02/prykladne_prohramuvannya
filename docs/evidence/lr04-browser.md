# ЛР4 — фактична браузерна перевірка

20.10.2026, локальний API :5080, синтетична Alice, перевірку проведено інструментами Codex. Login і helper own/Bob повторено після фінального reset, 113/113 tests і live HTTP retest; helper-знімки оновлено за останнім прогоном.

1. `auth-lab.html`: Alice login → HTTP 204, profile → HTTP 200; UI «Сеанс активний».
2. У тій самій сесії головна сторінка: «Знайдено: 3». Показано лише інциденти Alice 1/3/5.
3. Клік на incident-1: власні details відображено; один public comment Morgan; internal comment-3 відсутній.
4. `lr04-probe.html`: кнопка власних details викликає глобальну `loadIncidentDetails` в same-origin iframe з наданим клієнтом; результат збігається з прямим кліком.
5. Кнопка Bob викликає ту саму функцію з id incident-2. Details замінено на «Помилка: Інцидент не знайдено». Старі details очищено.

Console API керованого браузера не доступний. Навчальна helper-сторінка лише відтворює Console-команду методички, не модифікує response або authorization. HTTP 200/404 і еквівалентні unknown 404 підтверджені integration tests та окремим live script; коди не виводилися з одного лише тексту UI.

Власні знімки: `lr04-browser-own.png` (головна сторінка), `lr04-browser-own-probe.png` і `lr04-browser-denied.png` (helper із незміненим клієнтом). Це знімки цього прогону, не ілюстрації методички. Пароль, cookie, Set-Cookie, Network/Application не включено. Тимчасовий файл передачі seed-пароля поза Git видалено після входу.

Дані й помилки у `app.js` відображаються через textContent/createTextElement. Helper не робить mutation. Самостійну ручну перевірку студентом у цьому документі не заявлено.
