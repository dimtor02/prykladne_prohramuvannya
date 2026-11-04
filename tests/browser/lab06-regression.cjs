const { chromium } = require(process.env.LAB05_PLAYWRIGHT_PATH || 'playwright');
const fs = require('node:fs/promises');
const path = require('node:path');
const assert = require('node:assert/strict');
const API = 'http://localhost:5080';
const output = process.env.LAB06_OUTPUT_DIR || path.resolve(__dirname, '../../docs/evidence/lr06');
const password = process.env.SeedUsers__Password;
let browser;
(async () => {
  assert(password, 'Set SeedUsers__Password outside Git.');
  await fs.mkdir(output, { recursive: true });
  browser = await chromium.launch({ headless: true,
    ...(process.env.LAB05_CHROME_PATH ? { executablePath: process.env.LAB05_CHROME_PATH } : {}) });
  const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const page = await context.newPage();
  const result = { utc: new Date().toISOString(), browser: browser.version(), cases: [], mutations: [] };
  const failures = [];
  page.on('pageerror', e => failures.push(e.message));
  page.on('request', r => {
    if (r.url().startsWith(API) && ['POST','PUT','DELETE'].includes(r.method()))
      result.mutations.push({ method: r.method(), path: new URL(r.url()).pathname, tokenPresent: !!r.headers()['x-csrf-token'] });
  });
  async function login(user) {
    await page.goto(API + '/auth-lab.html');
    await page.locator('#login [name=userName]').fill(user);
    await page.locator('#login [name=password]').fill(password);
    const response = page.waitForResponse(r => r.url().endsWith('/api/auth/login'));
    await page.locator('#login button').click();
    assert.equal((await response).status(), 204);
    await page.waitForFunction(() => document.querySelector('#session-state').textContent === 'Сеанс активний');
  }
  const run = Date.now().toString(36);
  await page.goto(API + '/auth-lab.html');
  await page.locator('#register [name=userName]').fill('lr06-' + run);
  await page.locator('#register [name=email]').fill('lr06-' + run + '@example.test');
  await page.locator('#register [name=displayName]').fill('LR06 synthetic browser user');
  await page.locator('#register [name=password]').fill(password);
  const registered = page.waitForResponse(r => r.url().endsWith('/api/auth/register'));
  await page.locator('#register button').click();
  assert.equal((await registered).status(), 201);
  result.cases.push({ name: 'register-form-preserved', status: 201 });
  await login('alice');
  await page.reload();
  await page.waitForFunction(() => !document.querySelector('#create-fields').disabled);
  await page.locator('#create [name=title]').fill('LR06 auth page ' + run);
  await page.locator('#create [name=description]').fill('Legitimate auth-page create after F5.');
  await page.locator('#create [name=severity]').selectOption('Low');
  await page.locator('#create [name=occurredAtUtc]').fill('2026-09-01T12:00');
  const createReply = page.waitForResponse(r => r.request().method() === 'POST' && r.url().endsWith('/api/incidents'));
  await page.locator('#create button').click();
  const made = await createReply;
  assert.equal(made.status(), 201);
  const item = await made.json();
  result.cases.push({ name: 'auth-page-F5-create', status: 201, id: item.id });
  await page.goto(API + '/browser-lab.html');
  await page.reload();
  await page.locator('#incident-id').fill(item.id);
  const loaded = page.waitForResponse(r => r.url().endsWith('/api/incidents/' + item.id));
  await page.locator('#load').click();
  assert.equal((await loaded).status(), 200);
  const flow = await page.evaluate(async id => {
    const url = '/api/incidents/' + id;
    const before = await (await fetch(url)).json();
    const body = JSON.stringify({ title: before.title, description: 'Updated with standard antiforgery pair' });
    const denied = await fetch(url, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body });
    const unchanged = await (await fetch(url)).json();
    const allowed = await window.secureLabCsrf.fetch(url, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body });
    const after = await (await fetch(url)).json();
    return { denied: denied.status, unchanged: JSON.stringify(before) === JSON.stringify(unchanged), allowed: allowed.status, after: after.description };
  }, item.id);
  assert.equal(flow.denied, 400); assert(flow.unchanged); assert.equal(flow.allowed, 204);
  assert.equal(flow.after, 'Updated with standard antiforgery pair');
  result.cases.push({ name: 'REG-CSRF-01-browser', ...flow });
  await page.locator('#edit [name=title]').fill('LR06 browser page ' + run);
  await page.locator('#edit [name=description]').fill('5 < 7; plain <b>text</b> after page navigation and F5');
  const otherReply = page.waitForResponse(r => r.request().method() === 'POST' && r.url().endsWith('/api/incidents'));
  await page.locator('#edit button').click();
  const other = await otherReply;
  assert.equal(other.status(), 201);
  const second = await other.json();
  await page.waitForFunction(() => document.querySelector('#description').textContent.includes('5 < 7'));
  await page.screenshot({ path: path.join(output, 'browser-csrf-safe-text.png'), fullPage: true });
  result.cases.push({ name: 'browser-page-create-after-navigation-F5', status: 201, id: second.id });
  for (const id of [item.id, second.id]) {
    const status = await page.evaluate(async id => (await window.secureLabCsrf.fetch('/api/incidents/' + id, { method: 'DELETE' })).status, id);
    assert.equal(status, 204);
  }
  await page.goto(API + '/auth-lab.html');
  const logout = page.waitForResponse(r => r.url().endsWith('/api/auth/logout'));
  await page.locator('#logout').click();
  assert.equal((await logout).status(), 204);
  await page.waitForFunction(() => document.querySelector('#session-state').textContent === 'Не ввійшли');
  await login('bob');
  await page.goto(API + '/browser-lab.html');
  await page.reload();
  const bobFlow = await page.evaluate(async suffix => {
    const response = await window.secureLabCsrf.fetch('/api/incidents', { method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title: 'LR06 Bob ' + suffix, description: 'New principal gets a new token', severity: 'Low', occurredAtUtc: new Date().toISOString() }) });
    const body = await response.json();
    const deleted = await window.secureLabCsrf.fetch('/api/incidents/' + body.id, { method: 'DELETE' });
    return { create: response.status, deleted: deleted.status };
  }, run);
  assert.equal(bobFlow.create, 201); assert.equal(bobFlow.deleted, 204);
  result.cases.push({ name: 'logout-login-Bob-F5-new-token', ...bobFlow });
  assert.equal(await page.evaluate(() => localStorage.length + sessionStorage.length), 0);
  const cookies = await context.cookies();
  result.cookieFlags = cookies.map(({ name, httpOnly, secure, sameSite }) => ({ name, httpOnly, secure, sameSite }));
  assert.deepEqual(failures, []);
  result.pageErrors = failures;
  await fs.writeFile(path.join(output, 'browser-csrf.json'), JSON.stringify(result, null, 2));
  console.log('PASS: actual browser login/create/navigation/F5/CSRF state proof/logout/new principal; no page errors.');
})().finally(async () => { if (browser) await browser.close(); }).catch(e => { console.error(e.message); process.exitCode = 1; });
