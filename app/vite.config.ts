import path from "path"
import react from "@vitejs/plugin-react"
import { defineConfig } from "vite"
import { inspectAttr } from 'kimi-plugin-inspect-react'

const backendTarget = process.env.VITE_BACKEND_URL || 'http://localhost:5246';
const proxyConfig = {
  // Forward all /api requests to the ASP.NET backend
  '/api': {
    target: backendTarget,
    changeOrigin: true,
  },
  // Forward SignalR WebSocket connections
  '/hubs': {
    target: backendTarget,
    changeOrigin: true,
    ws: true,
  },
  // Forward static file uploads
  '/uploads': {
    target: backendTarget,
    changeOrigin: true,
  },
};

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
    proxy: proxyConfig,
  },
  preview: {
    port: 5173,
    proxy: proxyConfig,
  },
});
