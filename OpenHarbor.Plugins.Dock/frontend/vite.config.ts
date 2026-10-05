import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  base: './',
  plugins: [react()],
  build: {
    outDir: '../public',
    emptyOutDir: true,
    assetsDir: 'assets',
    rollupOptions: {
      external: (id) => id === '/_host/plugin-ui.js' || /^(react|react-dom)(\/.*)?$/.test(id),
      output: {
        paths: (id) => /^(react|react-dom)(\/.*)?$/.test(id) ? '/_host/plugin-ui.js' : id,
      },
    },
  },
});
