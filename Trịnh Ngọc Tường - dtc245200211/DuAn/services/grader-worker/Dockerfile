FROM node:18-alpine

WORKDIR /app

# Cài đặt docker cli nếu cần giao tiếp trực tiếp qua socket
RUN apk add --no-cache docker-cli python3 py3-pip openjdk17-jre gcc g++

COPY package*.json ./
RUN npm install --production

COPY . .

CMD ["node", "src/worker.js"]
