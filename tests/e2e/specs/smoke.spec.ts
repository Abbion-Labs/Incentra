import { test, expect } from '@playwright/test';

const credentials = {
  evaluator: { email: 'evaluator@local.dev', password: 'Eval123!' },
  controller: { email: 'controller@local.dev', password: 'Control123!' },
  employee: { email: 'marko@local.dev', password: 'Marko123!' },
};

async function login(page: import('@playwright/test').Page, email: string, password: string) {
  await page.goto('/login');
  await page.getByLabel(/email/i).fill(email);
  await page.getByLabel(/password|lozinka/i).fill(password);
  await page.getByRole('button', { name: /sign in|prijavi/i }).click();
}

test.describe('Smoke', () => {
  test('evaluator lands on workflow area', async ({ page }) => {
    await login(page, credentials.evaluator.email, credentials.evaluator.password);
    await expect(page).toHaveURL(/\/evaluator/);
    await expect(page.locator('body')).toContainText(/evaluator|ocenjivač|zaposleni|employees/i);
  });

  test('controller sees workflow', async ({ page }) => {
    await login(page, credentials.controller.email, credentials.controller.password);
    await expect(page).toHaveURL(/\/controller/);
  });

  test('employee sees own evaluations', async ({ page }) => {
    await login(page, credentials.employee.email, credentials.employee.password);
    await expect(page).toHaveURL(/\/employee/);
  });
});
