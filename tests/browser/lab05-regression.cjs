const { chromium } = require(process.env.LAB05_PLAYWRIGHT_PATH || 'playwright');
const fs = require('node:fs/promises');
const path = require('node:path');
const assert = require('node:assert/strict');
const project = path.resolve(__dirname, '../..');
const privateDir = process.env.LAB05_RUN_DIR || path.join(require('node:os').tmpdir(), 'securelab-lr05');
const output = process.env.LAB05_OUTPUT_DIR || path.join(project, 'docs/evidence/lr05');
const runId = Date.now().toString(36);
const API = 'http://localhost:5080';
const stage = process.argv[2];
let browser;
(async () => {
  assert(['normal','vulnerable','fixed','cors-before','cors-no-credentials','cors-fixed','csp'].includes(stage),'Choose a documented test stage');
  await fs.mkdir(output, { recursive: true });
  await fs.mkdir(privateDir, { recursive: true });
  const password = process.env.SeedUsers__Password;
  assert(password, 'Set SeedUsers__Password locally, outside Git');
  const marker = (await fs.readFile(path.join(__dirname, 'lab05-marker.txt'), 'utf8')).trim();
  browser = await chromium.launch({ headless: true,
    ...(process.env.LAB05_CHROME_PATH ? {executablePath:process.env.LAB05_CHROME_PATH} : {}),
    ignoreDefaultArgs: ['--unsafely-disable-devtools-self-xss-warnings'] });
  const result = { stage, browser: browser.version(), at: new Date().toISOString(), cases: [] };
  async function actor(user) {
    const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
    const page = await context.newPage();
    await page.goto(API + '/auth-lab.html');
    await page.locator('#login [name=userName]').fill(user);
    await page.locator('#login [name=password]').fill(password);
    const responsePromise = page.waitForResponse(r => r.url().endsWith('/api/auth/login'));
    await page.locator('#login button').click();
    assert.equal((await responsePromise).status(), 204, 'Login ' + user);
    await page.goto(API + '/browser-lab.html');
    return { context, page };
  }
  async function render(page, id) {
    await page.reload();
    await page.locator('#incident-id').fill(id);
    const responsePromise = page.waitForResponse(r => r.url().endsWith('/api/incidents/' + id));
    await page.locator('#load').click();
    const response = await responsePromise;
    await page.waitForTimeout(300);
    const dom = await page.evaluate(() => ({
      marker: document.querySelector('#lab05-marker-target').getAttribute('data-lab05-marker'),
      description: { text: document.querySelector('#description').textContent,
        children: document.querySelector('#description').childElementCount },
      comments: [...document.querySelector('#comments').children].map(p => ({text:p.textContent,children:p.childElementCount})),
      status: document.querySelector('#status').textContent
    }));
    return { status: response.status(), json: response.ok() ? await response.json() : null, dom };
  }
  async function create(page, text, title) {
    await page.locator('#edit [name=title]').fill(title);
    await page.locator('#edit [name=description]').fill(text);
    const responsePromise = page.waitForResponse(r => r.request().method() === 'POST' && r.url().endsWith('/api/incidents'));
    await page.locator('#edit button').click();
    const response = await responsePromise;
    assert.equal(response.status(), 201, await response.text());
    const data = await response.json();
    assert.equal(data.description, text);
    return data.id;
  }
  if (['normal','vulnerable','fixed'].includes(stage)) {
    const alice = await actor('alice');
    const seed = await render(alice.page, '20000000-0000-0000-0000-000000000001');
    assert.equal(seed.status, 200);
    result.cases.push({name:'seed-normal-flow', ...seed});
    const values = ["Користувач O'Brien написав \"терміново\"", '5 < 7 і 9 > 3', 'Київ — Ω', '<b>жирний</b> текст', 'А'.repeat(4000)];
    if (stage === 'normal' || stage === 'fixed') {
      for (let i=0;i<values.length;i++) {
        const id = await create(alice.page, values[i], `LR05 ${stage} ${runId} normal ${i}`);
        const seen = await render(alice.page,id);
        assert.equal(seen.json.description,values[i]);
        if (stage === 'fixed' || i!==3) {
          assert.equal(seen.dom.description.text,values[i]);
          assert.equal(seen.dom.description.children,0);
        } else assert.equal(seen.dom.description.children,1);
        result.cases.push({name:'normal-'+i,input:values[i],id,...seen});
      }
    }
    if (stage !== 'normal') {
      let fixtures;
      if (stage === 'vulnerable') {
        fixtures = { descriptionId: await create(alice.page, marker, 'LR05 canonical harmless marker '+runId) };
        await fs.writeFile(path.join(privateDir,'fixtures.json'), JSON.stringify(fixtures));
      } else {
        try { fixtures = JSON.parse(await fs.readFile(path.join(privateDir,'fixtures.json'))); }
        catch { fixtures = {}; }
        const existing = fixtures.descriptionId && await alice.context.request.get(API+'/api/incidents/'+fixtures.descriptionId);
        if (!existing || existing.status()===404) {
          fixtures = {descriptionId: await create(alice.page,marker,'LR05 fixed marker '+runId)};
          await fs.writeFile(path.join(privateDir,'fixtures.json'),JSON.stringify(fixtures));
        }
      }
      for (const user of ['alice','morgan','bob']) {
        const a = user==='alice'?alice:await actor(user);
        const seen = await render(a.page, fixtures.descriptionId);
        assert.equal(seen.status, user==='bob'?404:200);
        assert.equal(seen.dom.marker, user==='bob'||stage==='fixed'?null:'executed');
        if (user!=='bob') {
          assert.equal(seen.json.description,marker);
          assert.equal(seen.dom.description.children,stage==='fixed'?0:1);
          if(stage==='fixed') assert.equal(seen.dom.description.text,marker);
        }
        result.cases.push({name:'description-'+user,id:fixtures.descriptionId,...seen});
        await a.page.screenshot({path:path.join(output,`${stage}-${user}.png`),fullPage:true});
        if(user!=='bob') {
          const comment = await render(a.page,'20000000-0000-0000-0000-000000000001');
          const seeded = comment.json.comments.find(c=>c.id==='30000000-0000-0000-0000-000000000005');
          assert(seeded, 'Missing lab05 seed comment; run current seed/reset');
          if(seeded) {
            assert.equal(seeded.text,marker);
            assert.equal(comment.dom.marker,stage==='fixed'?null:'executed');
            result.cases.push({name:'comment-'+user,...comment});
            if(stage==='fixed') {
              const repeated=await a.page.evaluate(async()=>{await load();return [...document.querySelector('#comments').children].map(p=>({text:p.textContent,children:p.childElementCount}));});
              assert.deepEqual(repeated,comment.dom.comments);
              for(let i=0;i<repeated.length;i++) {
                assert.equal(repeated[i].text,comment.json.comments[i].text);
                assert.equal(repeated[i].children,0);
              }
            }
            await a.page.screenshot({path:path.join(output,`${stage}-comments-${user}.png`),fullPage:true});
          }
        }
      }
    }
    const cookies=await alice.context.cookies();
    result.cookies=cookies.map(({name,domain,path,httpOnly,sameSite,secure})=>({name,domain,path,httpOnly,sameSite,secure}));
  }
  if(stage.startsWith('cors-')) {
    const alice=await actor('alice');
    for(const port of [4173,4174]) {
      for(const kind of ['simple','preflight','cookie','without-cookie']) {
        const page=await alice.context.newPage();
        await page.goto(`http://localhost:${port}/`);
        const cdp=await alice.context.newCDPSession(page);
        const network=[];
        await cdp.send('Network.enable');
        await cdp.send('Network.setCacheDisabled',{cacheDisabled:true});
        const safeHeaders=headers=>Object.fromEntries(Object.entries(headers).filter(([k])=>['origin','access-control-request-method','access-control-request-headers','access-control-allow-origin','access-control-allow-methods','access-control-allow-headers','access-control-allow-credentials'].includes(k.toLowerCase())));
        cdp.on('Network.requestWillBeSent',e=>{if(e.request.url.startsWith(API))network.push({event:'request',id:e.requestId,method:e.request.method,url:e.request.url,headers:safeHeaders(e.request.headers)});});
        cdp.on('Network.requestWillBeSentExtraInfo',e=>network.push({event:'actually-sent',id:e.requestId,headers:safeHeaders(e.headers),cookiePresent:Object.keys(e.headers).some(k=>k.toLowerCase()==='cookie')}));
        cdp.on('Network.responseReceivedExtraInfo',e=>network.push({event:'server-response',id:e.requestId,status:e.statusCode,headers:safeHeaders(e.headers)}));
        const console=[];page.on('console',m=>console.push(m.text()));
        const protectedRoute=kind==='cookie'||kind==='without-cookie';
        const url=API+(protectedRoute?'/api/incidents':'/health')+`?lr05=${stage}-${port}-${kind}`;
        const value=await page.evaluate(async({url,kind})=>{
          try {
            const response=await fetch(url,{credentials:kind==='cookie'?'include':'omit',headers:kind==='preflight'?{'X-Lab-Cors-Probe':'stage-3'}:{}});
            return {readable:true,status:response.status,body:await response.text()};
          } catch(e) {return {readable:false,error:e.name+': '+e.message};}
        },{url,kind});
        await page.waitForTimeout(300);
        const expected=stage!=='cors-before'&&port===4173&&(kind!=='cookie'||stage==='cors-fixed');
        if(value.readable!==expected) {
          await fs.writeFile(path.join(output,stage+'-failure.json'),JSON.stringify({stage,port,kind,value,network,console},null,2));
        }
        assert.equal(value.readable,expected,`${stage}/${port}/${kind}`);
        if(expected)assert.equal(value.status,kind==='without-cookie'?401:200);
        const requests = network.filter(n=>n.event==='request');
        const sentIds = new Set(network.filter(n=>n.event==='actually-sent').map(n=>n.id));
        if(kind==='preflight') {
          const options=requests.find(n=>n.method==='OPTIONS');
          assert(options && sentIds.has(options.id),'Browser must send real OPTIONS');
          const actualGet=requests.some(n=>n.method==='GET'&&sentIds.has(n.id));
          assert.equal(actualGet,expected,'Actual GET only after successful preflight');
        }
        if(kind==='cookie') assert(network.some(n=>n.event==='actually-sent'&&n.cookiePresent),'Expected cookie to be sent');
        result.cases.push({name:`${port}-${kind}`,...value,network,console});
        await page.close();
      }
    }
    if(stage==='cors-fixed') {
      const context=await browser.newContext();const page=await context.newPage();
      await page.goto('http://localhost:4173/');
      const anonymous=await page.evaluate(async()=>{const r=await fetch('http://localhost:5080/api/incidents',{credentials:'include'});return r.status;});
      assert.equal(anonymous,401);result.cases.push({name:'4173-anonymous-include',status:anonymous});
    }
  }
  if(stage==='csp') {
    const alice=await actor('alice');
    const resourceSet=new Set();const messages=[];
    alice.page.on('response',r=>resourceSet.add(r.url()));
    alice.page.on('console',m=>messages.push(m.text()));
    for(const route of ['/','/auth-lab.html','/browser-lab.html','/lr04-probe.html']) {
      const response=await alice.page.goto(API+route);
      assert.equal(response.status(),200);
      const headers=await response.allHeaders();
      assert.match(headers['content-security-policy'],/frame-ancestors 'self'/);
      assert.equal(headers['content-security-policy-report-only'],undefined);
      assert.equal(headers['x-content-type-options'],'nosniff');
      assert.equal(headers['referrer-policy'],'no-referrer');
      await alice.page.waitForTimeout(300);
      if(route==='/') {
        await alice.page.waitForFunction(()=>document.querySelectorAll('#incident-list button').length>0);
        await alice.page.locator('#incident-list button').first().click();
        await alice.page.waitForFunction(()=>document.querySelector('#incident-details h3'));
      }
      if(route==='/lr04-probe.html') {
        await alice.page.locator('#own').click();
        await alice.page.frameLocator('#tracker').locator('#incident-details h3').waitFor();
      }
      result.cases.push({name:route,status:response.status(),headers:Object.fromEntries(Object.entries(headers).filter(([k])=>['content-security-policy','x-content-type-options','referrer-policy'].includes(k)))});
    }
    assert.equal(messages.filter(m=>m.includes('Content Security Policy')||m.includes('violates')).length,0,'Legitimate CSP violations');
    result.resources=[...resourceSet].sort();
    result.legitimateConsole=messages;
    await alice.page.goto(API+'/');
    const violations=await alice.page.evaluate(async()=>{
      const events=[];
      document.addEventListener('securitypolicyviolation',e=>events.push({directive:e.effectiveDirective,blockedURI:e.blockedURI,disposition:e.disposition}));
      new Image().src='http://localhost:4173/x.png';
      await new Promise(resolve=>setTimeout(resolve,400));return events;
    });
    assert(violations.some(v=>v.directive==='img-src'&&v.disposition==='enforce'));
    result.cases.push({name:'image-blocked',violations});
    const page=await alice.context.newPage();await page.goto('http://localhost:4173/');
    const frameConsole=[];page.on('console',m=>frameConsole.push(m.text()));
    const embedding=await page.evaluate(async()=>{
      const frame=document.createElement('iframe');frame.src='http://localhost:5080/';document.body.append(frame);
      await new Promise(resolve=>setTimeout(resolve,500));return frame.contentWindow.length;
    });
    assert(frameConsole.some(m=>m.includes('frame-ancestors')&&m.includes('violat')),'Cross-origin frame must be blocked by CSP');
    result.cases.push({name:'cross-origin-embedding',windowLength:embedding,console:frameConsole});
    await alice.page.screenshot({path:path.join(output,'csp-client.png'),fullPage:true});
  }
  await fs.writeFile(path.join(output,stage+'.json'),JSON.stringify(result,null,2));
  console.log(JSON.stringify({stage,browser:result.browser,cases:result.cases.map(x=>({name:x.name,status:x.status,marker:x.dom?.marker})),result:'PASS'}));
})().catch(e=>{console.error(e.message);process.exitCode=1;}).finally(async()=>{if(browser)await browser.close();});
