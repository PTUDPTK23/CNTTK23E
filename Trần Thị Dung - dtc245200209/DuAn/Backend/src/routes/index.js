const express = require('express');
const router = express.Router();
const healthRoute = require('./health.route');

// Định tuyến API
router.use('/health', healthRoute);

// Route mặc định cho API root
router.get('/', (req, res) => {
  res.json({
    name: 'Mass Notification System API',
    version: '1.0.0',
    documentation: 'See README.md for endpoint specifications',
    endpoints: {
      health: '/api/health'
    }
  });
});

module.exports = router;
