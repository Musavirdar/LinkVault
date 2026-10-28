import path from "path"
import react from "@vitejs/plugin-react"
import { defineConfig } from "vite"
import { inspectAttr } from 'kimi-plugin-inspect-react'

// https://vite.dev/config/
export default defineConfig({
  base: './',
  plugins: [inspectAttr(), react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    proxy: {
      // Forward all /api requests to the ASP.NET backend
      '/api': {
        target: 'http://localhost:5246',
        changeOrigin: true,
      },
      // Forward SignalR WebSocket connections
      '/hubs': {
        target: 'http://localhost:5246',
        changeOrigin: true,
        ws: true,
      },
      // Forward static file uploads
      '/uploads': {
        target: 'http://localhost:5246',
        changeOrigin: true,
      },
    },
  },
});
