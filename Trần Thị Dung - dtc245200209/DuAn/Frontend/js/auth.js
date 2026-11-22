/**
 * Authentication Module (Mock UI Level - Tuần 1)
 * Giả lập đăng nhập, lưu phiên làm việc vào localStorage
 */

const AUTH_STORAGE_KEY = 'notification_system_user';

const Auth = {
  // Lấy thông tin user hiện tại
  getCurrentUser() {
    const data = localStorage.getItem(AUTH_STORAGE_KEY);
    if (!data) return null;
    try {
      return JSON.parse(data);
    } catch (e) {
      return null;
    }
  },

  // Đăng nhập giả lập
  login(email, password, role = 'admin') {
    if (!email || !password) {
      return { success: false, message: 'Vui lòng nhập đầy đủ Email và Mật khẩu!' };
    }

    const mockUser = {
      id: 1,
      email: email.trim(),
      name: email.split('@')[0] || 'Quản trị viên',
      role: role,
      token: 'mock-jwt-token-week1-' + Date.now(),
      loginTime: new Date().toISOString()
    };

    localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(mockUser));
    return { success: true, user: mockUser };
  },

  // Đăng xuất
  logout() {
    localStorage.removeItem(AUTH_STORAGE_KEY);
    window.location.href = 'login.html';
  },

  // Kiểm tra quyền truy cập vào trang dashboard
  requireAuth() {
    const user = this.getCurrentUser();
    if (!user) {
      window.location.href = 'login.html';
      return false;
    }
    return true;
  },

  // Nếu đã đăng nhập mà truy cập login thì chuyển về dashboard
  redirectIfAuthenticated() {
    const user = this.getCurrentUser();
    if (user) {
      window.location.href = 'index.html';
    }
  }
};
