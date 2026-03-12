import { defineConfig } from '@vben/vite-config';

export default defineConfig(async () => {
  return {
    application: {},
    vite: {
      server: {
        proxy: {
          '/api': {
            changeOrigin: true,
            rewrite: (path) => path.replace(/^\/api/, ''),
            // Proxy to .NET backend
            target: 'http://localhost:5180',
            ws: true,
          },
        },
      },
    },
  };
});
