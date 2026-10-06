import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import cssInjectedByJsPlugin from 'vite-plugin-css-injected-by-js';

export default defineConfig({
  base: './',
  plugins: [react(), cssInjectedByJsPlugin()],
  build: {
    outDir: '../public',
    emptyOutDir: true,
    assetsDir: 'assets',
    lib: {
      entry: 'src/window.tsx',
      formats: ['es'],
      fileName: () => 'window.js',
    },
    rollupOptions: {
      external: (id) => id === '/_host/plugin-ui.js' || /^(react|react-dom)(\/.*)?$/.test(id),
      output: {
        paths: (id) => /^(react|react-dom)(\/.*)?$/.test(id) ? '/_host/plugin-ui.js' : id,
      },
    },
  },
});