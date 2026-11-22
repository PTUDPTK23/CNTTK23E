const express = require('express');
const router = express.Router();
const healthController = require('../controllers/health.controller');

// GET /api/health - Endpoint kiểm tra tình trạng máy chủ
router.get('/', healthController.checkHealth);

module.exports = router;
