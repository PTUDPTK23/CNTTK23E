/**
 * Application Main Controller (Tuần 1)
 * Điều khiển Dashboard, Sidebar, và Kiểm tra Health Check Backend API
 */

const API_BASE_URL = 'http://localhost:5000';

document.addEventListener('DOMContentLoaded', () => {
  // 1. Kiểm tra xác thực (Auth Guard)
  if (!Auth.requireAuth()) return;

  const currentUser = Auth.getCurrentUser();
  renderUserInfo(currentUser);

  // 2. Mock Campaigns Data (Mô phỏng dữ liệu khung hiển thị)
  renderMockCampaigns();

  // 3. Khởi chạy kiểm tra Health Check từ Backend API
  checkBackendHealth();

  // 4. Sự kiện nút kiểm tra lại Health Check
  const btnRefreshHealth = document.getElementById('btnRefreshHealth');
  if (btnRefreshHealth) {
    btnRefreshHealth.addEventListener('click', () => {
      checkBackendHealth(true);
    });
  }

  // 5. Sự kiện Đăng xuất
  const btnLogout = document.getElementById('btnLogout');
  if (btnLogout) {
    btnLogout.addEventListener('click', (e) => {
      e.preventDefault();
      if (confirm('Bạn có chắc chắn muốn đăng xuất khỏi hệ thống?')) {
        Auth.logout();
      }
    });
  }
});

/**
 * Hiển thị thông tin người dùng lên Sidebar và Header
 */
function renderUserInfo(user) {
  if (!user) return;
  const userNameEl = document.getElementById('currentUserName');
  const userRoleEl = document.getElementById('currentUserRole');
  const userAvatarEl = document.getElementById('currentUserAvatar');

  if (userNameEl) userNameEl.textContent = user.name || user.email;
  if (userRoleEl) userRoleEl.textContent = user.role === 'admin' ? 'Quản trị viên' : 'Nhân viên chiến dịch';
  if (userAvatarEl) {
    const firstChar = (user.name || user.email || 'U').charAt(0).toUpperCase();
    userAvatarEl.textContent = firstChar;
  }
}

/**
 * Gọi API Backend Express để kiểm tra /api/health
 */
async function checkBackendHealth(manual = false) {
  const statusPill = document.getElementById('systemStatusPill');
  const statusDot = document.getElementById('systemStatusDot');
  const statusText = document.getElementById('systemStatusText');
  const healthBox = document.getElementById('healthBoxContent');
  const healthMeta = document.getElementById('healthMeta');

  if (statusText) statusText.textContent = 'Đang kiểm tra Backend...';
  if (healthBox) healthBox.textContent = '// Đang gửi yêu cầu GET tới ' + API_BASE_URL + '/api/health ...';

  try {
    const response = await fetch(`${API_BASE_URL}/api/health`, {
      method: 'GET',
      headers: {
        'Accept': 'application/json'
      }
    });

    if (response.ok) {
      const data = await response.json();

      // Cập nhật giao diện Trực tuyến
      if (statusDot) {
        statusDot.className = 'status-dot online';
      }
      if (statusText) {
        statusText.textContent = `Backend: Online (${data.uptime})`;
      }
      if (healthBox) {
        healthBox.textContent = JSON.stringify(data, null, 2);
      }
      if (healthMeta) {
        healthMeta.innerHTML = `
          <span class="badge badge-success">HTTP 200 OK</span>
          <span style="font-size: 12px; color: #64748b; margin-left: 10px;">
            Môi trường: <b>${data.environment}</b> | Phiên bản: <b>v${data.version}</b>
          </span>
        `;
      }
    } else {
      throw new Error(`HTTP ${response.status}`);
    }
  } catch (error) {
    // Cập nhật giao diện Ngoại tuyến
    if (statusDot) {
      statusDot.className = 'status-dot offline';
    }
    if (statusText) {
      statusText.textContent = 'Backend: Chưa kết nối (Offline)';
    }
    if (healthBox) {
      healthBox.textContent = 
`[!] Không thể kết nối tới Backend tại ${API_BASE_URL}/api/health.
Nguyên nhân có thể do máy chủ Express chưa khởi động.

Cách khởi động máy chủ Backend trên Windows:
1. Mở một cửa sổ Terminal mới
2. cd Backend
3. npm start (hoặc npm run dev)
4. Bấm nút "Kiểm tra lại kết nối" ở trên để cập nhật lại trạng thái.`;
    }
    if (healthMeta) {
      healthMeta.innerHTML = `<span class="badge badge-danger">Connection Refused</span>`;
    }
  }
}

/**
 * Hiển thị bảng danh sách chiến dịch mô phỏng (Mock UI)
 */
function renderMockCampaigns() {
  const tableBody = document.getElementById('campaignTableBody');
  if (!tableBody) return;

  const mockCampaigns = [
    {
      id: 'CMP-001',
      name: 'Thông báo lịch thi học kỳ 1 năm học 2026-2027',
      group: 'Sinh viên Khóa K23',
      total: 450,
      status: 'PENDING',
      statusText: 'Chờ gửi',
      statusBadge: 'badge-warning',
      createdAt: '2026-11-17 08:30'
    },
    {
      id: 'CMP-002',
      name: 'Khảo sát ý kiến sinh viên về môn học phân tán',
      group: 'Lớp CNTT K23E',
      total: 65,
      status: 'PROCESSING',
      statusText: 'Đang xử lý',
      statusBadge: 'badge-info',
      createdAt: '2026-11-17 14:15'
    },
    {
      id: 'CMP-003',
      name: 'Xác nhận thông tin học bổng khuyến khích học tập',
      group: 'Ban Cán sự Lớp',
      total: 18,
      status: 'COMPLETED',
      statusText: 'Hoàn thành',
      statusBadge: 'badge-success',
      createdAt: '2026-11-16 10:00'
    }
  ];

  tableBody.innerHTML = mockCampaigns.map(c => `
    <tr>
      <td><strong>${c.id}</strong></td>
      <td>${c.name}</td>
      <td><span class="badge badge-secondary">${c.group}</span></td>
      <td><b>${c.total}</b> người</td>
      <td><span class="badge ${c.statusBadge}">${c.statusText}</span></td>
      <td>${c.createdAt}</td>
      <td>
        <button class="btn btn-sm btn-primary" onclick="alert('Chi tiết chiến dịch ${c.id} sẽ được kết nối API ở Tuần 3.');">Xem</button>
      </td>
    </tr>
  `).join('');
}
