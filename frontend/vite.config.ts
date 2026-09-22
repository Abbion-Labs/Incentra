import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

interface HeaderRule {
  headers: Array<{ key: string; value: string }>;
}

// The deployed headers, above all the Content-Security-Policy, live in vercel.json. `npm run preview` serves
// the built app with the same ones, so the policy can be tried out locally instead of only after a deploy.
// The dev server is left alone: Vite's own dev tooling does not survive a policy this strict.
function deployedHeaders(): Record<string, string> {
  try {
    const vercelConfig = JSON.parse(
      readFileSync(resolve(__dirname, '../vercel.json'), 'utf8'),
    ) as { services?: { frontend?: { headers?: HeaderRule[] } } };

    const rules = vercelConfig.services?.frontend?.headers ?? [];
    return Object.fromEntries(
      rules.flatMap((rule) =>
        rule.headers.map(({ key, value }) => [key, value]),
      ),
    );
  } catch {
    return {};
  }
}

const apiProxy = {
  '/api': {
    target: 'http://localhost:5000',
    changeOrigin: true,
  },
  '/avatars': {
    target: 'http://localhost:5000',
    changeOrigin: true,
  },
};

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: apiProxy,
  },
  preview: {
    headers: deployedHeaders(),
    proxy: apiProxy,
  },
});
