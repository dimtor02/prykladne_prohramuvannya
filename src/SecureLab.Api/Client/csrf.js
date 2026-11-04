// One per-page, in-memory token; never put cookies/tokens in browser storage.
window.secureLabCsrf = (() => {
  let token;
  let generation = 0;
  function reset() { token = undefined; generation++; }
  async function getToken() {
    if (token) return token;
    const current = generation;
    const response = await fetch("/api/auth/csrf-token", { credentials: "same-origin", cache: "no-store", redirect: "error" });
    if (!response.ok) throw new Error(`Не вдалося підтвердити сеанс: HTTP ${response.status}. Увійдіть знову.`);
    const body = await response.json();
    if (current !== generation) throw new Error("Сеанс змінився. Повторіть дію після входу.");
    if (typeof body.token !== "string" || !body.token) throw new Error("Сервер не надав CSRF-токен.");
    token = body.token;
    return token;
  }
  async function apiFetch(path, options = {}) {
    const url = new URL(path, location.origin);
    if (url.origin !== location.origin) throw new Error("Потрібна адреса цього застосунку.");
    const method = (options.method || "GET").toUpperCase();
    const publicAuth = ["/api/auth/login", "/api/auth/register"].includes(url.pathname);
    const headers = new Headers(options.headers);
    if (!["GET", "HEAD", "OPTIONS"].includes(method) && !publicAuth)
      headers.set("X-CSRF-TOKEN", await getToken());
    const response = await fetch(url, { ...options, method, headers, credentials: "same-origin", redirect: "error" });
    if (response.ok && ["/api/auth/login", "/api/auth/logout"].includes(url.pathname)) reset();
    if (response.status === 401) reset();
    if (response.status === 400) {
      const problem = await response.clone().json().catch(() => null);
      if (problem?.type === "urn:securelab:csrf") reset();
    }
    // Never replay a mutation automatically, including after a CSRF rejection.
    return response;
  }
  return { fetch: apiFetch, reset };
})();
