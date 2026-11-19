# Infrastructure
`docker-compose.yml` (PostgreSQL 16 + RabbitMQ 4) là file hiện có của bạn — gói này KHÔNG ghi đè nó.
`docker-compose.apps.yml` là tùy chọn để chạy API/Worker trong Docker. Không chạy `down -v` hay `system prune --volumes`.
