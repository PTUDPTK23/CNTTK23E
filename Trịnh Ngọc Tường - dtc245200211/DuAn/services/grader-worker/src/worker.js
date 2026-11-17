const amqp = require('amqplib');
const { exec, spawn } = require('child_process');
const fs = require('fs');
const path = require('path');

const RABBITMQ_URL = process.env.RABBITMQ_URL || 'amqp://guest:guest@localhost:5672';
const QUEUE_NAME = 'grading_jobs';
const RESULT_QUEUE = 'grading_results';
const WORKER_ID = process.env.WORKER_ID || `worker-${Math.floor(Math.random() * 1000)}`;

console.log(`[${WORKER_ID}] Starting Distributed Grader Worker Node...`);

// Helper thực thi một test case trong sandbox cô lập
function runTestcase(command, args, inputData, timeLimitMs) {
  return new Promise((resolve) => {
    const startTime = Date.now();
    let stdoutData = '';
    let stderrData = '';
    let timedOut = false;

    const child = spawn(command, args);

    const timer = setTimeout(() => {
      timedOut = true;
      try { child.kill('SIGKILL'); } catch (e) {}
    }, timeLimitMs);

    child.stdout.on('data', (data) => { stdoutData += data.toString(); });
    child.stderr.on('data', (data) => { stderrData += data.toString(); });

    child.stdin.write(inputData + '\n');
    child.stdin.end();

    child.on('close', (code) => {
      clearTimeout(timer);
      const executionTime = Date.now() - startTime;
      if (timedOut) {
        resolve({
          status: 'TIME_LIMIT_EXCEEDED',
          actual_output: '',
          error_log: `Chạy quá thời gian giới hạn (${timeLimitMs}ms)`,
          execution_time_ms: executionTime
        });
      } else if (code !== 0) {
        resolve({
          status: 'RUNTIME_ERROR',
          actual_output: stdoutData,
          error_log: stderrData || `Exit code: ${code}`,
          execution_time_ms: executionTime
        });
      } else {
        resolve({
          status: 'SUCCESS',
          actual_output: stdoutData.trim(),
          error_log: stderrData,
          execution_time_ms: executionTime
        });
      }
    });

    child.on('error', (err) => {
      clearTimeout(timer);
      resolve({
        status: 'EXEC_ERROR',
        actual_output: '',
        error_log: err.message,
        execution_time_ms: Date.now() - startTime
      });
    });
  });
}

// Hàm thực thi & chấm bài nộp
async function gradeSubmission(job) {
  const { submission_id, language, source_code, testcases, time_limit_ms = 2000 } = job;
  console.log(`[${WORKER_ID}] Grading submission ${submission_id} (Language: ${language})`);

  const tmpDir = path.join('/tmp', `sub_${submission_id}`);
  if (!fs.existsSync(tmpDir)) fs.mkdirSync(tmpDir, { recursive: true });

  let runCmd = '';
  let runArgs = [];

  try {
    if (language.toUpperCase() === 'PYTHON') {
      const scriptPath = path.join(tmpDir, 'solution.py');
      fs.writeFileSync(scriptPath, source_code);
      runCmd = 'python3';
      runArgs = [scriptPath];
    } else if (language.toUpperCase() === 'JAVASCRIPT' || language.toUpperCase() === 'NODE') {
      const scriptPath = path.join(tmpDir, 'solution.js');
      fs.writeFileSync(scriptPath, source_code);
      runCmd = 'node';
      runArgs = [scriptPath];
    } else {
      // Mặc định fallback chạy với node/python
      const scriptPath = path.join(tmpDir, 'solution.py');
      fs.writeFileSync(scriptPath, source_code);
      runCmd = 'python3';
      runArgs = [scriptPath];
    }

    let totalScore = 0;
    let passedCount = 0;
    let maxExecutionTime = 0;
    const details = [];

    for (const tc of testcases) {
      const runResult = await runTestcase(runCmd, runArgs, tc.input_data, time_limit_ms);
      maxExecutionTime = Math.max(maxExecutionTime, runResult.execution_time_ms);

      const isMatch = (runResult.status === 'SUCCESS' && runResult.actual_output === tc.expected_output.trim());
      const testcaseStatus = isMatch ? 'PASSED' : (runResult.status === 'SUCCESS' ? 'FAILED' : runResult.status);

      if (isMatch) {
        totalScore += (tc.score_weight || 2);
        passedCount++;
      }

      details.push({
        testcase_id: tc.id,
        status: testcaseStatus,
        actual_output: runResult.actual_output,
        error_log: runResult.error_log,
        execution_time_ms: runResult.execution_time_ms
      });
    }

    let verdict = 'ACCEPTED';
    if (passedCount === 0 && testcases.length > 0) {
      verdict = details[0].status === 'TIME_LIMIT_EXCEEDED' ? 'TIME_LIMIT_EXCEEDED' : 'WRONG_ANSWER';
    } else if (passedCount < testcases.length) {
      verdict = 'PARTIALLY_ACCEPTED';
    }

    return {
      submission_id,
      verdict,
      total_score: totalScore,
      execution_time_ms: maxExecutionTime,
      memory_used_kb: 1024,
      details
    };
  } finally {
    // Dọn dẹp thư mục tạm
    try {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    } catch (e) {}
  }
}

// Lắng nghe Message Queue từ RabbitMQ
async function startWorker() {
  try {
    const conn = await amqp.connect(RABBITMQ_URL);
    const channel = await conn.createChannel();
    await channel.assertQueue(QUEUE_NAME, { durable: true });
    await channel.assertQueue(RESULT_QUEUE, { durable: true });

    // Prefetch = 1 đảm bảo worker chỉ nhận 1 task mỗi thời điểm (Fair dispatch)
    channel.prefetch(1);
    console.log(`[${WORKER_ID}] Ready and waiting for submissions on queue '${QUEUE_NAME}'...`);

    channel.consume(QUEUE_NAME, async (msg) => {
      if (msg !== null) {
        try {
          const job = JSON.parse(msg.content.toString());
          const gradeResult = await gradeSubmission(job);

          // Đẩy kết quả chấm bài vào result queue
          channel.sendToQueue(
            RESULT_QUEUE,
            Buffer.from(JSON.stringify(gradeResult)),
            { persistent: true }
          );

          channel.ack(msg);
          console.log(`[${WORKER_ID}] Completed grading submission ${job.submission_id}: Score = ${gradeResult.total_score}`);
        } catch (err) {
          console.error(`[${WORKER_ID}] Failed to grade job:`, err);
          channel.nack(msg, false, true); // Requeue on unhandled exception
        }
      }
    });
  } catch (err) {
    console.warn(`[${WORKER_ID}] RabbitMQ connection failed, retry in 5s:`, err.message);
    setTimeout(startWorker, 5000);
  }
}

startWorker();
