import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  define: {
    'process.env.NODE_ENV': JSON.stringify('production'),
  },
  plugins: [react()],
  build: {
    outDir: '../public',
    emptyOutDir: false,
    lib: {
      entry: 'src/plugin-ui.js',
      formats: ['es'],
      fileName: () => 'plugin-ui.js',
    },
    rollupOptions: {
      output: {
        inlineDynamicImports: true,
      },
    },
  },
});