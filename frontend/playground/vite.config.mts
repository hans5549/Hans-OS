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
            // 後端 API 伺服器
            target: 'http://localhost:5180',
            ws: true,
          },
        },
      },
    },
  };
});
