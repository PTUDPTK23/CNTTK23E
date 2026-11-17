import React, { useState } from 'react';
import {
  Bus, Mail, Lock, Eye, EyeOff, ShieldCheck, Zap, Headphones,
  ArrowRight, Search, Calendar, MapPin, Users, Filter, Star,
  Wifi, Coffee, BatteryCharging, Wind, CheckCircle2, QrCode,
  CreditCard, Smartphone, Banknote, Ticket, Clock, ChevronRight,
  LogOut, User, RefreshCw, X, AlertCircle, Share2, Printer
} from 'lucide-react';
import './App.css';

// Danh sách các tuyến xe và chuyến xe thực tế (Mẫu Đề tài 17)
const SAMPLE_TRIPS = [
  {
    id: 'TRIP-01',
    operator: 'XeNhanh Express VIP',
    operatorLogo: 'XN',
    vehicleType: 'Limousine 11 chỗ VIP',
    rating: 4.9,
    reviewsCount: 1420,
    departureTime: '06:30',
    arrivalTime: '08:45',
    duration: '2h 15m',
    from: 'Hà Nội',
    to: 'Thái Nguyên',
    fromStation: 'Bến xe Mỹ Đình (Cổng số 2)',
    toStation: 'Bến xe Trung tâm Thái Nguyên',
    price: 140000,
    availableSeats: 6,
    totalSeats: 11,
    amenities: ['Wifi 5G', 'Nước suối', 'Cổng sạc Type-C', 'Điều hòa'],
    decks: 1,
    pickupPoints: ['Bến xe Mỹ Đình', 'Đại học Quốc Gia Hà Nội', 'Sân bay Nội Bài (Ngã tư Kim Anh)'],
    dropoffPoints: ['Đại học CNTT & Truyền thông (ICTU)', 'Ngã tư Gang Thép', 'Bến xe Trung tâm Thái Nguyên']
  },
  {
    id: 'TRIP-02',
    operator: 'Hà Lan Buslines',
    operatorLogo: 'HL',
    vehicleType: 'Limousine cao cấp 9 chỗ',
    rating: 4.8,
    reviewsCount: 890,
    departureTime: '08:00',
    arrivalTime: '10:00',
    duration: '2h 00m',
    from: 'Hà Nội',
    to: 'Thái Nguyên',
    fromStation: '87 Đại Cồ Việt, Hai Bà Trưng',
    toStation: 'VP Hoàng Văn Thụ, Thái Nguyên',
    price: 150000,
    availableSeats: 4,
    totalSeats: 9,
    amenities: ['Wifi tốc độ cao', 'Ghế massage', 'Nước suối', 'Khăn lạnh'],
    decks: 1,
    pickupPoints: ['87 Đại Cồ Việt', 'Rạp xiếc Trung Ương', 'Công viên Cầu Giấy'],
    dropoffPoints: ['VP Hà Lan Thái Nguyên', 'Bến xe Thái Nguyên', 'ĐH Y Dược Thái Nguyên']
  },
  {
    id: 'TRIP-03',
    operator: 'XeNhanh Sleeper',
    operatorLogo: 'XN',
    vehicleType: 'Giường nằm Royal 34 phòng',
    rating: 4.9,
    reviewsCount: 2150,
    departureTime: '21:30',
    arrivalTime: '03:45',
    duration: '6h 15m',
    from: 'Hà Nội',
    to: 'Sa Pa',
    fromStation: 'Bến xe Giáp Bát',
    toStation: 'Bến xe Sa Pa (Điện Biên Phủ)',
    price: 280000,
    availableSeats: 12,
    totalSeats: 34,
    amenities: ['Màn hình TV', 'Rèm riêng tư', 'Chăn gối cao cấp', 'Wifi', 'Sạc pin'],
    decks: 2,
    pickupPoints: ['Bến xe Giáp Bát', 'Ngã tư Nội Bài', 'Bến xe Mỹ Đình'],
    dropoffPoints: ['Bến xe Sa Pa', 'Nhà thờ Đá Sa Pa', 'Chợ Sa Pa']
  },
  {
    id: 'TRIP-04',
    operator: 'Sao Việt Express',
    operatorLogo: 'SV',
    vehicleType: 'Giường nằm Cabin đơn VIP',
    rating: 4.7,
    reviewsCount: 1680,
    departureTime: '22:30',
    arrivalTime: '04:30',
    duration: '6h 00m',
    from: 'Hà Nội',
    to: 'Sa Pa',
    fromStation: '789 Giải Phóng, Hoàng Mai',
    toStation: '571 Điện Biên Phủ, Sa Pa',
    price: 320000,
    availableSeats: 8,
    totalSeats: 32,
    amenities: ['Cabin riêng', 'Massage rung', 'Ổ cắm điện', 'Bánh ngọt', 'Nước'],
    decks: 2,
    pickupPoints: ['VP Giải Phóng', 'Bến xe Nam Hồng', 'Sân bay Nội Bài'],
    dropoffPoints: ['VP Sa Pa Điện Biên Phủ', 'Bến xe Sa Pa']
  },
  {
    id: 'TRIP-05',
    operator: 'Phương Trang (FUTA Bus)',
    operatorLogo: 'PT',
    vehicleType: 'Giường nằm 40 chỗ',
    rating: 4.8,
    reviewsCount: 3820,
    departureTime: '23:00',
    arrivalTime: '07:00',
    duration: '8h 00m',
    from: 'TP. Hồ Chí Minh',
    to: 'Đà Lạt',
    fromStation: 'Bến xe Miền Đông Mới',
    toStation: 'Bến xe Liên Tỉnh Đà Lạt',
    price: 300000,
    availableSeats: 15,
    totalSeats: 40,
    amenities: ['Wifi', 'Nước suối', 'Điều hòa', 'Dép trung chuyển'],
    decks: 2,
    pickupPoints: ['Bến xe Miền Đông', 'Hàng Xanh', 'Ngã 4 Thủ Đức'],
    dropoffPoints: ['Bến xe Đà Lạt', 'Trung chuyển nội thành Đà Lạt']
  }
];

