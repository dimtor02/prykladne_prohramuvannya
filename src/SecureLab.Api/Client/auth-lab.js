const profile = document.querySelector("#profile");
const sessionState = document.querySelector("#session-state");
const createFields = document.querySelector("#create-fields");

async function apiFetch(path, options = {}) {
  const url = new URL(path, location.origin);
  if (url.origin !== location.origin) throw new Error("Потрібна адреса цього застосунку.");
  return fetch(url, { ...options, credentials: "same-origin" });
}

function showState(state) {
  sessionState.textContent = state === "authenticated" ? "Сеанс активний"
    : state === "anonymous" ? "Не ввійшли" : "Не вдалося перевірити сеанс";
  createFields.disabled = state !== "authenticated";
  if (state !== "authenticated") profile.textContent = "";
}

async function jsonBody(response) {
  if (!response.headers.get("Content-Type")?.includes("json"))
    throw new Error(`Неочікуваний формат відповіді: HTTP ${response.status}.`);
  return response.json();
}

async function failure(response) {
  let detail = "Не вдалося виконати дію.";
  if (response.headers.get("Content-Type")?.includes("json")) {
    const problem = await response.json();
    detail = problem.errors ? Object.values(problem.errors).flat().join(" ")
      : problem.detail || problem.title || detail;
  }
  return new Error(`HTTP ${response.status}: ${detail}`);
}

async function refreshProfile() {
  try {
    const response = await apiFetch("/api/me");
    if (response.status === 401) {
      showState("anonymous");
      return 401;
    }
    if (!response.ok) throw await failure(response);
    const user = await jsonBody(response);
    if (typeof user.id !== "string" || typeof user.userName !== "string" || typeof user.displayName !== "string")
      throw new Error("Неочікуваний формат профілю.");
    profile.textContent = `${user.displayName}\nІм’я користувача: ${user.userName}\nID: ${user.id}`;
    showState("authenticated");
    return response.status;
  } catch (error) {
    showState("unknown");
    throw error;
  }
}

function bindForm(selector, resultSelector, action) {
  const form = document.querySelector(selector);
  const result = document.querySelector(resultSelector);
  form.addEventListener("submit", async event => {
    event.preventDefault();
    const button = form.querySelector("button[type=submit]");
    button.disabled = true;
    result.textContent = "Надсилаємо…";
    try { await action(form, result); }
    catch (error) { result.textContent = error instanceof TypeError ? "Немає зв’язку із сервером." : error.message; }
    finally {
      if (form.elements.password) form.elements.password.value = "";
      button.disabled = false;
    }
  });
}

bindForm("#register", "#register-result", async (form, result) => {
  const values = Object.fromEntries(new FormData(form));
  const body = { userName: values.userName, email: values.email, displayName: values.displayName, password: values.password };
  form.elements.password.value = "";
  const response = await apiFetch("/api/auth/register", {
    method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body)
  });
  if (!response.ok) throw await failure(response);
  const user = await jsonBody(response);
  document.querySelector("#login").elements.userName.value = user.userName;
  result.textContent = `Реєстрація: HTTP ${response.status}. Обліковий запис створено. Увійдіть зі своїм паролем.`;
});

bindForm("#login", "#login-result", async (form, result) => {
  const values = Object.fromEntries(new FormData(form));
  const body = { userName: values.userName, password: values.password };
  form.elements.password.value = "";
  const response = await apiFetch("/api/auth/login", {
    method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body)
  });
  if (!response.ok) {
    if (response.status === 401) await refreshProfile();
    throw await failure(response);
  }
  const status = await refreshProfile();
  result.textContent = `Вхід: HTTP ${response.status}; профіль: HTTP ${status}.`;
});

bindForm("#create", "#create-result", async (form, result) => {
  const values = Object.fromEntries(new FormData(form));
  const body = { title: values.title, description: values.description, severity: values.severity,
    occurredAtUtc: new Date(values.occurredAtUtc).toISOString() };
  const response = await apiFetch("/api/incidents", {
    method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body)
  });
  if (!response.ok) {
    if (response.status === 401) showState("anonymous");
    throw await failure(response);
  }
  const incident = await jsonBody(response);
  result.textContent = `Інцидент створено: HTTP ${response.status}. ID: ${incident.id}`;
  form.reset();
});

document.querySelector("#me").addEventListener("click", async () => {
  const result = document.querySelector("#session-result");
  try { result.textContent = `Профіль: HTTP ${await refreshProfile()}.`; }
  catch (error) { result.textContent = error instanceof TypeError ? "Немає зв’язку із сервером." : error.message; }
});

document.querySelector("#logout").addEventListener("click", async () => {
  const result = document.querySelector("#session-result");
  try {
    const response = await apiFetch("/api/auth/logout", { method: "POST" });
    if (!response.ok) throw await failure(response);
    profile.textContent = "";
    document.querySelector("#create").reset();
    for (const id of ["#create-result", "#login-result", "#register-result"])
      document.querySelector(id).textContent = "";
    const status = await refreshProfile();
    result.textContent = `Вихід: HTTP ${response.status}; повторний профіль: HTTP ${status}.`;
    if (status !== 401) throw new Error("Не вдалося підтвердити завершення сеансу.");
  } catch (error) {
    showState("unknown");
    result.textContent = error instanceof TypeError ? "Немає зв’язку із сервером." : error.message;
  }
});

refreshProfile().catch(error => {
  document.querySelector("#session-result").textContent = error instanceof TypeError
    ? "Немає зв’язку із сервером." : error.message;
});
