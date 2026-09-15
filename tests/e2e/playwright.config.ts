import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './specs',
  timeout: 60_000,
  retries: process.env.CI ? 1 : 0,
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
  webServer: process.env.CI
    ? undefined
    : [
        {
          command: 'dotnet run --project ../../src/VariableCompensation.Host/VariableCompensation.Host.csproj',
          url: 'http://localhost:5000/health',
          reuseExistingServer: true,
          timeout: 120_000,
        },
        {
          command: 'npm run dev -- --host 127.0.0.1 --port 5173',
          cwd: '../../frontend',
          url: 'http://localhost:5173',
          reuseExistingServer: true,
          timeout: 120_000,
        },
      ],
});
