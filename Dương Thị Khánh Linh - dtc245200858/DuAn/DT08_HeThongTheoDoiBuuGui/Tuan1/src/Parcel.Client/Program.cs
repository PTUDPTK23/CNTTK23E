
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5100";

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

Console.OutputEncoding = System.Text.Encoding.UTF8;

while (true)
{
    Console.WriteLine();
    Console.WriteLine("==================================");
    Console.WriteLine("    HE THONG THEO DOI BUU GUI");
    Console.WriteLine("==================================");
    Console.WriteLine("1. Xem danh sach buu gui");
    Console.WriteLine("2. Tra cuu buu gui theo ma");
    Console.WriteLine("3. Xem lich su van chuyen");
    Console.WriteLine("4. Tao buu gui moi");
    Console.WriteLine("5. Cap nhat trang thai buu gui");
    Console.WriteLine("0. Thoat");
    Console.Write("Vui long chon chuc nang: ");

    var choice = Console.ReadLine();

    try
    {
        if (choice == "1")
        {
            var parcels = await httpClient
                .GetFromJsonAsync<List<ParcelDto>>("/api/parcels");

            Console.WriteLine("\nDANH SACH BUU GUI");

            if (parcels == null || parcels.Count == 0)
            {
                Console.WriteLine("Chua co buu gui nao.");
                continue;
            }

            foreach (var parcel in parcels)
            {
                Console.WriteLine("----------------------------------");
                Console.WriteLine($"Ma buu gui: {parcel.Code}");
                Console.WriteLine($"Nguoi gui: {parcel.Sender}");
                Console.WriteLine($"Nguoi nhan: {parcel.Receiver}");
                Console.WriteLine($"Trang thai: {parcel.Status}");
            }
        }
        else if (choice == "2")
        {
            Console.Write("\nNhap ma buu gui: ");
            var code = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(code))
            {
                Console.WriteLine("Vui long nhap ma buu gui.");
                continue;
            }

            var response = await httpClient.GetAsync(
                $"/api/parcels/{Uri.EscapeDataString(code)}");

            if (response.IsSuccessStatusCode)
            {
                var parcel = await response.Content
                    .ReadFromJsonAsync<ParcelDto>();

                Console.WriteLine("\nTHONG TIN BUU GUI");
                Console.WriteLine($"Ma buu gui: {parcel?.Code}");
                Console.WriteLine($"Nguoi gui: {parcel?.Sender}");
                Console.WriteLine($"Nguoi nhan: {parcel?.Receiver}");
                Console.WriteLine($"Dia chi: {parcel?.Address}");
                Console.WriteLine($"Noi dung: {parcel?.Description}");
                Console.WriteLine($"Trang thai: {parcel?.Status}");
                Console.WriteLine($"Ngay tao: {parcel?.CreatedAt}");
            }
            else if (response.StatusCode ==
                     System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine("Khong tim thay buu gui.");
            }
            else
            {
                Console.WriteLine(
                    $"Loi HTTP: {(int)response.StatusCode}");
            }
        }
        else if (choice == "3")
        {
            Console.Write("\nNhap ma buu gui: ");
            var code = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(code))
            {
                Console.WriteLine("Vui long nhap ma buu gui.");
                continue;
            }

            var response = await httpClient.GetAsync(
                $"/api/parcels/{Uri.EscapeDataString(code)}/history");

            if (response.IsSuccessStatusCode)
            {
                var histories = await response.Content
                    .ReadFromJsonAsync<List<ParcelHistoryDto>>();

                Console.WriteLine("\nLICH SU VAN CHUYEN");

                if (histories == null || histories.Count == 0)
                {
                    Console.WriteLine("Chua co lich su van chuyen.");
                    continue;
                }

                foreach (var history in histories)
                {
                    Console.WriteLine("----------------------------------");
                    Console.WriteLine($"Ma buu gui: {history.ParcelCode}");
                    Console.WriteLine($"Trang thai: {history.Status}");
                    Console.WriteLine($"Thoi gian: {history.UpdatedAt}");
                    Console.WriteLine($"Ghi chu: {history.Note}");
                }
            }
            else if (response.StatusCode ==
                     System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine("Khong tim thay buu gui.");
            }
            else
            {
                Console.WriteLine(
                    $"Loi HTTP: {(int)response.StatusCode}");
            }
        }

        else if (choice == "4")
        {
            Console.WriteLine("\nTAO BUU GUI MOI");

            Console.Write("Nhap ten nguoi gui: ");
            var sender = Console.ReadLine();

            Console.Write("Nhap ten nguoi nhan: ");
            var receiver = Console.ReadLine();

            Console.Write("Nhap dia chi nhan: ");
            var address = Console.ReadLine();

            Console.Write("Nhap mo ta buu gui: ");
            var description = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(sender) ||
                string.IsNullOrWhiteSpace(receiver) ||
                string.IsNullOrWhiteSpace(address))
            {
                Console.WriteLine(
                    "Thong tin nguoi gui, nguoi nhan va dia chi khong duoc de trong.");
                continue;
            }

            var newParcel = new CreateParcelRequest
            {
                Sender = sender,
                Receiver = receiver,
                Address = address,
                Description = description ?? ""
            };

            var response = await httpClient.PostAsJsonAsync(
                "/api/parcels", newParcel);

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Created)
            {
                var parcel = await response.Content
                    .ReadFromJsonAsync<ParcelDto>();

                Console.WriteLine("\nTAO BUU GUI THANH CONG!");
                Console.WriteLine($"Ma buu gui: {parcel?.Code}");
                Console.WriteLine($"Nguoi gui: {parcel?.Sender}");
                Console.WriteLine($"Nguoi nhan: {parcel?.Receiver}");
                Console.WriteLine($"Trang thai: {parcel?.Status}");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Tao buu gui that bai!");
                Console.WriteLine($"Loi HTTP: {(int)response.StatusCode}");
                Console.WriteLine($"Chi tiet: {error}");
            }
        }

        else if (choice == "5")
        {
            Console.WriteLine("\nCAP NHAT TRANG THAI BUU GUI");

            Console.Write("Nhap ma buu gui: ");
            var code = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(code))
            {
                Console.WriteLine("Vui long nhap ma buu gui.");
            continue;
            }

            Console.WriteLine("Cac trang thai co the chon:");
            Console.WriteLine("1. Accepted");
            Console.WriteLine("2. InTransit");
            Console.WriteLine("3. AtDeliveryOffice");
            Console.WriteLine("4. OutForDelivery");
            Console.WriteLine("5. Delivered");
            Console.WriteLine("6. DeliveryFailed");
            Console.WriteLine("7. Returned");

            Console.Write("Chon trang thai: ");
            var statusChoice = Console.ReadLine();

            var status = statusChoice switch
            {
                "1" => "Accepted",
                "2" => "InTransit",
                "3" => "AtDeliveryOffice",
                "4" => "OutForDelivery",
                "5" => "Delivered",
                "6" => "DeliveryFailed",
                "7" => "Returned",
                 _ => ""
            };

            if (string.IsNullOrEmpty(status))
            {
                Console.WriteLine("Lua chon trang thai khong hop le.");
            continue;
            }

            var updateRequest = new UpdateStatusRequest
            {
                Status = status
            };

            var response = await httpClient.PutAsJsonAsync(
            $"/api/parcels/{Uri.EscapeDataString(code)}/status",
            updateRequest);

            if (response.IsSuccessStatusCode)
            {
            var parcel = await response.Content
            .ReadFromJsonAsync<ParcelDto>();

            Console.WriteLine("\nCAP NHAT THANH CONG!");
            Console.WriteLine($"Ma buu gui: {parcel?.Code}");
            Console.WriteLine($"Trang thai moi: {parcel?.Status}");
            }
            else if (response.StatusCode ==
            System.Net.HttpStatusCode.NotFound)
            {
            Console.WriteLine("Khong tim thay buu gui.");
            }
            else
            {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Cap nhat that bai!");
            Console.WriteLine($"Loi HTTP: {(int)response.StatusCode}");
            Console.WriteLine($"Chi tiet: {error}");
            }
        }

        else if (choice == "0")
        {
            Console.WriteLine("Da thoat chuong trinh.");
            break;
        }
        else
        {
            Console.WriteLine("Lua chon khong hop le.");
        }
    }
    catch (HttpRequestException)
    {
        Console.WriteLine(
            "Khong the ket noi API. Hay kiem tra may chu.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Co loi xay ra: {ex.Message}");
    }
}

public class ParcelDto
{
    public string Code { get; set; } = "";
    public string Sender { get; set; } = "";
    public string Receiver { get; set; } = "";
    public string Address { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class ParcelHistoryDto
{
    public string ParcelCode { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
    public string Note { get; set; } = "";
}

public class CreateParcelRequest
{
    public string Sender { get; set; } = "";
    public string Receiver { get; set; } = "";
    public string Address { get; set; } = "";
    public string Description { get; set; } = "";
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = "";
}

