const app = require('./app');
const config = require('./config');

const PORT = config.port;

const server = app.listen(PORT, () => {
  console.log('====================================================');
  console.log(`🚀 Mass Notification System - Backend API`);
  console.log(`📡 Server đang chạy tại: http://localhost:${PORT}`);
  console.log(`🩺 Health check endpoint: http://localhost:${PORT}/api/health`);
  console.log(`🌍 Môi trường: ${config.env}`);
  console.log('====================================================');
});

// Xử lý dừng máy chủ an toàn (Graceful Shutdown)
process.on('SIGINT', () => {
  console.log('\n🛑 Đang tắt Backend Server...');
  server.close(() => {
    console.log('✅ Server đã dừng an toàn.');
    process.exit(0);
  });
});

process.on('SIGTERM', () => {
  console.log('\n🛑 SIGTERM nhận được. Đang tắt server...');
  server.close(() => {
    process.exit(0);
  });
});
