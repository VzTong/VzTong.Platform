import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5174,
    // Browser gọi cùng origin → Vite forward sang Identity, bỏ kiểm tra cert
    proxy: {
      '/connect': {
        target: 'https://localhost:44322',
        changeOrigin: true,
        secure: false,
      },
      '/api': {
        target: 'https://localhost:44322',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})