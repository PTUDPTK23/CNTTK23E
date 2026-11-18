const $ = (selector) => document.querySelector(selector);
const statusLabels = { received: "Đã tiếp nhận", processing: "Đang chấm", graded: "Đã chấm xong", failed: "Cần kiểm tra" };
const submissions = new Map();
let toastTimer;

const dateLabel = new Intl.DateTimeFormat("vi-VN", { weekday: "long", day: "numeric", month: "long" });
$("#today-label").textContent = dateLabel.format(new Date());

function showToast(message) {
  const toast = $("#toast");
  toast.textContent = message;
  toast.classList.add("show");
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => toast.classList.remove("show"), 3200);
}

function setConnection(state, detail) {
  const card = $(".connection-card");
  card.classList.toggle("connected", state === "connected");
  card.classList.toggle("offline", state === "offline");
  $("#connection-label").textContent = state === "connected" ? "Đã kết nối" : state === "offline" ? "Mất kết nối" : "Đang kết nối";
  $("#connection-detail").textContent = detail;
}

function updateSummary() {
  $("#stat-completed").textContent = [...submissions.values()].filter((item) => ["graded", "failed"].includes(item.status)).length;
}

function renderSubmission(item) {
  $("#empty-state").hidden = submissions.size > 0;
  let row = document.querySelector(`[data-submission-id="${CSS.escape(item.id)}"]`);
  if (!row) {
    row = document.createElement("div");
    row.className = "submission-item";
    row.dataset.submissionId = item.id;
    const icon = document.createElement("span");
    icon.className = "submission-symbol";
    icon.textContent = "↗";
    const info = document.createElement("div");
    info.className = "submission-info";
    const title = document.createElement("div");
    title.className = "submission-title";
    const id = document.createElement("div");
    id.className = "submission-id";
    const status = document.createElement("span");
    status.className = "status-chip";
    info.append(title, id);
    row.append(icon, info, status);
    $("#submission-list").prepend(row);
  }
  row.querySelector(".submission-title").textContent = `${item.studentCode} · ${item.assignmentTitle}`;
  row.querySelector(".submission-id").textContent = `Mã lượt nộp: ${item.id.slice(0, 8)} · ${new Date(item.updatedAt).toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" })}`;
  const chip = row.querySelector(".status-chip");
  chip.className = `status-chip ${item.status}`;
  chip.textContent = statusLabels[item.status] || item.status;
  updateSummary();
}

async function loadAssignments() {
  const response = await fetch("/api/assignments");
  if (!response.ok) throw new Error("Không tải được danh sách bài tập.");
  const assignments = await response.json();
  const select = $("#assignment-select");
  select.replaceChildren();
  for (const assignment of assignments) {
    const option = document.createElement("option");
    option.value = assignment.id;
    option.textContent = `${assignment.title} — ${assignment.description}`;
    select.append(option);
  }
  $("#stat-assignments").textContent = String(assignments.length).padStart(2, "0");
}

const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/submissions")
  .withAutomaticReconnect()
  .build();

connection.on("SubmissionStatusChanged", (message) => {
  const item = submissions.get(message.submissionId.toLowerCase());
  if (!item) return;
  item.status = message.status;
  item.updatedAt = message.updatedAt;
  renderSubmission(item);
  if (["graded", "failed"].includes(item.status)) {
    showToast(item.status === "graded" ? `Bài của ${item.studentCode} đã chấm xong.` : `Bài của ${item.studentCode} cần được kiểm tra.`);
  }
});

connection.onreconnecting(() => setConnection("offline", "Đang thử kết nối lại..."));
connection.onreconnected(() => setConnection("connected", "Đường truyền realtime đang hoạt động."));
connection.onclose(() => setConnection("offline", "Hãy tải lại trang để kết nối."));

async function startSignalR() {
  try {
    await connection.start();
    setConnection("connected", "Đường truyền realtime đang hoạt động.");
  } catch (error) {
    setConnection("offline", "Chưa kết nối được. Đang thử lại...");
    setTimeout(startSignalR, 3000);
  }
}

$("#submission-form").addEventListener("submit", async (event) => {
  event.preventDefault();
  const message = $("#form-message");
  const button = $("#submit-button");
  message.textContent = "";
  message.className = "form-message";
  if (connection.state !== signalR.HubConnectionState.Connected) {
    message.textContent = "Chưa kết nối realtime. Vui lòng đợi kết nối rồi thử lại.";
    return;
  }
  const assignmentId = Number($("#assignment-select").value);
  const assignmentTitle = $("#assignment-select").selectedOptions[0]?.textContent.split(" — ")[0] || "Bài tập";
  const studentCode = $("#student-code").value.trim();
  const repositoryUrl = $("#repository-url").value.trim();
  if (!assignmentId || !studentCode || !repositoryUrl) return;

  button.disabled = true;
  button.querySelector("span:nth-child(2)").textContent = "Đang gửi bài...";
  try {
    const response = await fetch(`/api/assignments/${assignmentId}/submissions`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ studentCode, repositoryUrl })
    });
    const result = await response.json();
    if (!response.ok) throw new Error(result.message || "Không thể nộp bài.");

    const item = { id: result.id.toLowerCase(), studentCode: result.studentCode, assignmentTitle, status: result.status, updatedAt: result.updatedAt };
    submissions.set(item.id, item);
    renderSubmission(item);
    await connection.invoke("WatchSubmission", result.id);
    message.textContent = "Đã nhận bài. Trạng thái chấm sẽ cập nhật trực tiếp ở bên cạnh.";
    message.classList.add("success");
    $("#repository-url").value = "";
    showToast("Nộp bài thành công!");
  } catch (error) {
    message.textContent = error.message || "Có lỗi xảy ra, vui lòng thử lại.";
  } finally {
    button.disabled = false;
    button.querySelector("span:nth-child(2)").textContent = "Nộp bài tập";
  }
});

Promise.all([loadAssignments(), startSignalR()]).catch((error) => {
  $("#form-message").textContent = error.message;
});
