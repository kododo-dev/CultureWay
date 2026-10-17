// Browser scenarios of the editor in Way.Shell, against the running demo (Production, AllowAnonymous):
//   node check.mjs http://localhost:5091 [chrome|msedge|firefox|webkit]
// Needs the playwright package (npm i --no-save playwright); firefox and webkit need `npx playwright install`.
import { chromium, firefox, webkit } from 'playwright';

const base = process.argv[2].replace(/\/$/, '');
const which = process.argv[3] ?? 'chrome';
const browser = which === 'firefox' ? await firefox.launch() : which === 'webkit' ? await webkit.launch() : await chromium.launch({ channel: which });
const results = [];
const record = (name, ok, detail = '') => results.push({ name, ok, detail });
const visible = (l) => l.first().waitFor({ timeout: 10000 }).then(() => true, () => false);
const url = (page) => decodeURIComponent(page.url());

async function open(options = {}) {
  const context = await browser.newContext({ locale: 'en-US', colorScheme: 'light', ...options });
  const page = await context.newPage();
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  page.on('console', (m) => { if (m.type() === 'error' && !m.text().startsWith('Failed to load resource')) errors.push(m.text()); });
  page.on('response', (r) => { if (r.status() >= 400 && !r.url().endsWith('/favicon.ico')) errors.push(`${r.status()} ${r.url()}`); });
  return { context, page, errors };
}

/** The value textarea of the editor holding `value`, or null. */
async function cellWithValue(page, value) {
  await page.locator('main textarea').first().waitFor();
  const index = await page.locator('main textarea').evaluateAll((els, v) => els.findIndex((e) => e.value === v), value);
  return index >= 0 ? page.locator('main textarea').nth(index) : null;
}

async function save(page) {
  const [response] = await Promise.all([
    page.waitForResponse((r) => r.url().endsWith('/translations/api/UpdateTranslations')),
    page.getByRole('button', { name: 'Save' }).click(),
  ]);
  return response.status();
}

{
  const { context, page, errors } = await open();
  await page.goto(`${base}/translations/`);
  const menu = page.getByTestId('way-menu');
  record('editor opens in the shell', await visible(page.getByRole('heading', { name: 'All' })));
  record('title from the Way', (await page.title()) === 'Translations', await page.title());
  record('namespaces in the shell menu', await visible(menu.getByText('Common')));
  // Each flag is a separate file the module loads when a culture shows it.
  record('flags of the cultures load', await visible(page.locator('main img[src^="data:image/svg+xml"]')));

  const font = await page.locator('main h1').first().evaluate((e) => getComputedStyle(e).fontFamily);
  record("CultureWay's monospace theme in its area", /IBM Plex Mono/.test(font), font);

  // Moving around the editor keeps what was loaded: a reload would drop changes not saved yet.
  let reloads = 0;
  const countReloads = (r) => { if (r.url().endsWith('/api/GetTranslations')) reloads++; };
  page.on('request', countReloads);
  await menu.getByText('Common').click();
  record('namespace from the menu', (await visible(page.getByRole('heading', { name: 'Common' }))) && url(page).endsWith('#/ns/Common'), url(page));
  await menu.getByText('2', { exact: true }).click();
  record('nested namespace', (await visible(page.getByRole('heading', { name: 'Feature.2' }))) && url(page).endsWith('#/ns/Feature.2'), url(page));
  await menu.getByText('Nav').click();
  record('another namespace from a nested one (paths from the Way root)', (await visible(page.getByRole('heading', { name: 'Nav' }))) && url(page).endsWith('#/ns/Nav'), url(page));
  page.off('request', countReloads);
  record('namespaces do not load the translations again', reloads === 0, `${reloads}`);
  await page.goBack();
  record('browser back returns to the previous namespace', (await visible(page.getByRole('heading', { name: 'Feature.2' }))) && url(page).endsWith('#/ns/Feature.2'), url(page));

  await menu.getByText('Common').click();
  await page.getByRole('heading', { name: 'Common' }).waitFor();
  const cell = await cellWithValue(page, 'Anuluj');
  record('translation with its value', !!cell);
  if (cell) {
    await cell.fill('Anuluj!');
    const status = await save(page);
    record('saving through the shell API client', status === 204, `${status}`);
    await page.reload();
    const saved = await cellWithValue(page, 'Anuluj!');
    record('saved value after reload (deep link to the namespace)', !!saved && url(page).endsWith('#/ns/Common'), url(page));
    if (saved) {
      await saved.fill('Anuluj');
      await save(page);
    }
  }

  await page.getByRole('button', { name: 'Language', exact: true }).click();
  await page.getByRole('menuitem', { name: 'Polski' }).click();
  record('language from the shell', await visible(page.getByPlaceholder('Szukaj kluczy i wartości…')));

  await page.getByRole('button', { name: 'Ciemny motyw' }).click();
  const header = await page.locator('main header').first().evaluate((e) => getComputedStyle(e).backgroundColor);
  record("CultureWay's dark paper in its area", header === 'rgb(34, 34, 34)', header);
  record('no errors', errors.length === 0, errors.join(' | '));
  await context.close();
}

{
  const { context, page } = await open({ viewport: { width: 390, height: 800 } });
  await page.goto(`${base}/translations/`);
  await visible(page.getByRole('heading', { name: 'All' }));
  await page.getByRole('button', { name: 'Open menu' }).click();
  await page.getByTestId('way-menu').getByText('Common').click();
  const closed = await page.getByTestId('way-menu').waitFor({ state: 'hidden', timeout: 5000 }).then(() => true, () => false);
  record('phone: a namespace from the menu closes it', closed && url(page).endsWith('#/ns/Common'), url(page));
  await context.close();
}

await browser.close();
const failed = results.filter((r) => !r.ok);
for (const r of results) console.log(`${r.ok ? 'ok  ' : 'FAIL'} ${r.name}${r.detail ? ` — ${r.detail}` : ''}`);
console.log(`\n${which} ${base}: ${results.length - failed.length}/${results.length} ok`);
process.exit(failed.length ? 1 : 0);
