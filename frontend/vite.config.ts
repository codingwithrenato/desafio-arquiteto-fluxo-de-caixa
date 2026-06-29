import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

// Em desenvolvimento (npm run dev) o Vite faz o mesmo proxy que o Nginx faz em produção,
// mantendo o frontend "same-origin" com as APIs (sem CORS).
export default defineConfig({
  plugins: [vue()],
  server: {
    port: 8080,
    proxy: {
      "/api/lanc": {
        target: "http://localhost:8081",
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/lanc/, ""),
      },
      "/api/cons": {
        target: "http://localhost:8082",
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/cons/, ""),
      },
    },
  },
});
