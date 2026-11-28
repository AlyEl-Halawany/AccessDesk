import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';

const baseUrl = process.env.ACCESSDESK_URL ?? 'http://127.0.0.1:5173';
const outputDir = fileURLToPath(new URL('../docs/screenshots/', import.meta.url));
await mkdir(outputDir, { recursive: true });

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({ viewport: { width: 1440, height: 1000 }, deviceScaleFactor: 1 });
page.on('pageerror', (error) => console.error(`Browser error: ${error.message}`));

async function screenshot(name) {
  await page.evaluate(async () => {
    await document.fonts.ready;
  });
  await page.mouse.move(1439, 999);
  await page.waitForTimeout(900);
  await page.screenshot({ path: `${outputDir}/${name}.png`, fullPage: true });
  console.log(`Captured ${name}.png at ${page.url()}`);
}

async function login(accountLabel) {
  await page.goto(`${baseUrl}/login`, { waitUntil: 'networkidle' });
  await page.getByRole('button', { name: accountLabel }).click();
  await page.getByRole('button', { name: 'Sign In' }).click();
  await page.waitForURL(`${baseUrl}/`, { timeout: 15000 });
  await page.getByRole('heading', { name: 'Dashboard' }).waitFor();
}

try {
  await page.goto(`${baseUrl}/login`, { waitUntil: 'networkidle' });
  await screenshot('login-redesigned');

  await login('Employee (Charlie)');
  await screenshot('dashboard-redesigned');
  await page.goto(`${baseUrl}/requests/new`, { waitUntil: 'networkidle' });
  const applicationSelect = page.locator('select').first();
  const githubOption = applicationSelect.locator('option').filter({ hasText: 'GitHub Enterprise' }).first();
  const githubApplicationId = await githubOption.getAttribute('value');
  if (!githubApplicationId) throw new Error('GitHub Enterprise was not available in the application list.');
  await applicationSelect.selectOption(githubApplicationId);
  await screenshot('new-request-redesigned');
  await page.goto(`${baseUrl}/my-access`, { waitUntil: 'networkidle' });
  await screenshot('my-access-redesigned');

  await login('Manager (Alice)');
  await page.goto(`${baseUrl}/approvals`, { waitUntil: 'networkidle' });
  await screenshot('approval-inbox-redesigned');

  await login('App Owner (Bob)');
  await page.goto(`${baseUrl}/approvals`, { waitUntil: 'networkidle' });
  await screenshot('approval-inbox-owner-redesigned');

  await login('Security Admin');
  await page.goto(`${baseUrl}/audit-logs`, { waitUntil: 'networkidle' });
  await screenshot('audit-log-redesigned');
  await page.goto(`${baseUrl}/applications`, { waitUntil: 'networkidle' });
  await screenshot('applications-redesigned');
  await page.goto(`${baseUrl}/jml`, { waitUntil: 'networkidle' });
  await screenshot('jml-console-redesigned');
  await page.goto(`${baseUrl}/reviews`, { waitUntil: 'networkidle' });
  await screenshot('access-reviews-redesigned');
} finally {
  await browser.close();
}
