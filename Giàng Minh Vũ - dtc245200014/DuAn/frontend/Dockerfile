FROM node:24-alpine AS build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
ARG VITE_API_URL=/api
ARG VITE_DASHBOARD_API_URL=/dashboard-api
ARG VITE_DASHBOARD_HUB_URL=/hubs/dashboard
ENV VITE_API_URL=$VITE_API_URL VITE_DASHBOARD_API_URL=$VITE_DASHBOARD_API_URL VITE_DASHBOARD_HUB_URL=$VITE_DASHBOARD_HUB_URL
RUN npm run build
FROM nginx:1.27-alpine
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=3s --retries=3 CMD wget -q -O /dev/null http://127.0.0.1/ || exit 1
