const express = require('express');
const http = require('http');
const { Server } = require('socket.io');
const cors = require('cors');
const { Pool } = require('pg');
const amqp = require('amqplib');
const path = require('path');
const fs = require('fs');

const app = express();
const server = http.createServer(app);
const io = new Server(server, {
  cors: { origin: '*', methods: ['GET', 'POST'] }
});

const PORT = process.env.PORT || 5000;
const DB_CONFIG = {
  host: process.env.DB_HOST || 'localhost',
  port: parseInt(process.env.DB_PORT || '5432', 10),
  user: process.env.DB_USER || 'postgres',
  password: process.env.DB_PASSWORD || 'postgrespassword',
  database: process.env.DB_NAME || 'autograder_db'
};
const RABBITMQ_URL = process.env.RABBITMQ_URL || 'amqp://guest:guest@localhost:5672';
const QUEUE_NAME = 'grading_jobs';
const RESULT_QUEUE = 'grading_results';

const pool = new Pool(DB_CONFIG);
let amqpChannel = null;

app.use(cors());
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// Realtime WebSocket Connection
io.on('connection', (socket) => {
  console.log(`[WebSocket] Client connected: ${socket.id}`);
  socket.on('join_submission', (submissionId) => {
    socket.join(`sub_${submissionId}`);
    console.log(`[WebSocket] Client joined room sub_${submissionId}`);
  });
  socket.on('disconnect', () => {
    console.log(`[WebSocket] Client disconnected: ${socket.id}`);
  });
});

// RabbitMQ Connect & Listeners
async function setupRabbitMQ() {
  try {
    const conn = await amqp.connect(RABBITMQ_URL);
    amqpChannel = await conn.createChannel();
    await amqpChannel.assertQueue(QUEUE_NAME, { durable: true });
    await amqpChannel.assertQueue(RESULT_QUEUE, { durable: true });
    console.log('[RabbitMQ] Connected and queues asserted successfully.');

    // Consume result queue to update DB and push WebSocket
    amqpChannel.consume(RESULT_QUEUE, async (msg) => {
      if (msg !== null) {
        try {
          const result = JSON.parse(msg.content.toString());
          console.log(`[RabbitMQ] Received result for submission ${result.submission_id}: ${result.verdict} (${result.total_score} pts)`);
          
          // Update DB
          await pool.query(
            `UPDATE submissions 
             SET status = 'COMPLETED', verdict = $1, total_score = $2, 
                 execution_time_ms = $3, memory_used_kb = $4, updated_at = NOW()
             WHERE id = $5`,
            [result.verdict, result.total_score, result.execution_time_ms || 0, result.memory_used_kb || 0, result.submission_id]
          );

          // Update details
          if (Array.isArray(result.details)) {
            for (const d of result.details) {
              await pool.query(
                `INSERT INTO submission_details (submission_id, testcase_id, status, actual_output, error_log, execution_time_ms)
                 VALUES ($1, $2, $3, $4, $5, $6)`,
                [result.submission_id, d.testcase_id, d.status, d.actual_output || '', d.error_log || '', d.execution_time_ms || 0]
              );
            }
          }

          // Emit WebSocket event to client
          io.to(`sub_${result.submission_id}`).emit('submission_update', {
            submission_id: result.submission_id,
            status: 'COMPLETED',
            verdict: result.verdict,
            total_score: result.total_score,
            details: result.details
          });

          amqpChannel.ack(msg);
        } catch (err) {
          console.error('[RabbitMQ] Error handling result:', err);
          amqpChannel.nack(msg, false, false);
        }
      }
    });
  } catch (err) {
    console.warn('[RabbitMQ] Connection failed, retrying in 5s...', err.message);
    setTimeout(setupRabbitMQ, 5000);
  }
}

// REST API ROUTES
app.get('/api/health', (req, res) => {
  res.json({
    status: 'UP',
    service: 'autograder-core-service',
    timestamp: new Date().toISOString(),
    author: 'Trịnh Ngọc Tường - DTC245200211'
  });
});

// Lấy danh sách bài tập 9 tuần
app.get('/api/assignments', async (req, res) => {
  try {
    const result = await pool.query(
      `SELECT a.*, c.course_name, c.course_code,
        (SELECT COUNT(*) FROM testcases t WHERE t.assignment_id = a.id) as total_testcases
       FROM assignments a
       JOIN courses c ON a.course_id = c.id
       ORDER BY a.week_no ASC`
    );
    res.json(result.rows);
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: 'Database error', message: err.message });
  }
});

