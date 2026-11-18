
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Dữ liệu mẫu ban đầu
var parcels = new List<Parcel>
{
    new Parcel
    {
        Code = "BG001",
        Sender = "Nguyen Van A",
        Receiver = "Tran Thi B",
        Address = "Thai Nguyen",
        Description = "Tai lieu hoc tap",
        Status = "Accepted",
        CreatedAt = DateTime.Now
    }
};
var parcelHistories = new List<ParcelHistory>
{
    new ParcelHistory
    {
        ParcelCode = "BG001",
        Status = "Created",
        UpdatedAt = DateTime.Now.AddHours(-2),
        Note = "Buu gui duoc tao"
    },
    new ParcelHistory
    {
        ParcelCode = "BG001",
        Status = "Accepted",
        UpdatedAt = DateTime.Now.AddHours(-1),
        Note = "Buu gui da duoc tiep nhan"
    },
    new ParcelHistory
    {
        ParcelCode = "BG001",
        Status = "InTransit",
        UpdatedAt = DateTime.Now,
        Note = "Buu gui dang duoc van chuyen"
    }
};

// Trang kiểm tra API
app.MapGet("/", () => "Parcel Tracking API is running!");

// 1. Xem danh sách bưu gửi
app.MapGet("/api/parcels", () =>
{
    return Results.Ok(parcels);
});

// 2. Tra cứu bưu gửi theo mã
app.MapGet("/api/parcels/{code}", (string code) =>
{
    var parcel = parcels.FirstOrDefault(
        p => p.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    if (parcel == null)
        return Results.NotFound("Khong tim thay buu gui");

    return Results.Ok(parcel);
});

// 3. Tạo bưu gửi mới
app.MapPost("/api/parcels", (CreateParcelRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Sender) ||
        string.IsNullOrWhiteSpace(request.Receiver) ||
        string.IsNullOrWhiteSpace(request.Address))
    {
        return Results.BadRequest("Thong tin khong duoc de trong");
    }

    var parcel = new Parcel
    {
        Code = "BG" + (parcels.Count + 1).ToString("D3"),
        Sender = request.Sender,
        Receiver = request.Receiver,
        Address = request.Address,
        Description = request.Description,
        Status = "Created",
        CreatedAt = DateTime.Now
    };

    parcels.Add(parcel);

parcelHistories.Add(new ParcelHistory
{
    ParcelCode = parcel.Code,
    Status = "Created",
    UpdatedAt = DateTime.Now,
    Note = "Buu gui moi duoc tao"
});

return Results.Created(
    $"/api/parcels/{parcel.Code}", parcel);
});

// 4. Cập nhật trạng thái bưu gửi
app.MapPut("/api/parcels/{code}/status",
    (string code, UpdateStatusRequest request) =>
{
    var parcel = parcels.FirstOrDefault(
        p => p.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    if (parcel == null)
        return Results.NotFound("Khong tim thay buu gui");

    var validStatuses = new[]
    {
        "Created",
        "Accepted",
        "InTransit",
        "AtDeliveryOffice",
        "OutForDelivery",
        "Delivered",
        "DeliveryFailed",
        "Returned"
    };

    if (!validStatuses.Contains(request.Status))
        return Results.BadRequest("Trang thai khong hop le");

    parcel.Status = request.Status;

var history = new ParcelHistory
{
    ParcelCode = parcel.Code,
    Status = request.Status,
    UpdatedAt = DateTime.Now,
    Note = "Trang thai buu gui duoc cap nhat"
};

parcelHistories.Add(history);

return Results.Ok(parcel);
});

// 5. Xem lịch sử vận chuyển theo mã bưu gửi
app.MapGet("/api/parcels/{code}/history", (string code) =>
{
    var parcel = parcels.FirstOrDefault(
        p => p.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    if (parcel == null)
        return Results.NotFound("Khong tim thay buu gui");

    var history = parcelHistories
        .Where(h => h.ParcelCode.Equals(
            code, StringComparison.OrdinalIgnoreCase))
        .OrderBy(h => h.UpdatedAt)
        .ToList();

    return Results.Ok(history);
});

// 6. Chạy API
app.Run();

// Các lớp dữ liệu
class Parcel
{
    public string Code { get; set; } = "";
    public string Sender { get; set; } = "";
    public string Receiver { get; set; } = "";
    public string Address { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

record CreateParcelRequest(
    string Sender,
    string Receiver,
    string Address,
    string Description
);

record UpdateStatusRequest(string Status);
class ParcelHistory
{
    public string ParcelCode { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
    public string Note { get; set; } = "";
}