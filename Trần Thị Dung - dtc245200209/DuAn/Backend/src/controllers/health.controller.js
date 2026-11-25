/**
 * Health Controller
 * Cung cấp thông tin trạng thái hoạt động của Backend API
 */

const startTime = Date.now();

const checkHealth = (req, res) => {
  const uptimeSeconds = Math.floor((Date.now() - startTime) / 1000);

  return res.status(200).json({
    status: 'healthy',
    message: 'Mass Notification System API is up and running',
    timestamp: new Date().toISOString(),
    uptime: `${uptimeSeconds}s`,
    environment: process.env.NODE_ENV || 'development',
    version: '1.0.0',
    phase: 'Week 1 - Project Initialization & Architecture Design',
    services: {
      api: 'online',
      database: 'pending_week2',
      rabbitmq: 'pending_week4',
      workers: 'pending_week5'
    }
  });
};

module.exports = {
  checkHealth
};
