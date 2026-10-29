import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/avatars': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
  build: {
    rollupOptions: {
      output: {
        // Biblioteke se menjaju ređe od koda aplikacije: u svom fajlu ostaju u kešu browsera i posle deploy-a.
        manualChunks: (id) =>
          id.includes('/node_modules/') ? 'vendor' : undefined,
      },
    },
  },
});
