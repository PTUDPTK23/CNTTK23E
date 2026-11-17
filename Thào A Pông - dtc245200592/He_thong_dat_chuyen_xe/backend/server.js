
const express = require("express");
const cors = require("cors");
require("dotenv").config();

const app = express();

app.use(cors());
app.use(express.json());

// API kiểm tra hệ thống
app.get("/", (req, res) => {
  res.json({
    message: "He thong dat chuyen xe API dang hoat dong!",
    status: "success"
  });
});

// API kiểm tra sức khỏe server
app.get("/api/health", (req, res) => {
  res.json({
    status: "OK",
    message: "Backend hoat dong binh thuong"
  });
});

const PORT = process.env.PORT || 5000;

app.listen(PORT, () => {
  console.log(`Backend dang chay tai http://localhost:${PORT}`);
});
