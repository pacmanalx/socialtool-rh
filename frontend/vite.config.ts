import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const apiTarget = env.VITE_API_URL || 'http://localhost:5000'

  return {
    plugins: [
      react(),
      tailwindcss()
    ],
    build: {
      // Pasta dos arquivos com hash. Trocada de "assets" em 27/09/2026: o CDN guardou respostas erradas
      // para os nomes antigos, e um caminho novo dispensa limpar o cache.
      assetsDir: 'static'
    },
    server: {
      port: Number(env.VITE_PORT) || 3000,
      proxy: {
        '/api': {
          target: apiTarget,
          changeOrigin: true,
          secure: false
        },
        '/hubs': {
          target: apiTarget,
          changeOrigin: true,
          ws: true,
          secure: false
        }
      }
    }
  }
})
