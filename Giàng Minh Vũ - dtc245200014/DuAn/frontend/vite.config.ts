import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
export default defineConfig({plugins:[react(),tailwindcss()],server:{host:'0.0.0.0',proxy:{'/api':{target:process.env.VITE_EVENT_API_TARGET||'http://localhost:5001',changeOrigin:true,rewrite:p=>p.replace(/^\/api/,'')},'/dashboard-api':{target:process.env.VITE_DASHBOARD_API_TARGET||'http://localhost:5002',changeOrigin:true,rewrite:p=>p.replace(/^\/dashboard-api/,'')},'/hubs':{target:process.env.VITE_DASHBOARD_API_TARGET||'http://localhost:5002',ws:true,changeOrigin:true}}}});
