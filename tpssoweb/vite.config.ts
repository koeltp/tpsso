import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src')
    }
  },
  server: {
    port: 3008,
    proxy: {
      '/api': {
        target: 'https://localhost:7044',
        changeOrigin: true,
        secure: false
      },
      '/connect': {
        target: 'https://localhost:7044',
        changeOrigin: true,
        secure: false
      }
    }
  }
})