export default function App() {
  // Navigation / Views: 'login' (chuẩn theo ảnh thiết kế đề bài) hoặc 'system' (hệ thống đặt vé đầy đủ)
  const [currentView, setCurrentView] = useState('login');
  const [authMode, setAuthMode] = useState('login'); // 'login' | 'register' | 'forgot'
  const [showPassword, setShowPassword] = useState(false);
  const [toastMessage, setToastMessage] = useState(null);

  // Auth Inputs
  const [authEmail, setAuthEmail] = useState('');
  const [authPassword, setAuthPassword] = useState('');
  const [authFullName, setAuthFullName] = useState('');
  const [authPhone, setAuthPhone] = useState('');

  // Logged-in user state
  const [currentUser, setCurrentUser] = useState({
    name: 'Thào A Póng',
    code: 'dtc245200592',
    class: 'CNTTK23E',
    email: 'dtc245200592@ictu.edu.vn',
    isLoggedIn: false
  });

  // Search parameters
  const [tripType, setTripType] = useState('oneway'); // 'oneway' | 'roundtrip'
  const [origin, setOrigin] = useState('Hà Nội');
  const [destination, setDestination] = useState('Thái Nguyên');
  const [departDate, setDepartDate] = useState('2026-11-18');
  const [returnDate, setReturnDate] = useState('2026-11-20');
  const [passengers, setPassengers] = useState(1);
  const [vehicleFilter, setVehicleFilter] = useState('all');
  const [sortBy, setSortBy] = useState('earliest');

  // Active trip & Booking flow states
  const [selectedTrip, setSelectedTrip] = useState(null);
  const [selectedSeats, setSelectedSeats] = useState([]);
  const [selectedPickup, setSelectedPickup] = useState('');
  const [selectedDropoff, setSelectedDropoff] = useState('');

  // Checkout & Ticket state
  const [isCheckoutOpen, setIsCheckoutOpen] = useState(false);
  const [passengerName, setPassengerName] = useState(currentUser.name);
  const [passengerPhone, setPassengerPhone] = useState('0987654321');
  const [passengerEmail, setPassengerEmail] = useState(currentUser.email);
  const [passengerNotes, setPassengerNotes] = useState('');
  const [couponCode, setCouponCode] = useState('');
  const [appliedDiscount, setAppliedDiscount] = useState(0);
  const [paymentMethod, setPaymentMethod] = useState('vnpay');
  const [activeTicket, setActiveTicket] = useState(null);
  const [myTickets, setMyTickets] = useState([]);
  const [systemTab, setSystemTab] = useState('booking'); // 'booking' | 'mytickets' | 'lookup'
  const [lookupQuery, setLookupQuery] = useState('');
  const [lookupResult, setLookupResult] = useState(null);

  // Helper: Hiển thị toast thông báo
  const showToast = (msg, type = 'success') => {
    setToastMessage({ text: msg, type });
    setTimeout(() => setToastMessage(null), 3500);
  };

  // Xử lý đăng nhập
  const handleLoginSubmit = (e) => {
    e?.preventDefault();
    if (!authEmail.trim() || !authPassword.trim()) {
      showToast('Vui lòng nhập Email/Số điện thoại và Mật khẩu!', 'error');
      return;
    }
    const loggedInUser = {
      name: authFullName || 'Thào A Póng',
      code: 'dtc245200592',
      class: 'CNTTK23E',
      email: authEmail,
      isLoggedIn: true
    };
    setCurrentUser(loggedInUser);
    setPassengerName(loggedInUser.name);
    setPassengerEmail(loggedInUser.email);
    showToast(`Đăng nhập thành công! Chào mừng ${loggedInUser.name}`);
    setCurrentView('system');
  };

  // Đăng nhập nhanh tài khoản mẫu
  const handleQuickDemoLogin = () => {
    const demoUser = {
      name: 'Thào A Póng',
      code: 'dtc245200592',
      class: 'CNTTK23E - ICTU',
      email: 'dtc245200592@ictu.edu.vn',
      isLoggedIn: true
    };
    setCurrentUser(demoUser);
    setPassengerName(demoUser.name);
    setPassengerEmail(demoUser.email);
    showToast(`Đăng nhập thành công với tài khoản mẫu: ${demoUser.name}`);
    setCurrentView('system');
  };

  // Đổi chiều nơi đi - nơi đến
  const handleSwapLocations = () => {
    const temp = origin;
    setOrigin(destination);
    setDestination(temp);
    showToast(`Đã đảo chiều tuyến: ${destination} ⇄ ${temp}`);
  };

  // Toggle chọn ghế
  const handleToggleSeat = (seatCode, price) => {
    if (selectedSeats.includes(seatCode)) {
      setSelectedSeats(selectedSeats.filter(s => s !== seatCode));
    } else {
      if (selectedSeats.length >= 6) {
        showToast('Mỗi hành khách chỉ được chọn tối đa 6 ghế!', 'error');
        return;
      }
      setSelectedSeats([...selectedSeats, seatCode]);
    }
  };

  // Áp dụng mã giảm giá
  const handleApplyCoupon = () => {
    const code = couponCode.trim().toUpperCase();
    if (code === 'XENHANH20') {
      setAppliedDiscount(0.20);
      showToast('Áp dụng mã XENHANH20 thành công: Giảm 20% giá vé!');
    } else if (code === 'SVCNTTK23' || code === 'ICTU') {
      setAppliedDiscount(30000);
      showToast('Áp dụng mã sinh viên CNTTK23E: Giảm trực tiếp 30.000đ!');
    } else {
      showToast('Mã giảm giá không hợp lệ hoặc đã hết hạn!', 'error');
    }
  };

  // Tính tổng tiền
  const calculateTotal = () => {
    if (!selectedTrip || selectedSeats.length === 0) return 0;
    const baseTotal = selectedSeats.length * selectedTrip.price;
    if (appliedDiscount < 1) {
      return baseTotal * (1 - appliedDiscount);
    }
    return Math.max(0, baseTotal - appliedDiscount);
  };

  // Xác nhận thanh toán & Tạo vé điện tử
  const handleConfirmBooking = () => {
    if (!passengerName.trim() || !passengerPhone.trim()) {
      showToast('Vui lòng nhập đầy đủ Họ tên và Số điện thoại!', 'error');
      return;
    }
    const ticketId = `XN-${Math.floor(100000 + Math.random() * 900000)}`;
    const newTicket = {
      id: ticketId,
      trip: selectedTrip,
      seats: [...selectedSeats],
      totalAmount: calculateTotal(),
      passenger: {
        name: passengerName,
        phone: passengerPhone,
        email: passengerEmail,
        notes: passengerNotes
      },
      pickup: selectedPickup || selectedTrip.pickupPoints[0],
      dropoff: selectedDropoff || selectedTrip.dropoffPoints[0],
      paymentMethod,
      bookingTime: new Date().toLocaleString('vi-VN'),
      departDate,
      status: 'Đã thanh toán (Thành công)'
    };

    setMyTickets([newTicket, ...myTickets]);
    setActiveTicket(newTicket);
    setIsCheckoutOpen(false);
    setSelectedSeats([]);
    showToast(`Đặt vé thành công! Mã vé: ${ticketId}`);
  };

  // Tra cứu vé
  const handleLookup = () => {
    if (!lookupQuery.trim()) {
      showToast('Vui lòng nhập mã vé hoặc số điện thoại!', 'error');
      return;
    }
    const found = myTickets.find(t =>
      t.id.toLowerCase() === lookupQuery.trim().toLowerCase() ||
      t.passenger.phone === lookupQuery.trim()
    );
    if (found) {
      setLookupResult(found);
      showToast(`Đã tìm thấy thông tin vé ${found.id}!`);
    } else {
      // Mock một vé nếu chưa có trong danh sách để người dùng test thoải mái
      const mockResult = {
        id: lookupQuery.toUpperCase(),
        trip: SAMPLE_TRIPS[0],
        seats: ['A01', 'A02'],
        totalAmount: 280000,
        passenger: {
          name: 'Thào A Póng',
          phone: lookupQuery.startsWith('0') ? lookupQuery : '0987654321',
          email: 'dtc245200592@ictu.edu.vn'
        },
        pickup: 'Bến xe Mỹ Đình (Hà Nội)',
        dropoff: 'Bến xe Trung tâm Thái Nguyên',
        paymentMethod: 'vnpay',
        bookingTime: '17/11/2026, 21:00:00',
        departDate: '2026-11-18',
        status: 'Đã xác nhận & Thanh toán'
      };
      setLookupResult(mockResult);
      showToast(`Đã tìm thấy thông tin vé ${mockResult.id}!`);
    }
  };

  // Lọc danh sách chuyến
  const filteredTrips = SAMPLE_TRIPS.filter(trip => {
    if (origin && !trip.from.toLowerCase().includes(origin.toLowerCase())) return false;
    if (destination && !trip.to.toLowerCase().includes(destination.toLowerCase())) return false;
    if (vehicleFilter === 'limousine' && !trip.vehicleType.toLowerCase().includes('limousine')) return false;
    if (vehicleFilter === 'sleeper' && !trip.vehicleType.toLowerCase().includes('giường nằm')) return false;
    return true;
  });

  return (
    <div className="main-app-container">
      {/* Thanh điều khiển nhanh góc trên: Cho phép chuyển đổi linh hoạt bất cứ lúc nào */}
      <div className="top-dev-bar">
        <span className="badge">ĐỀ TÀI 17</span>
        <span>Hệ thống đặt chuyến xe</span>
        <button
          onClick={() => setCurrentView(currentView === 'login' ? 'system' : 'login')}
          title="Chuyển đổi giữa Giao diện đăng nhập chuẩn ảnh và Hệ thống đặt vé"
        >
          <RefreshCw size={14} />
          {currentView === 'login' ? 'Vào Hệ thống đặt vé' : 'Xem Màn hình Đăng nhập (Mẫu)'}
        </button>
      </div>

      {/* Toast thông báo */}
      {toastMessage && (
        <div className={`toast-notice ${toastMessage.type}`}>
          {toastMessage.type === 'success' ? <CheckCircle2 size={18} color="#10b981" /> : <AlertCircle size={18} color="#ef4444" />}
          <span>{toastMessage.text}</span>
        </div>
      )}

      {/* ===================================================================
          VIEW 1: MÀN HÌNH ĐĂNG NHẬP (CHÍNH XÁC 100% THEO HÌNH ẢNH ĐỀ TÀI)
         =================================================================== */}
      {currentView === 'login' && (
        <div className="login-page-container">
          <div className="wave-bg-overlay"></div>
          <div className="wave-curve-1"></div>
          <div className="wave-curve-2"></div>

          <div className="login-layout-card">
            {/* Cột trái: Nhận diện thương hiệu XeNhanh, Slogan, 3 Huy hiệu & Xe Bus trên cao tốc */}
            <div className="login-left-col">
              {/* Logo XeNhanh */}
              <div className="brand-logo-wrap">
                <div className="brand-bus-icon">
                  <Bus size={32} strokeWidth={2.2} />
                </div>
                <div className="brand-text-block">
                  <h1 className="brand-title">XeNhanh</h1>
                  <span className="brand-slogan">Đặt xe dễ dàng – Đi muôn nơi</span>
                </div>
              </div>

              {/* Tiêu đề quảng bá */}
              <div className="hero-headline-wrap">
                <h2 className="hero-main-title">Đặt vé xe trực tuyến</h2>
                <h3 className="hero-sub-title">Nhanh chóng – Tiện lợi – An toàn</h3>
              </div>

              {/* 3 Huy hiệu tính năng */}
              <div className="feature-badges-row">
                <div className="feature-pill">
                  <div className="feature-icon-circle">
                    <Zap size={22} fill="currentColor" />
                  </div>
                  <span className="feature-text">Đặt vé nhanh<br />chỉ vài phút</span>
                </div>

                <div className="feature-pill">
                  <div className="feature-icon-circle">
                    <ShieldCheck size={22} />
                  </div>
                  <span className="feature-text">Thanh toán an toàn<br />đa dạng</span>
                </div>

                <div className="feature-pill">
                  <div className="feature-icon-circle">
                    <Headphones size={22} />
                  </div>
                  <span className="feature-text">Hỗ trợ 24/7</span>
                </div>
              </div>

              {/* Hình ảnh Xe khách hiện đại trên đường cao tốc với thành phố phía sau */}
              <div className="bus-visual-container">
                <img
                  src="/bus-crop.png"
                  alt="XeNhanh Coach Bus on Highway"
                  className="bus-visual-img"
                  onError={(e) => {
                    // Dự phòng nếu ảnh chưa kịp tải thì dùng banner gốc
                    e.target.src = '/login-banner.png';
                  }}
                />
              </div>
            </div>

            {/* Cột phải: Card Đăng nhập / Đăng ký màu trắng nổi bật */}
            <div className="auth-card-container">
              {authMode === 'login' && (
                <>
                  <div className="auth-card-header">
                    <h2 className="auth-card-title">Đăng nhập tài khoản</h2>
                    <p className="auth-card-subtitle">Để tiếp tục đặt vé xe</p>
                  </div>

                  <form className="auth-form" onSubmit={handleLoginSubmit}>
                    {/* Ô Email hoặc SĐT */}
                    <div className="input-field-group">
                      <div className="input-icon-left">
                        <Mail size={18} />
                      </div>
                      <input
                        type="text"
                        className="auth-input"
                        placeholder="Email hoặc số điện thoại"
                        value={authEmail}
                        onChange={(e) => setAuthEmail(e.target.value)}
                        required
                      />
                    </div>

                    {/* Ô Mật khẩu */}
                    <div className="input-field-group">
                      <div className="input-icon-left">
                        <Lock size={18} />
                      </div>
                      <input
                        type={showPassword ? 'text' : 'password'}
                        className="auth-input"
                        placeholder="Mật khẩu"
                        value={authPassword}
                        onChange={(e) => setAuthPassword(e.target.value)}
                        required
                      />
                      <button
                        type="button"
                        className="input-action-btn"
                        onClick={() => setShowPassword(!showPassword)}
                        title={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
                      >
                        {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                      </button>
                    </div>

                    <div className="auth-options-row">
                      <label className="remember-label">
                        <input type="checkbox" defaultChecked /> Ghi nhớ đăng nhập
                      </label>
                      <button
                        type="button"
                        className="forgot-password-link"
                        onClick={() => setAuthMode('forgot')}
                      >
                        Quên mật khẩu?
                      </button>
                    </div>

                    {/* Nút Đăng nhập */}
                    <button type="submit" className="btn-auth-submit">
                      Đăng nhập
                    </button>
                  </form>

                  {/* Hoặc đăng nhập với */}
                  <div className="social-divider">
                    <span>Hoặc đăng nhập với</span>
                  </div>

                  {/* Hai nút Google và Facebook */}
                  <div className="social-buttons-grid">
                    <button
                      type="button"
                      className="btn-social"
                      onClick={() => {
                        showToast('Đang kết nối Google...');
                        setTimeout(handleQuickDemoLogin, 800);
                      }}
                    >
                      {/* Google G Logo SVG */}
                      <svg width="18" height="18" viewBox="0 0 24 24">
                        <path fill="#4285F4" d="M23.745 12.27c0-.7-.06-1.4-.19-2.07H12v4.51h6.6c-.29 1.52-1.14 2.82-2.4 3.68v3.05h3.88c2.27-2.09 3.66-5.17 3.66-9.17z" />
                        <path fill="#34A853" d="M12 24c3.24 0 5.95-1.08 7.93-2.91l-3.88-3.05c-1.08.72-2.45 1.16-4.05 1.16-3.12 0-5.77-2.1-6.72-4.93H1.25v3.15C3.26 21.36 7.33 24 12 24z" />
                        <path fill="#FBBC05" d="M5.28 14.27c-.25-.72-.38-1.49-.38-2.27s.13-1.55.38-2.27V6.58H1.25C.45 8.16 0 9.97 0 12s.45 3.84 1.25 5.42l4.03-3.15z" />
                        <path fill="#EA4335" d="M12 4.75c1.77 0 3.35.61 4.6 1.8l3.42-3.42C17.95 1.19 15.24 0 12 0 7.33 0 3.26 2.64 1.25 6.58l4.03 3.15c.95-2.83 3.6-4.98 6.72-4.98z" />
                      </svg>
                      Google
                    </button>

                    <button
                      type="button"
                      className="btn-social"
                      onClick={() => {
                        showToast('Đang kết nối Facebook...');
                        setTimeout(handleQuickDemoLogin, 800);
                      }}
                    >
                      {/* Facebook f Logo SVG */}
                      <svg width="20" height="20" viewBox="0 0 24 24" fill="#1877F2">
                        <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z" />
                      </svg>
                      Facebook
                    </button>
                  </div>

                  {/* Chuyển sang Đăng ký */}
                  <div className="auth-card-footer">
                    Chưa có tài khoản?{' '}
                    <button
                      type="button"
                      onClick={() => setAuthMode('register')}
                    >
                      Đăng ký ngay &gt;
                    </button>
                  </div>

                  {/* Nút Đăng nhập nhanh tài khoản mẫu dành cho chấm bài / demo */}
                  <div className="demo-login-box">
                    <p>💡 Đề tài 17: Kiểm thử hệ thống nhanh</p>
                    <button
                      type="button"
                      className="demo-login-btn"
                      onClick={handleQuickDemoLogin}
                    >
                      <Zap size={14} /> Vào thẳng Hệ thống đặt chuyến xe
                    </button>
                  </div>
                </>
              )}

              {/* Chế độ Đăng ký */}
              {authMode === 'register' && (
                <>
                  <div className="auth-card-header">
                    <h2 className="auth-card-title">Đăng ký tài khoản</h2>
                    <p className="auth-card-subtitle">Tạo tài khoản XeNhanh chỉ với vài bước</p>
                  </div>

                  <form className="auth-form" onSubmit={(e) => {
                    e.preventDefault();
                    showToast('Đăng ký tài khoản thành công! Vui lòng đăng nhập.');
                    setAuthMode('login');
                  }}>
                    <div className="input-field-group">
                      <div className="input-icon-left"><User size={18} /></div>
                      <input
                        type="text"
                        className="auth-input"
                        placeholder="Họ và tên của bạn"
                        value={authFullName}
                        onChange={(e) => setAuthFullName(e.target.value)}
                        required
                      />
                    </div>

                    <div className="input-field-group">
                      <div className="input-icon-left"><Smartphone size={18} /></div>
                      <input
                        type="tel"
                        className="auth-input"
                        placeholder="Số điện thoại"
                        value={authPhone}
                        onChange={(e) => setAuthPhone(e.target.value)}
                        required
                      />
                    </div>

                    <div className="input-field-group">
                      <div className="input-icon-left"><Mail size={18} /></div>
                      <input
                        type="email"
                        className="auth-input"
                        placeholder="Địa chỉ Email"
                        value={authEmail}
                        onChange={(e) => setAuthEmail(e.target.value)}
                        required
                      />
                    </div>

                    <div className="input-field-group">
                      <div className="input-icon-left"><Lock size={18} /></div>
                      <input
                        type="password"
                        className="auth-input"
                        placeholder="Tạo mật khẩu"
                        value={authPassword}
                        onChange={(e) => setAuthPassword(e.target.value)}
                        required
                      />
                    </div>

                    <button type="submit" className="btn-auth-submit">
                      Tạo tài khoản ngay
                    </button>
                  </form>

                  <div className="auth-card-footer">
                    Đã có tài khoản?{' '}
                    <button type="button" onClick={() => setAuthMode('login')}>
                      Đăng nhập ngay &gt;
                    </button>
                  </div>
                </>
              )}

              {/* Chế độ Quên mật khẩu */}
              {authMode === 'forgot' && (
                <>
                  <div className="auth-card-header">
                    <h2 className="auth-card-title">Khôi phục mật khẩu</h2>
                    <p className="auth-card-subtitle">Nhập email hoặc SĐT để nhận mã xác thực</p>
                  </div>

                  <form className="auth-form" onSubmit={(e) => {
                    e.preventDefault();
                    showToast('Mã OTP xác thực đã được gửi về số điện thoại / email của bạn!');
                    setAuthMode('login');
                  }}>
                    <div className="input-field-group">
                      <div className="input-icon-left"><Mail size={18} /></div>
                      <input
                        type="text"
                        className="auth-input"
                        placeholder="Email hoặc số điện thoại đã đăng ký"
                        required
                      />
                    </div>

                    <button type="submit" className="btn-auth-submit">
                      Gửi mã xác thực OTP
                    </button>
                  </form>

                  <div className="auth-card-footer">
                    <button type="button" onClick={() => setAuthMode('login')}>
                      &lt; Quay lại Đăng nhập
                    </button>
                  </div>
                </>
              )}
            </div>
          </div>
        </div>
      )}

      {/* ===================================================================
          VIEW 2: HỆ THỐNG ĐẶT CHUYẾN XE TOÀN DIỆN (FULL XENHANH PLATFORM)
         =================================================================== */}
      {currentView === 'system' && (
        <div className="booking-system-wrapper">
          {/* Header Navigation */}
          <header className="system-navbar">
            <div className="navbar-container">
              <div className="nav-brand" onClick={() => setSystemTab('booking')}>
                <div className="nav-brand-logo">
                  <Bus size={24} />
                </div>
                <div>
                  <div className="nav-brand-name">XeNhanh</div>
                  <div className="nav-brand-tagline">Hệ thống đặt vé xe trực tuyến</div>
                </div>
              </div>

              <nav className="nav-links">
                <button
                  className={`nav-link-item ${systemTab === 'booking' ? 'active' : ''}`}
                  onClick={() => setSystemTab('booking')}
                >
                  Tìm chuyến xe
                </button>
                <button
                  className={`nav-link-item ${systemTab === 'mytickets' ? 'active' : ''}`}
                  onClick={() => setSystemTab('mytickets')}
                >
                  Vé của tôi ({myTickets.length})
                </button>
                <button
                  className={`nav-link-item ${systemTab === 'lookup' ? 'active' : ''}`}
                  onClick={() => setSystemTab('lookup')}
                >
                  Tra cứu vé
                </button>
              </nav>

              <div className="nav-actions">
                <div className="user-profile-badge">
                  <div className="user-avatar">
                    {currentUser.name ? currentUser.name[0] : 'U'}
                  </div>
                  <div className="user-info">
                    <span className="user-name">{currentUser.name}</span>
                    <span className="user-sub">{currentUser.code} - {currentUser.class}</span>
                  </div>
                </div>

                <button
                  className="btn-nav-logout"
                  onClick={() => {
                    setCurrentView('login');
                    showToast('Đã quay lại màn hình Đăng nhập (Mẫu thiết kế)');
                  }}
                  title="Quay lại Màn hình Đăng nhập"
                >
                  <LogOut size={16} /> Mẫu Đăng nhập
                </button>
              </div>
            </div>
          </header>

          {/* TAB 1: TÌM & ĐẶT CHUYẾN XE */}
          {systemTab === 'booking' && (
            <>
              {/* Hero Search Section */}
              <section className="booking-hero">
                <div className="hero-content">
                  <h1>Đặt vé xe khách trực tuyến dễ dàng</h1>
                  <p>Cam kết giữ 100% chỗ, hoàn tiền nếu không có chỗ, hỗ trợ 24/7</p>
                </div>

                {/* Form tìm kiếm chuyến */}
                <div className="search-box-card">
                  <div className="search-tabs">
                    <button
                      className={`tab-btn ${tripType === 'oneway' ? 'active' : ''}`}
                      onClick={() => setTripType('oneway')}
                    >
                      <ArrowRight size={16} /> Vé Một chiều
                    </button>
                    <button
                      className={`tab-btn ${tripType === 'roundtrip' ? 'active' : ''}`}
                      onClick={() => setTripType('roundtrip')}
                    >
                      <RefreshCw size={16} /> Vé Khứ hồi
                    </button>
                  </div>

                  <div className="search-fields-grid">
                    {/* Điểm đi */}
                    <div className="search-field-item">
                      <span className="field-label"><MapPin size={14} /> Điểm xuất phát</span>
                      <select
                        className="field-select"
                        value={origin}
                        onChange={(e) => setOrigin(e.target.value)}
                      >
                        <option value="Hà Nội">Hà Nội</option>
                        <option value="Thái Nguyên">Thái Nguyên</option>
                        <option value="TP. Hồ Chí Minh">TP. Hồ Chí Minh</option>
                        <option value="Sa Pa">Sa Pa (Lào Cai)</option>
                        <option value="Đà Nẵng">Đà Nẵng</option>
                        <option value="Hải Phòng">Hải Phòng</option>
                        <option value="Đà Lạt">Đà Lạt</option>
                      </select>
                    </div>

                    {/* Nút hoán đổi */}
                    <button
                      type="button"
                      className="btn-swap-locations"
                      onClick={handleSwapLocations}
                      title="Hoán đổi nơi đi và nơi đến"
                    >
                      ⇄
                    </button>

                    {/* Điểm đến */}
                    <div className="search-field-item">
                      <span className="field-label"><MapPin size={14} /> Nơi đến</span>
                      <select
                        className="field-select"
                        value={destination}
                        onChange={(e) => setDestination(e.target.value)}
                      >
                        <option value="Thái Nguyên">Thái Nguyên</option>
                        <option value="Hà Nội">Hà Nội</option>
                        <option value="Sa Pa">Sa Pa (Lào Cai)</option>
                        <option value="Đà Lạt">Đà Lạt</option>
                        <option value="TP. Hồ Chí Minh">TP. Hồ Chí Minh</option>
                        <option value="Hải Phòng">Hải Phòng</option>
                        <option value="Đà Nẵng">Đà Nẵng</option>
                      </select>
                    </div>

                    {/* Ngày đi */}
                    <div className="search-field-item">
                      <span className="field-label"><Calendar size={14} /> Ngày khởi hành</span>
                      <input
                        type="date"
                        className="field-input"
                        value={departDate}
                        onChange={(e) => setDepartDate(e.target.value)}
                      />
                    </div>

                    {/* Số hành khách */}
                    <div className="search-field-item">
                      <span className="field-label"><Users size={14} /> Số khách</span>
                      <select
                        className="field-select"
                        value={passengers}
                        onChange={(e) => setPassengers(Number(e.target.value))}
                      >
                        <option value={1}>1 vé (1 khách)</option>
                        <option value={2}>2 vé (2 khách)</option>
                        <option value={3}>3 vé (3 khách)</option>
                        <option value={4}>4 vé (4 khách)</option>
                      </select>
                    </div>

                    {/* Nút tìm kiếm */}
                    <button
                      type="button"
                      className="btn-search-trips"
                      onClick={() => showToast(`Đang tìm kiếm chuyến xe từ ${origin} đi ${destination}...`)}
                    >
                      <Search size={18} /> Tìm chuyến xe
                    </button>
                  </div>

                  {/* Gợi ý tuyến phổ biến */}
                  <div className="quick-routes-bar">
                    <span className="quick-routes-label">Tuyến thịnh hành:</span>
                    <button className="route-chip" onClick={() => { setOrigin('Hà Nội'); setDestination('Thái Nguyên'); }}>
                      Hà Nội → Thái Nguyên (từ 140k)
                    </button>
                    <button className="route-chip" onClick={() => { setOrigin('Hà Nội'); setDestination('Sa Pa'); }}>
                      Hà Nội → Sa Pa (từ 280k)
                    </button>
                    <button className="route-chip" onClick={() => { setOrigin('TP. Hồ Chí Minh'); setDestination('Đà Lạt'); }}>
                      Sài Gòn → Đà Lạt (từ 300k)
                    </button>
                  </div>
                </div>
              </section>

              {/* Main Content Area */}
              <div className="trips-main-section">
                {/* Bộ lọc bên trái */}
                <aside className="filter-sidebar">
                  <div className="filter-header">
                    <h3><Filter size={18} /> Bộ lọc tìm kiếm</h3>
                    <button
                      className="btn-reset-filter"
                      onClick={() => { setVehicleFilter('all'); }}
                    >
                      Xóa lọc
                    </button>
                  </div>

                  <div className="filter-group">
                    <h4 className="filter-group-title">Loại xe</h4>
                    <label className="filter-checkbox-item">
                      <input
                        type="radio"
                        name="vehicleFilter"
                        checked={vehicleFilter === 'all'}
                        onChange={() => setVehicleFilter('all')}
                      />
                      Tất cả loại xe ({SAMPLE_TRIPS.length})
                    </label>
                    <label className="filter-checkbox-item">
                      <input
                        type="radio"
                        name="vehicleFilter"
                        checked={vehicleFilter === 'limousine'}
                        onChange={() => setVehicleFilter('limousine')}
                      />
                      Limousine VIP 9-11 chỗ
                    </label>
                    <label className="filter-checkbox-item">
                      <input
                        type="radio"
                        name="vehicleFilter"
                        checked={vehicleFilter === 'sleeper'}
                        onChange={() => setVehicleFilter('sleeper')}
                      />
                      Giường nằm Cabin VIP
                    </label>
                  </div>

                  <div className="filter-group">
                    <h4 className="filter-group-title">Giờ khởi hành</h4>
                    <label className="filter-checkbox-item">
                      <input type="checkbox" defaultChecked /> Buổi sáng (06:00 - 12:00)
                    </label>
                    <label className="filter-checkbox-item">
                      <input type="checkbox" defaultChecked /> Buổi chiều (12:00 - 18:00)
                    </label>
                    <label className="filter-checkbox-item">
                      <input type="checkbox" defaultChecked /> Buổi tối & Đêm (18:00 - 24:00)
                    </label>
                  </div>

                  <div className="filter-group">
                    <h4 className="filter-group-title">Tiện ích đi kèm</h4>
                    <label className="filter-checkbox-item">
                      <input type="checkbox" defaultChecked /> Wifi miễn phí
                    </label>
                    <label className="filter-checkbox-item">
                      <input type="checkbox" defaultChecked /> Cổng sạc điện thoại
                    </label>
                    <label className="filter-checkbox-item">
                      <input type="checkbox" defaultChecked /> Ghế massage thư giãn
                    </label>
                  </div>
                </aside>

                {/* Danh sách chuyến xe bên phải */}
                <main className="trips-list-container">
                  <div className="results-top-bar">
                    <span className="results-count">
                      Tìm thấy <span>{filteredTrips.length}</span> chuyến xe từ <b>{origin}</b> đi <b>{destination}</b>
                    </span>

                    <div className="sort-group">
                      <span>Sắp xếp theo:</span>
                      <select
                        className="sort-select"
                        value={sortBy}
                        onChange={(e) => setSortBy(e.target.value)}
                      >
                        <option value="earliest">Giờ đi sớm nhất</option>
                        <option value="priceLow">Giá vé thấp nhất</option>
                        <option value="rating">Đánh giá cao nhất</option>
                      </select>
                    </div>
                  </div>

                  {filteredTrips.length === 0 ? (
                    <div style={{ background: 'white', padding: '40px', borderRadius: '16px', textAlign: 'center' }}>
                      <p style={{ color: '#64748b' }}>Không tìm thấy chuyến xe nào phù hợp. Vui lòng thử chọn tuyến khác!</p>
                      <button
                        style={{ marginTop: '14px', background: '#186fe8', color: 'white', padding: '8px 18px', borderRadius: '8px', fontWeight: 600 }}
                        onClick={() => { setOrigin('Hà Nội'); setDestination('Thái Nguyên'); setVehicleFilter('all'); }}
                      >
                        Xem tuyến Hà Nội ⇄ Thái Nguyên
                      </button>
                    </div>
                  ) : (
                    filteredTrips.map(trip => (
                      <div key={trip.id} className="trip-card">
                        <div className="trip-card-header">
                          <div className="operator-badge">
                            <div className="operator-logo-wrap">
                              <Bus size={22} />
                            </div>
                            <div>
                              <h4 className="operator-name">{trip.operator}</h4>
                              <span className="operator-vehicle-type">{trip.vehicleType}</span>
                            </div>
                          </div>

                          <div className="trip-rating">
                            <Star size={14} fill="#f59e0b" color="#f59e0b" />
                            <span>{trip.rating}</span>
                            <span style={{ color: '#6b7280', fontWeight: 'normal' }}>({trip.reviewsCount})</span>
                          </div>
                        </div>

                        {/* Lộ trình & Thời gian */}
                        <div className="trip-route-info">
                          <div className="time-station">
                            <span className="trip-time">{trip.departureTime}</span>
                            <span className="trip-station">{trip.fromStation}</span>
                            <span className="trip-city">{trip.from}</span>
                          </div>

                          <div className="trip-duration-visual">
                            <span className="duration-text">{trip.duration}</span>
                            <div className="duration-line"></div>
                          </div>

                          <div className="time-station" style={{ textAlign: 'right' }}>
                            <span className="trip-time">{trip.arrivalTime}</span>
                            <span className="trip-station">{trip.toStation}</span>
                            <span className="trip-city">{trip.to}</span>
                          </div>
                        </div>

                        {/* Tiện ích và Nút chọn chỗ */}
                        <div className="trip-card-footer">
                          <div className="trip-amenities">
                            {trip.amenities.map((item, idx) => (
                              <span key={idx} className="amenity-tag">
                                {idx === 0 && <Wifi size={13} />}
                                {idx === 1 && <Coffee size={13} />}
                                {idx === 2 && <BatteryCharging size={13} />}
                                {item}
                              </span>
                            ))}
                          </div>

                          <div className="trip-price-wrap">
                            <div className="price-text-block">
                              <span className="price-amount">
                                {trip.price.toLocaleString('vi-VN')} đ
                              </span>
                              <div className="seats-left">
                                Còn {trip.availableSeats} chỗ trống
                              </div>
                            </div>

                            <button
                              className="btn-select-trip"
                              onClick={() => {
                                if (selectedTrip?.id === trip.id) {
                                  setSelectedTrip(null);
                                  setSelectedSeats([]);
                                } else {
                                  setSelectedTrip(trip);
                                  setSelectedSeats([]);
                                  setSelectedPickup(trip.pickupPoints[0]);
                                  setSelectedDropoff(trip.dropoffPoints[0]);
                                }
                              }}
                            >
                              {selectedTrip?.id === trip.id ? 'Đóng chọn ghế ▲' : 'Chọn chuyến ▼'}
                            </button>
                          </div>
                        </div>

                        {/* SƠ ĐỒ CHỌN CHỖ NGỒI (KHI MỞ EXPAND) */}
                        {selectedTrip?.id === trip.id && (
                          <div className="seat-map-wrapper">
                            <div className="seat-map-header">
                              <h4>Sơ đồ chọn ghế ngồi ({trip.vehicleType})</h4>
                              <div className="seat-legends">
                                <div className="legend-item">
                                  <div className="legend-box available"></div>
                                  <span>Trống</span>
                                </div>
                                <div className="legend-item">
                                  <div className="legend-box selected"></div>
                                  <span>Đang chọn</span>
                                </div>
                                <div className="legend-item">
                                  <div className="legend-box booked"></div>
                                  <span>Đã bán</span>
                                </div>
                              </div>
                            </div>

                            {/* Khối sơ đồ ghế: Tầng 1 / Tầng 2 hoặc Limousine */}
                            {trip.decks === 2 ? (
                              <div className="decks-container">
                                {/* Tầng 1 */}
                                <div className="deck-card">
                                  <div className="deck-title">TẦNG 1 (DƯỚI)</div>
                                  <div className="seats-grid">
                                    {['A01', 'A02', 'A03', 'A04', 'A05', 'A06', 'A07', 'A08', 'A09'].map((s, idx) => {
                                      const isBooked = [2, 5].includes(idx);
                                      const isSelected = selectedSeats.includes(s);
                                      return (
                                        <button
                                          key={s}
                                          disabled={isBooked}
                                          className={`seat-item ${isSelected ? 'selected' : ''} ${isBooked ? 'booked' : ''}`}
                                          onClick={() => handleToggleSeat(s, trip.price)}
                                        >
                                          <span>{s}</span>
                                          <span style={{ fontSize: '10px' }}>{isBooked ? 'Đã bán' : `${trip.price / 1000}k`}</span>
                                        </button>
                                      );
                                    })}
                                  </div>
                                </div>

                                {/* Tầng 2 */}
                                <div className="deck-card">
                                  <div className="deck-title">TẦNG 2 (TRÊN)</div>
                                  <div className="seats-grid">
                                    {['B01', 'B02', 'B03', 'B04', 'B05', 'B06', 'B07', 'B08', 'B09'].map((s, idx) => {
                                      const isBooked = [1, 4, 7].includes(idx);
                                      const isSelected = selectedSeats.includes(s);
                                      return (
                                        <button
                                          key={s}
                                          disabled={isBooked}
                                          className={`seat-item ${isSelected ? 'selected' : ''} ${isBooked ? 'booked' : ''}`}
                                          onClick={() => handleToggleSeat(s, trip.price)}
                                        >
                                          <span>{s}</span>
                                          <span style={{ fontSize: '10px' }}>{isBooked ? 'Đã bán' : `${trip.price / 1000}k`}</span>
                                        </button>
                                      );
                                    })}
                                  </div>
                                </div>
                              </div>
                            ) : (
                              /* Xe Limousine 1 tầng */
                              <div className="deck-card" style={{ maxWidth: '520px', margin: '0 auto 20px' }}>
                                <div className="deck-title">KHOANG HÀNH KHÁCH VIP</div>
                                <div className="seats-grid">
                                  {['VIP-01', 'VIP-02', 'VIP-03', 'VIP-04', 'VIP-05', 'VIP-06', 'VIP-07', 'VIP-08', 'VIP-09'].map((s, idx) => {
                                    const isBooked = [0, 3].includes(idx);
                                    const isSelected = selectedSeats.includes(s);
                                    return (
                                      <button
                                        key={s}
                                        disabled={isBooked}
                                        className={`seat-item ${isSelected ? 'selected' : ''} ${isBooked ? 'booked' : ''}`}
                                        onClick={() => handleToggleSeat(s, trip.price)}
                                      >
                                        <span>{s}</span>
                                        <span style={{ fontSize: '10px' }}>{isBooked ? 'Đã bán' : `${trip.price / 1000}k`}</span>
                                      </button>
                                    );
                                  })}
                                </div>
                              </div>
                            )}

                            {/* Chọn Điểm đón & Điểm trả */}
                            <div className="points-selection-grid">
                              <div className="point-select-box">
                                <label><MapPin size={14} color="#186fe8" /> Chọn điểm đón khách:</label>
                                <select
                                  value={selectedPickup}
                                  onChange={(e) => setSelectedPickup(e.target.value)}
                                >
                                  {trip.pickupPoints.map((pt, i) => (
                                    <option key={i} value={pt}>{pt}</option>
                                  ))}
                                </select>
                              </div>

                              <div className="point-select-box">
                                <label><MapPin size={14} color="#10b981" /> Chọn điểm trả khách:</label>
                                <select
                                  value={selectedDropoff}
                                  onChange={(e) => setSelectedDropoff(e.target.value)}
                                >
                                  {trip.dropoffPoints.map((pt, i) => (
                                    <option key={i} value={pt}>{pt}</option>
                                  ))}
                                </select>
                              </div>
                            </div>

                            {/* Thanh tóm tắt ghế & Nút tiến hành thanh toán */}
                            <div className="seat-summary-bar">
                              <div className="selected-seats-info">
                                <div>
                                  Ghế đã chọn ({selectedSeats.length}):{' '}
                                  {selectedSeats.length === 0 ? (
                                    <span style={{ color: '#64748b', fontStyle: 'italic' }}>Chưa chọn ghế nào</span>
                                  ) : (
                                    <span className="seats-badges">
                                      {selectedSeats.map(s => <span key={s} className="seat-badge">{s}</span>)}
                                    </span>
                                  )}
                                </div>
                                <div className="total-fare-text">
                                  Tổng tiền: {(selectedSeats.length * trip.price).toLocaleString('vi-VN')} đ
                                </div>
                              </div>

                              <button
                                className="btn-proceed-checkout"
                                disabled={selectedSeats.length === 0}
                                style={{ opacity: selectedSeats.length === 0 ? 0.6 : 1, cursor: selectedSeats.length === 0 ? 'not-allowed' : 'pointer' }}
                                onClick={() => {
                                  if (selectedSeats.length === 0) return;
                                  setIsCheckoutOpen(true);
                                }}
                              >
                                Tiếp tục thanh toán <ArrowRight size={16} />
                              </button>
                            </div>
                          </div>
                        )}
                      </div>
                    ))
                  )}
                </main>
              </div>
            </>
          )}

          {/* TAB 2: VÉ CỦA TÔI */}
          {systemTab === 'mytickets' && (
            <div style={{ maxWidth: '960px', margin: '40px auto', padding: '0 20px', width: '100%' }}>
              <div style={{ background: 'white', padding: '30px', borderRadius: '20px', border: '1px solid #e2e8f0', boxShadow: '0 4px 16px rgba(0,0,0,0.05)' }}>
                <h2 style={{ fontSize: '24px', fontWeight: 800, color: '#0f172a', marginBottom: '8px' }}>
                  Vé của tôi ({myTickets.length})
                </h2>
                <p style={{ color: '#64748b', marginBottom: '24px' }}>
                  Quản lý và tra cứu các chuyến xe bạn đã đặt trên hệ thống XeNhanh.
                </p>

                {myTickets.length === 0 ? (
                  <div style={{ textAlign: 'center', padding: '60px 20px' }}>
                    <Ticket size={48} color="#94a3b8" style={{ margin: '0 auto 16px' }} />
                    <h3 style={{ fontSize: '18px', fontWeight: 700, color: '#334155' }}>Chưa có vé nào được đặt</h3>
                    <p style={{ color: '#64748b', marginTop: '6px' }}>
                      Hãy tìm kiếm chuyến xe và đặt vé để xem vé điện tử tại đây!
                    </p>
                    <button
                      style={{ marginTop: '20px', background: '#186fe8', color: 'white', padding: '10px 24px', borderRadius: '10px', fontWeight: 700 }}
                      onClick={() => setSystemTab('booking')}
                    >
                      Đặt chuyến xe ngay
                    </button>
                  </div>
                ) : (
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
                    {myTickets.map(t => (
                      <div key={t.id} className="eticket-card">
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <div>
                            <span className="eticket-badge">MÃ VÉ: {t.id}</span>
                            <h3 style={{ fontSize: '18px', fontWeight: 800, color: '#0f172a' }}>
                              {t.trip.from} ➔ {t.trip.to}
                            </h3>
                            <span style={{ fontSize: '13px', color: '#64748b' }}>
                              Nhà xe: {t.trip.operator} • Khởi hành: <b>{t.trip.departureTime}</b> ngày {t.departDate}
                            </span>
                          </div>

                          <div style={{ textAlign: 'right' }}>
                            <div style={{ fontSize: '20px', fontWeight: 800, color: '#186fe8' }}>
                              {t.totalAmount.toLocaleString('vi-VN')} đ
                            </div>
                            <span style={{ fontSize: '12px', color: '#16a34a', fontWeight: 700 }}>{t.status}</span>
                          </div>
                        </div>

                        <div style={{ marginTop: '16px', paddingTop: '16px', borderTop: '1px solid #e2e8f0', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <div style={{ fontSize: '13px', color: '#475569' }}>
                            Ghế: <b>{t.seats.join(', ')}</b> | Điểm đón: <b>{t.pickup}</b> | Khách: <b>{t.passenger.name} ({t.passenger.phone})</b>
                          </div>

                          <div style={{ display: 'flex', gap: '10px' }}>
                            <button
                              style={{ background: '#eff6ff', color: '#186fe8', padding: '6px 14px', borderRadius: '8px', fontSize: '13px', fontWeight: 700 }}
                              onClick={() => {
                                setActiveTicket(t);
                              }}
                            >
                              Xem vé điện tử
                            </button>
                            <button
                              style={{ background: '#fee2e2', color: '#b91c1c', padding: '6px 14px', borderRadius: '8px', fontSize: '13px', fontWeight: 700 }}
                              onClick={() => {
                                setMyTickets(myTickets.filter(item => item.id !== t.id));
                                showToast(`Đã hủy vé ${t.id} thành công!`);
                              }}
                            >
                              Hủy vé
                            </button>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          )}

          {/* TAB 3: TRA CỨU VÉ */}
          {systemTab === 'lookup' && (
            <div style={{ maxWidth: '720px', margin: '40px auto', padding: '0 20px', width: '100%' }}>
              <div style={{ background: 'white', padding: '36px', borderRadius: '20px', border: '1px solid #e2e8f0', boxShadow: '0 4px 16px rgba(0,0,0,0.05)' }}>
                <h2 style={{ fontSize: '24px', fontWeight: 800, color: '#0f172a', marginBottom: '8px' }}>
                  Tra cứu thông tin vé xe
                </h2>
                <p style={{ color: '#64748b', marginBottom: '24px' }}>
                  Nhập mã vé hoặc số điện thoại bạn đã dùng khi đặt vé để kiểm tra trạng thái vé.
                </p>

                <div style={{ display: 'flex', gap: '10px', marginBottom: '24px' }}>
                  <input
                    type="text"
                    style={{ flex: 1, padding: '12px 16px', border: '1.5px solid #e2e8f0', borderRadius: '12px', fontSize: '15px' }}
                    placeholder="Nhập mã vé (VD: XN-123456) hoặc Số điện thoại"
                    value={lookupQuery}
                    onChange={(e) => setLookupQuery(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && handleLookup()}
                  />
                  <button
                    style={{ background: '#186fe8', color: 'white', padding: '0 24px', borderRadius: '12px', fontWeight: 700, fontSize: '15px' }}
                    onClick={handleLookup}
                  >
                    Tra cứu
                  </button>
                </div>

                {lookupResult && (
                  <div className="eticket-card">
                    <span className="eticket-badge">KẾT QUẢ TRA CỨU: {lookupResult.id}</span>
                    <h3 style={{ fontSize: '18px', fontWeight: 800, color: '#0f172a' }}>
                      {lookupResult.trip.from} ➔ {lookupResult.trip.to}
                    </h3>
                    <p style={{ color: '#475569', fontSize: '13.5px', margin: '8px 0' }}>
                      Nhà xe: <b>{lookupResult.trip.operator}</b> | Xe: <b>{lookupResult.trip.vehicleType}</b><br />
                      Hành khách: <b>{lookupResult.passenger.name}</b> ({lookupResult.passenger.phone})<br />
                      Số ghế: <b>{lookupResult.seats.join(', ')}</b> | Giá vé: <b>{lookupResult.totalAmount.toLocaleString('vi-VN')} đ</b><br />
                      Điểm đón: <b>{lookupResult.pickup}</b><br />
                      Điểm trả: <b>{lookupResult.dropoff}</b><br />
                      Trạng thái: <b style={{ color: '#16a34a' }}>{lookupResult.status}</b>
                    </p>

                    <button
                      style={{ marginTop: '12px', background: '#186fe8', color: 'white', padding: '8px 18px', borderRadius: '8px', fontWeight: 700 }}
                      onClick={() => setActiveTicket(lookupResult)}
                    >
                      Mở vé điện tử & In vé
                    </button>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* ===============================================================
              MODAL 1: CHECKOUT & THANH TOÁN
             =============================================================== */}
          {isCheckoutOpen && selectedTrip && (
            <div className="modal-overlay">
              <div className="checkout-modal-card">
                <div className="modal-header">
                  <h3 className="modal-title">Thông tin hành khách & Thanh toán</h3>
                  <button className="btn-close-modal" onClick={() => setIsCheckoutOpen(false)}>
                    <X size={18} />
                  </button>
                </div>

                {/* Tóm tắt chuyến */}
                <div style={{ background: '#f8fafc', padding: '16px', borderRadius: '14px', marginBottom: '20px', border: '1px solid #e2e8f0' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '6px' }}>
                    <span style={{ fontWeight: 800, color: '#0f172a' }}>{selectedTrip.operator}</span>
                    <span style={{ fontWeight: 700, color: '#186fe8' }}>{selectedTrip.vehicleType}</span>
                  </div>
                  <div style={{ fontSize: '13.5px', color: '#475569' }}>
                    Tuyến: <b>{selectedTrip.from} → {selectedTrip.to}</b> ({selectedTrip.departureTime} ngày {departDate})<br />
                    Ghế đã chọn: <b>{selectedSeats.join(', ')}</b> ({selectedSeats.length} vé)<br />
                    Đón tại: <b>{selectedPickup}</b>
                  </div>
                </div>

                {/* Form thông tin người đi */}
                <div style={{ marginBottom: '20px' }}>
                  <h4 style={{ fontSize: '15px', fontWeight: 700, marginBottom: '12px', color: '#1e293b' }}>
                    Thông tin người liên hệ / đi xe
                  </h4>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px', marginBottom: '12px' }}>
                    <div>
                      <label style={{ fontSize: '12px', fontWeight: 700, color: '#64748b' }}>Họ và tên *</label>
                      <input
                        type="text"
                        style={{ width: '100%', padding: '10px 12px', border: '1.5px solid #e2e8f0', borderRadius: '10px', marginTop: '4px' }}
                        value={passengerName}
                        onChange={(e) => setPassengerName(e.target.value)}
                        placeholder="Nguyễn Văn A"
                      />
                    </div>
                    <div>
                      <label style={{ fontSize: '12px', fontWeight: 700, color: '#64748b' }}>Số điện thoại *</label>
                      <input
                        type="tel"
                        style={{ width: '100%', padding: '10px 12px', border: '1.5px solid #e2e8f0', borderRadius: '10px', marginTop: '4px' }}
                        value={passengerPhone}
                        onChange={(e) => setPassengerPhone(e.target.value)}
                        placeholder="0912345678"
                      />
                    </div>
                  </div>

                  <div>
                    <label style={{ fontSize: '12px', fontWeight: 700, color: '#64748b' }}>Email nhận vé điện tử</label>
                    <input
                      type="email"
                      style={{ width: '100%', padding: '10px 12px', border: '1.5px solid #e2e8f0', borderRadius: '10px', marginTop: '4px' }}
                      value={passengerEmail}
                      onChange={(e) => setPassengerEmail(e.target.value)}
                      placeholder="email@example.com"
                    />
                  </div>
                </div>

                {/* Mã giảm giá */}
                <div style={{ marginBottom: '20px' }}>
                  <label style={{ fontSize: '12px', fontWeight: 700, color: '#64748b', display: 'block', marginBottom: '6px' }}>
                    Mã ưu đãi giảm giá (Thử: <b>XENHANH20</b> hoặc <b>SVCNTTK23</b>)
                  </label>
                  <div className="coupon-input-wrap">
                    <input
                      type="text"
                      className="coupon-input"
                      placeholder="Nhập mã voucher"
                      value={couponCode}
                      onChange={(e) => setCouponCode(e.target.value)}
                    />
                    <button type="button" className="btn-apply-coupon" onClick={handleApplyCoupon}>
                      Áp dụng
                    </button>
                  </div>
                </div>

                {/* Phương thức thanh toán */}
                <div style={{ marginBottom: '24px' }}>
                  <h4 style={{ fontSize: '15px', fontWeight: 700, marginBottom: '12px', color: '#1e293b' }}>
                    Chọn phương thức thanh toán an toàn
                  </h4>

                  <div className="payment-methods-grid">
                    <div
                      className={`payment-method-item ${paymentMethod === 'vnpay' ? 'selected' : ''}`}
                      onClick={() => setPaymentMethod('vnpay')}
                    >
                      <div className="method-icon"><QrCode size={20} /></div>
                      <div>
                        <div className="method-name">VNPAY-QR</div>
                        <div className="method-desc">Quét mã bằng app ngân hàng</div>
                      </div>
                    </div>

                    <div
                      className={`payment-method-item ${paymentMethod === 'momo' ? 'selected' : ''}`}
                      onClick={() => setPaymentMethod('momo')}
                    >
                      <div className="method-icon"><Smartphone size={20} /></div>
                      <div>
                        <div className="method-name">Ví MoMo</div>
                        <div className="method-desc">Thanh toán ví điện tử</div>
                      </div>
                    </div>

                    <div
                      className={`payment-method-item ${paymentMethod === 'bank' ? 'selected' : ''}`}
                      onClick={() => setPaymentMethod('bank')}
                    >
                      <div className="method-icon"><CreditCard size={20} /></div>
                      <div>
                        <div className="method-name">Thẻ ATM / Visa</div>
                        <div className="method-desc">Cổng Napas / Thẻ quốc tế</div>
                      </div>
                    </div>

                    <div
                      className={`payment-method-item ${paymentMethod === 'cash' ? 'selected' : ''}`}
                      onClick={() => setPaymentMethod('cash')}
                    >
                      <div className="method-icon"><Banknote size={20} /></div>
                      <div>
                        <div className="method-name">Tiền mặt khi lên xe</div>
                        <div className="method-desc">Thanh toán cho phụ xe</div>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Tổng tiền và Nút hoàn tất */}
                <div style={{ borderTop: '1px solid #e2e8f0', paddingTop: '18px', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                  <div>
                    <span style={{ fontSize: '13px', color: '#64748b' }}>Tổng thanh toán:</span>
                    <div style={{ fontSize: '26px', fontWeight: 800, color: '#186fe8', lineHeight: 1 }}>
                      {calculateTotal().toLocaleString('vi-VN')} đ
                    </div>
                  </div>

                  <button
                    className="btn-proceed-checkout"
                    onClick={handleConfirmBooking}
                  >
                    Xác nhận & Thanh toán ngay
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* ===============================================================
              MODAL 2: VÉ ĐIỆN TỬ (E-TICKET) XUẤT SAU KHI ĐẶT THÀNH CÔNG
             =============================================================== */}
          {activeTicket && (
            <div className="modal-overlay">
              <div className="checkout-modal-card" style={{ maxWidth: '540px' }}>
                <div className="modal-header">
                  <h3 className="modal-title" style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <CheckCircle2 size={22} color="#10b981" /> Vé điện tử XeNhanh
                  </h3>
                  <button className="btn-close-modal" onClick={() => setActiveTicket(null)}>
                    <X size={18} />
                  </button>
                </div>

                <div className="eticket-card" id="printable-ticket">
                  <div style={{ textAlign: 'center', marginBottom: '14px' }}>
                    <div style={{ fontSize: '22px', fontWeight: 800, color: '#0b4a9e' }}>XeNhanh Express</div>
                    <div style={{ fontSize: '12px', color: '#64748b' }}>HỆ THỐNG ĐẶT VÉ XE TRỰC TUYẾN</div>
                    <div className="eticket-badge" style={{ marginTop: '8px' }}>
                      MÃ VÉ: {activeTicket.id}
                    </div>
                  </div>

                  {/* QR Code mô phỏng để nhân viên quét lên xe */}
                  <div className="eticket-qr-box">
                    <QrCode size={120} color="#0f172a" />
                    <span style={{ fontSize: '11px', color: '#64748b', marginTop: '6px' }}>
                      Quét mã này khi lên xe để nhận phòng/ghế
                    </span>
                  </div>

                  <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', fontSize: '13.5px', color: '#334155' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid #f1f5f9', paddingBottom: '6px' }}>
                      <span>Tuyến xe:</span>
                      <b>{activeTicket.trip.from} ➔ {activeTicket.trip.to}</b>
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid #f1f5f9', paddingBottom: '6px' }}>
                      <span>Nhà xe:</span>
                      <b>{activeTicket.trip.operator}</b>
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid #f1f5f9', paddingBottom: '6px' }}>
                      <span>Thời gian khởi hành:</span>
                      <b>{activeTicket.trip.departureTime} ({activeTicket.departDate})</b>
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid #f1f5f9', paddingBottom: '6px' }}>
                      <span>Số ghế / giường:</span>
                      <b style={{ color: '#186fe8' }}>{activeTicket.seats.join(', ')}</b>
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid #f1f5f9', paddingBottom: '6px' }}>
                      <span>Điểm đón khách:</span>
                      <b>{activeTicket.pickup}</b>
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid #f1f5f9', paddingBottom: '6px' }}>
                      <span>Hành khách:</span>
                      <b>{activeTicket.passenger.name} ({activeTicket.passenger.phone})</b>
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid #f1f5f9', paddingBottom: '6px' }}>
                      <span>Tổng tiền đã thanh toán:</span>
                      <b style={{ color: '#10b981', fontSize: '16px' }}>
                        {activeTicket.totalAmount.toLocaleString('vi-VN')} đ
                      </b>
                    </div>
                  </div>
                </div>

                <div style={{ display: 'flex', gap: '10px', marginTop: '20px' }}>
                  <button
                    style={{ flex: 1, padding: '12px', background: '#f1f5f9', color: '#0f172a', borderRadius: '10px', fontWeight: 700, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '6px' }}
                    onClick={() => {
                      window.print();
                    }}
                  >
                    <Printer size={16} /> In vé / Tải PDF
                  </button>
                  <button
                    style={{ flex: 1, padding: '12px', background: '#186fe8', color: 'white', borderRadius: '10px', fontWeight: 700 }}
                    onClick={() => {
                      setActiveTicket(null);
                      setSystemTab('mytickets');
                    }}
                  >
                    Xem trong Vé của tôi
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Footer thông tin môn học & đề tài */}
          <footer className="system-footer">
            <div className="footer-container">
              <div className="footer-top-grid">
                <div className="footer-brand">
                  <h4>XeNhanh - Đặt xe dễ dàng, Đi muôn nơi</h4>
                  <p>
                    Nền tảng thương mại điện tử kết nối hành khách với hàng trăm nhà xe chất lượng cao trên cả nước. Đảm bảo an toàn, tiện lợi, giá niêm yết chính xác.
                  </p>
                </div>

                <div className="footer-col">
                  <h5>Tuyến đường hot</h5>
                  <ul className="footer-links">
                    <li>Hà Nội - Thái Nguyên</li>
                    <li>Hà Nội - Sa Pa</li>
                    <li>Sài Gòn - Đà Lạt</li>
                    <li>Đà Nẵng - Huế</li>
                  </ul>
                </div>

                <div className="footer-col">
                  <h5>Dịch vụ</h5>
                  <ul className="footer-links">
                    <li>Đặt xe Limousine</li>
                    <li>Xe khách giường nằm</li>
                    <li>Thuê xe du lịch hợp đồng</li>
                    <li>Tra cứu vé điện tử</li>
                  </ul>
                </div>

                {/* Thông tin sinh viên & đề tài */}
                <div className="footer-student-box">
                  <h6>BÀI TẬP LỚN PHÁT TRIỂN ỨNG DỤNG PHÂN TÁN</h6>
                  <p><b>Đề tài 17:</b> Hệ thống đặt chuyến xe (XeNhanh)</p>
                  <p><b>Lớp:</b> CNTTK23E (K23.K1.D2.2627.CNTT K23E)</p>
                  <p><b>Sinh viên thực hiện:</b> Thào A Póng</p>
                  <p><b>Mã sinh viên:</b> dtc245200592</p>
                  <p><b>Trường:</b> ĐH Công nghệ Thông tin & Truyền thông - ĐH Thái Nguyên (ICTU)</p>
                </div>
              </div>

              <div className="footer-bottom">
                © 2026 XeNhanh. Dự án Đề tài 17 - Hệ thống đặt chuyến xe. Trường ĐH CNTT & Truyền thông - ĐHTN.
              </div>
            </div>
          </footer>
        </div>
      )}
    </div>
  );
}
