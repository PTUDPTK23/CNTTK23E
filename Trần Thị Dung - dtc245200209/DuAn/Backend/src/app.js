const express = require('express');
const cors = require('cors');
const config = require('./config');
const routes = require('./routes');

const app = express();

// Middlewares
app.use(cors({
  origin: config.corsOrigin,
  methods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS'],
  allowedHeaders: ['Content-Type', 'Authorization']
}));

app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// Request Logger đơn giản
app.use((req, res, next) => {
  const start = Date.now();
  res.on('finish', () => {
    const duration = Date.now() - start;
    console.log(`[${new Date().toISOString()}] ${req.method} ${req.originalUrl} ${res.statusCode} - ${duration}ms`);
  });
  next();
});

// Gắn routes
app.use('/api', routes);

// Route gốc chào mừng
app.get('/', (req, res) => {
  res.json({
    project: 'Hệ thống gửi thông báo hàng loạt (Mass Notification System)',
    status: 'Backend API is running',
    healthCheck: '/api/health'
  });
});

// Xử lý 404 Not Found
app.use((req, res, next) => {
  res.status(404).json({
    error: 'Endpoint không tồn tại',
    path: req.originalUrl,
    suggestedEndpoint: '/api/health'
  });
});

// Xử lý lỗi toàn cục (Global Error Handler)
app.use((err, req, res, next) => {
  console.error('Unhandled Server Error:', err);
  res.status(err.status || 500).json({
    error: err.message || 'Lỗi máy chủ nội bộ',
    timestamp: new Date().toISOString()
  });
});

module.exports = app;
