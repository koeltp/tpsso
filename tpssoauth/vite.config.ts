import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  return {
    plugins: [vue()],
    resolve: {
      alias: {
        '@': resolve(__dirname, 'src')
      }
    },
    server: {
      port: 3010,
      proxy: {
        '/api': {
          target: env.VITE_API_TARGET,
          changeOrigin: true,
          secure: false
        },
        '/connect': {
          target: env.VITE_API_TARGET,
          changeOrigin: true,
          secure: false
        },
        '/.well-known': {
          target: env.VITE_API_TARGET,
          changeOrigin: true,
          secure: false
        },
        // 第三方登录回调路径（如 /signin-github）
        '/signin-': {
          target: env.VITE_API_TARGET,
          changeOrigin: true,
          secure: false
        }
      }
    }
  }
})