// Lấy chi tiết bài tập kèm testcases công khai
app.get('/api/assignments/:id', async (req, res) => {
  try {
    const { id } = req.params;
    const assignRes = await pool.query('SELECT * FROM assignments WHERE id = $1', [id]);
    if (assignRes.rows.length === 0) return res.status(404).json({ error: 'Assignment not found' });

    const testsRes = await pool.query(
      'SELECT id, input_data, expected_output, score_weight, order_num FROM testcases WHERE assignment_id = $1 AND is_hidden = false ORDER BY order_num ASC',
      [id]
    );

    res.json({
      ...assignRes.rows[0],
      sample_testcases: testsRes.rows
    });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

// Sinh viên nộp bài
app.post('/api/submissions', async (req, res) => {
  try {
    const { assignment_id, language, source_code, student_code = 'DTC245200211' } = req.body;

    if (!assignment_id || !language || !source_code) {
      return res.status(400).json({ error: 'Missing assignment_id, language, or source_code' });
    }

    // Lấy user_id của sinh viên
    let userRes = await pool.query('SELECT id FROM users WHERE student_code = $1', [student_code]);
    let userId = userRes.rows[0]?.id;
    if (!userId) {
      const newUser = await pool.query(
        "INSERT INTO users (username, full_name, student_code, role, password_hash) VALUES ($1, $2, $3, 'STUDENT', 'pass') RETURNING id",
        [student_code.toLowerCase(), 'Trịnh Ngọc Tường', student_code]
      );
      userId = newUser.rows[0].id;
    }

    // Tạo bản ghi submission
    const subRes = await pool.query(
      `INSERT INTO submissions (assignment_id, user_id, language, source_code, status, verdict)
       VALUES ($1, $2, $3, $4, 'PENDING', 'WAITING')
       RETURNING *`,
      [assignment_id, userId, language, source_code]
    );
    const submission = subRes.rows[0];

    // Lấy danh sách toàn bộ testcases (kể cả hidden) để gửi cho Worker
    const testcasesRes = await pool.query(
      'SELECT id, input_data, expected_output, score_weight FROM testcases WHERE assignment_id = $1 ORDER BY order_num ASC',
      [assignment_id]
    );

    // Đẩy task vào RabbitMQ
    if (amqpChannel) {
      const jobPayload = {
        submission_id: submission.id,
        assignment_id,
        language,
        source_code,
        time_limit_ms: 2000,
        memory_limit_mb: 256,
        testcases: testcasesRes.rows
      };

      amqpChannel.sendToQueue(
        QUEUE_NAME,
        Buffer.from(JSON.stringify(jobPayload)),
        { persistent: true }
      );
      console.log(`[RabbitMQ] Pushed submission job ${submission.id} to queue '${QUEUE_NAME}'`);
    } else {
      console.warn('[RabbitMQ] Channel not ready, job saved with PENDING state');
    }

    res.status(201).json({
      message: 'Submission received and queued for distributed grading',
      submission
    });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: err.message });
  }
});

// Lấy lịch sử bài nộp
app.get('/api/submissions/:id', async (req, res) => {
  try {
    const { id } = req.params;
    const subRes = await pool.query(
      `SELECT s.*, u.full_name, u.student_code, a.title as assignment_title, a.week_no
       FROM submissions s
       JOIN users u ON s.user_id = u.id
       JOIN assignments a ON s.assignment_id = a.id
       WHERE s.id = $1`,
      [id]
    );
    if (subRes.rows.length === 0) return res.status(404).json({ error: 'Submission not found' });

    const detailsRes = await pool.query(
      `SELECT sd.*, t.order_num, t.score_weight, t.is_hidden
       FROM submission_details sd
       JOIN testcases t ON sd.testcase_id = t.id
       WHERE sd.submission_id = $1
       ORDER BY t.order_num ASC`,
      [id]
    );

    res.json({
      submission: subRes.rows[0],
      details: detailsRes.rows
    });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

// Khởi chạy server
server.listen(PORT, () => {
  console.log(`[Core Service] Running on http://localhost:${PORT}`);
  setupRabbitMQ();
});
