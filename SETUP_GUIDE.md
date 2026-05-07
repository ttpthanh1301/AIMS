# 🚀 HƯỚNG DẪN CÀI ĐẶT MÔI TRƯỜNG VÀ CHẠY DỰ ÁN AIMS

## 📋 Mục lục

1. [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
2. [Cài đặt .NET SDK](#cài-đặt-net-sdk)
3. [Cài đặt SQL Server](#cài-đặt-sql-server)
4. [Cải đặt dự án](#cài-đặt-dự-án)
5. [Chạy dự án](#chạy-dự-án)
6. [Kiểm tra và xác thực](#kiểm-tra-và-xác-thực)
7. [Khắc phục sự cố](#khắc-phục-sự-cố)

---

## ⚙️ Yêu cầu hệ thống

### Cho macOS

- **macOS**: 12.x hoặc cao hơn (M1/M2/M3 hoặc Intel)
- **.NET SDK**: 10.0 hoặc cao hơn
- **SQL Server**: 2022 (qua Docker)
- **Docker**: Docker Desktop mới nhất
- **Công cụ**: Terminal, Git, Visual Studio Code (tuỳ chọn)

### Cho Windows

- **Windows**: Windows 10 hoặc Windows 11
- **.NET SDK**: 10.0 hoặc cao hơn
- **SQL Server**: 2022 hoặc SQL Server Express
- **Docker** (tuỳ chọn): Docker Desktop mới nhất
- **Công cụ**: PowerShell, Git, Visual Studio Code hoặc Visual Studio

### Cho Linux

- **Linux**: Ubuntu 20.04 LTS hoặc cao hơn
- **.NET SDK**: 10.0 hoặc cao hơn
- **SQL Server**: 2022 (qua Docker)
- **Docker**: Docker CE mới nhất

---

## 1️⃣ Cài đặt .NET SDK

### Bước 1: Tải và cài đặt .NET SDK

**Trên macOS:**

```bash
# Cài đặt qua Homebrew
brew install dotnet-sdk

# Hoặc tải từ Microsoft
# https://dotnet.microsoft.com/download
```

**Trên Windows:**

```powershell
# Cài đặt qua Chocolatey
choco install dotnet-sdk

# Hoặc tải trực tiếp từ:
# https://dotnet.microsoft.com/download
```

**Trên Linux (Ubuntu):**

```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version latest

# Hoặc
sudo apt-get update
sudo apt-get install dotnet-sdk-10.0
```

### Bước 2: Kiểm tra cài đặt

```bash
dotnet --version
```

**Kết quả mong muốn:**

```
10.0.3 (hoặc phiên bản cao hơn)
```

---

## 2️⃣ Cài đặt SQL Server

### Phương án A: Sử dụng Docker Compose (Khuyên dùng)

#### Yêu cầu

- Docker Desktop đã được cài đặt
- Cổng 1433 có sẵn

#### Cài đặt SQL Server

**Bước 1: Navigating to project directory**

```bash
cd /path/to/AIMS
```

**Bước 2: Khởi động SQL Server bằng Docker Compose**

```bash
docker-compose -f docker-compose.build.yml up -d sqlserver
```

**Bước 3: Kiểm tra SQL Server đã chạy**

```bash
# Xem logs để kiểm tra health check
docker logs aims-sqlserver

# Hoặc kiểm tra trạng thái
docker ps | grep sqlserver
```

#### Thông tin kết nối

- **Server**: `localhost,1433` hoặc `127.0.0.1,1433`
- **Username**: `sa`
- **Password**: `AIMS@2025!`
- **Database**: `AIMS` (sẽ được tạo tự động)
- **TrustServerCertificate**: `True`

---

### Phương án B: Cài đặt SQL Server trực tiếp (Chỉ macOS Intel)

⚠️ **Lưu ý**: SQL Server 2022 chỉ hỗ trợ macOS Intel, không hỗ trợ Apple Silicon (M1/M2/M3). **Sử dụng Docker Compose thay thế**.

---

## 3️⃣ Cài đặt dự án

### Bước 1: Clone hoặc mở dự án

```bash
# Nếu chưa có dự án
git clone <repository-url> AIMS
cd AIMS

# Hoặc nếu đã có
cd /path/to/AIMS
```

### Bước 2: Khôi phục NuGet packages

```bash
dotnet restore
```

**Kết quả mong muốn:**

```
Restore complete (X.Xs)
```

### Bước 3: Kiểm tra cấu hình database

Mở file `src/AIMS.BackendServer/appsettings.json` và kiểm tra:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AIMS;User Id=sa;Password=AIMS@2025!;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true"
  }
}
```

**Lưu ý**: Nếu dùng SQL Server khác, thay đổi connection string phù hợp.

### Bước 4: Chạy Entity Framework migrations (Tạo database)

```bash
# Từ thư mục gốc dự án
dotnet ef database update --project src/AIMS.BackendServer

# Hoặc từ thư mục BackendServer
cd src/AIMS.BackendServer
dotnet ef database update
cd ../..
```

**Kết quả mong muốn:**

```
Done. Successful database creation (X ms)
```

Nếu lỗi, xem phần [Khắc phục sự cố](#khắc-phục-sự-cố).

---

## 4️⃣ Chạy dự án

### Phương án 1: Chạy cả hai dịch vụ (Khuyên dùng)

#### Terminal 1 - Chạy Backend API

```bash
cd /path/to/AIMS
cd src/AIMS.BackendServer
dotnet run

# Hoặc với hot reload
dotnet watch run
```

**Kết quả mong muốn:**

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to exit.
```

#### Terminal 2 - Chạy Web Portal

```bash
cd /path/to/AIMS
cd src/AIMS.WebPortal
dotnet run

# Hoặc với hot reload
dotnet watch run
```

**Kết quả mong muốn:**

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5011
      Now listening on: http://localhost:5010
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to exit.
```

---

### Phương án 2: Chạy từ thư mục gốc

```bash
cd /path/to/AIMS

# Compile
dotnet build

# Chạy BackendServer
dotnet run --project src/AIMS.BackendServer/AIMS.BackendServer.csproj

# Trong terminal khác, chạy WebPortal
dotnet run --project src/AIMS.WebPortal/AIMS.WebPortal.csproj
```

---

### Phương án 3: Chạy Unit Tests

```bash
cd /path/to/AIMS

# Chạy tất cả tests
dotnet test

# Hoặc chạy với verbose
dotnet test --verbosity detailed

# Hoặc chạy riêng project test
dotnet test test/AIMS.BackendServer.UnitTests
```

---

## 5️⃣ Kiểm tra và xác thực

### Kiểm tra Backend API

#### 🔍 Scalar UI (Modern API Documentation)

- **URL**: `http://localhost:5000/scalar/v1` hoặc `https://localhost:5001/scalar/v1`
- **Tính năng**:
  - Giao diện hiện đại hơn Swagger
  - Test API endpoints trực tiếp
  - Xem request/response chi tiết
  - Hỗ trợ authentication

```bash
# Truy cập Scalar UI
open http://localhost:5000/scalar/v1
```

#### 📖 Swagger UI (Mặc định)

- **URL**: `http://localhost:5000/swagger` hoặc `https://localhost:5001/swagger`
- **Tính năng**:
  - Danh sách tất cả API endpoints
  - Test API trực tiếp từ UI
  - Xem schema của request/response
  - OpenAPI definition

```bash
# Truy cập Swagger UI
open http://localhost:5000/swagger
```

#### 🤖 OpenAI Integration

**Kiểm tra OpenAI API được cấu hình:**

1. **Kiểm tra appsettings.json:**

```bash
cat src/AIMS.BackendServer/appsettings.json | grep -i openai
```

2. **Thêm OpenAI Key (nếu chưa có):**

Mở `src/AIMS.BackendServer/appsettings.Development.json` và thêm:

```json
{
  "OpenAI": {
    "ApiKey": "sk-your-openai-api-key-here",
    "Model": "gpt-4",
    "BaseUrl": "https://api.openai.com/v1"
  }
}
```

3. **Hoặc sử dụng User Secrets (Khuyên dùng):**

```bash
cd src/AIMS.BackendServer

# Cài đặt User Secrets ID
dotnet user-secrets init

# Thêm OpenAI Key
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-openai-api-key-here"
dotnet user-secrets set "OpenAI:Model" "gpt-4"

# Xem tất cả secrets
dotnet user-secrets list
```

4. **Kiểm tra OpenAI endpoint:**

Từ Scalar UI hoặc Swagger, gọi endpoint OpenAI (ví dụ):

```bash
# Test API OpenAI
curl -X POST http://localhost:5000/api/ai/generate \
  -H "Content-Type: application/json" \
  -d '{"prompt":"Hello OpenAI"}'
```

**Lỗi thường gặp:**

- ❌ `401 Unauthorized`: OpenAI API Key sai hoặc hết hạn
- ❌ `429 Too Many Requests`: Vượt quá rate limit
- ❌ `404 Not Found`: Endpoint không tồn tại

**Giải pháp:**

```bash
# 1. Kiểm tra API Key có đúng không
echo $OPENAI_API_KEY

# 2. Test API Key trực tiếp
curl https://api.openai.com/v1/models \
  -H "Authorization: Bearer sk-your-key"

# 3. Xem logs chi tiết
dotnet run -- --verbosity diagnostic
```

#### ❤️ Health Check

**Kiểm tra API health:**

```bash
# HTTP endpoint
curl -X GET http://localhost:5000/health

# HTTPS endpoint
curl -X GET https://localhost:5001/health

# Kết quả mong muốn
# {"status":"Healthy","timestamp":"2024-05-06T..."}
```

### Kiểm tra Web Portal

**URL**: `http://localhost:5010` hoặc `https://localhost:5011`

**Kiểm tra:**

- ✅ Giao diện web tải lên bình thường
- ✅ Không có lỗi trong browser console (F12)
- ✅ Các API call từ frontend đến backend thành công
- ✅ Có thể đăng nhập (nếu đã cấu hình authentication)

```bash
# Mở Web Portal
open http://localhost:5010
```

### Kiểm tra Database Connection

**Phương án 1: Từ API (Khuyên dùng)**

Từ Scalar UI hoặc Swagger, gọi một endpoint bất kỳ (ví dụ: GET /api/users). Nếu database kết nối thành công:

- ✅ Response không có lỗi connection string
- ✅ Dữ liệu trả về từ database

**Phương án 2: SQL Server Management Studio (SSMS)**

```bash
# macOS/Linux - Cài sqlcmd
brew install mssql-tools18

# Kết nối trực tiếp
sqlcmd -S localhost,1433 -U sa -P AIMS@2025! -d AIMS

# Kiểm tra databases
> SELECT name FROM sys.databases
```

**Phương án 3: Azure Data Studio**

1. Tải: https://learn.microsoft.com/en-us/sql/azure-data-studio/download-azure-data-studio
2. Tạo connection mới:
   - Server: `localhost,1433`
   - Database: `AIMS`
   - User: `sa`
   - Password: `AIMS@2025!`
3. Nhấn Connect

### 📊 Kiểm tra toàn bộ hệ thống

**Checklist:**

| Thành phần         | URL/Command                         | Kết quả mong muốn            |
| ------------------ | ----------------------------------- | ---------------------------- |
| Scalar UI          | `http://localhost:5000/scalar/v1`   | 200 OK + Giao diện UI        |
| Swagger UI         | `http://localhost:5000/swagger`     | 200 OK + Danh sách endpoints |
| Health Check       | `curl http://localhost:5000/health` | `{"status":"Healthy"}`       |
| OpenAI Integration | Gọi endpoint AI từ Scalar           | 200 OK + Response từ OpenAI  |
| Web Portal         | `http://localhost:5010`             | 200 OK + Giao diện web       |
| Database           | `sqlcmd -S localhost,1433...`       | Kết nối thành công           |

**Script kiểm tra toàn bộ:**

```bash
#!/bin/bash
echo "🔍 Kiểm tra Backend API..."
curl -s http://localhost:5000/health | jq .
echo "\n✅ Scalar UI: http://localhost:5000/scalar/v1"
echo "✅ Swagger UI: http://localhost:5000/swagger"
echo "✅ Web Portal: http://localhost:5010"
echo "✅ OpenAI: Kiểm tra từ Scalar UI"
```

Lưu vào file `health-check.sh` và chạy:

```bash
chmod +x health-check.sh
./health-check.sh
```

---

## 6️⃣ Các lệnh hữu ích

```bash
# Kiểm tra phiên bản .NET
dotnet --version

# Liệt kê các project trong solution
dotnet sln list

# Build solution
dotnet build

# Build project cụ thể
dotnet build src/AIMS.BackendServer

# Clean build artifacts
dotnet clean

# Khôi phục NuGet packages
dotnet restore

# Chạy migrations (tạo/update database)
dotnet ef database update --project src/AIMS.BackendServer

# Xem migrations
dotnet ef migrations list --project src/AIMS.BackendServer

# Tạo migration mới
dotnet ef migrations add "MigrationName" --project src/AIMS.BackendServer

# Xóa migration cuối cùng
dotnet ef migrations remove --project src/AIMS.BackendServer

# Chạy unit tests
dotnet test

# Chạy tests với code coverage
dotnet test /p:CollectCoverage=true

# Xóa Docker container
docker stop aims-sqlserver
docker rm aims-sqlserver

# Xóa Docker volume (cảnh báo: sẽ mất dữ liệu)
docker volume rm aims_sqldata

# Xem logs Docker
docker logs aims-sqlserver
```

---

## 7️⃣ Khắc phục sự cố

### Lỗi 1: "Cannot connect to database"

**Nguyên nhân**: SQL Server chưa chạy hoặc connection string sai

**Giải pháp**:

```bash
# Kiểm tra SQL Server chạy
docker ps | grep sqlserver

# Nếu không chạy, khởi động
docker-compose -f docker-compose.build.yml up -d sqlserver

# Đợi 30-45 giây để SQL Server hoàn toàn khởi động
sleep 45

# Kiểm tra logs
docker logs aims-sqlserver
```

---

### Lỗi 2: "Duplicate NuGet imports"

**Nguyên nhân**: Các file obj/bin bị hỏng

**Giải pháp**:

```bash
cd /path/to/AIMS

# Xóa tất cả build artifacts
find . -type d -name obj -exec rm -rf {} + 2>/dev/null
find . -type d -name bin -exec rm -rf {} + 2>/dev/null

# Khôi phục và rebuild
dotnet restore
dotnet build
```

---

### Lỗi 3: ".NET SDK version not found"

**Nguyên nhân**: Phiên bản .NET SDK không khớp

**Giải pháp**:

```bash
# Kiểm tra phiên bản hiện tại
dotnet --version

# Cài đặt .NET SDK 10.0
# https://dotnet.microsoft.com/download

# Hoặc sử dụng global.json
# Tạo file global.json trong thư mục gốc dự án (nếu chưa có)
```

---

### Lỗi 4: "Port 5000/5001 đã được sử dụng"

**Giải pháp**:

```bash
# Tìm process dùng port 5000
lsof -i :5000

# Hoặc sử dụng port khác
dotnet run -- --urls "http://localhost:8080"

# Hoặc kill process
kill -9 <PID>
```

---

### Lỗi 5: "Entity Framework tools not installed"

**Giải pháp**:

```bash
# Cài đặt EF Core tools globally
dotnet tool install --global dotnet-ef

# Hoặc update nếu đã cài
dotnet tool update --global dotnet-ef
```

---

### Lỗi 6: "Scalar UI không hiển thị hoặc 404 Not Found"

**Nguyên nhân**: Scalar.AspNetCore chưa được cấu hình trong Program.cs hoặc middleware sai thứ tự

**Giải pháp**:

**1. Kiểm tra Scalar đã được install:**

```bash
dotnet add src/AIMS.BackendServer package Scalar.AspNetCore
```

**2. Kiểm tra Program.cs (BackendServer):**

```csharp
// Trong Program.cs - phải có:

// 1. Thêm Swagger/OpenAPI
builder.Services.AddOpenApi();

// 2. Thêm Scalar
app.MapOpenApi();
app.MapScalarApiReference();

// 3. Thêm Swagger UI (tuỳ chọn)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

**3. Truy cập Scalar:**

```bash
# Scalar UI
open http://localhost:5000/scalar/v1

# Hoặc kiểm tra endpoint
curl -X GET http://localhost:5000/scalar/v1
```

**4. Nếu vẫn 404:**

```bash
# Xóa cache và rebuild
dotnet clean
dotnet build
dotnet run
```

---

### Lỗi 7: "OpenAI API lỗi hoặc không kết nối"

**Nguyên nhân**: OpenAI API Key sai, expired, hoặc không được cấu hình

**Giải pháp:**

**1. Kiểm tra OpenAI Key cấu hình:**

```bash
# Xem appsettings.Development.json
cat src/AIMS.BackendServer/appsettings.Development.json | grep -A 5 OpenAI

# Hoặc xem User Secrets
cd src/AIMS.BackendServer
dotnet user-secrets list
```

**2. Kiểm tra OpenAI Service register trong Program.cs:**

```csharp
// Phải có trong Program.cs:
var openAiSettings = builder.Configuration.GetSection("OpenAI");
builder.Services.Configure<OpenAiSettings>(openAiSettings);

// Hoặc
builder.Services.AddScoped<IOpenAiService, OpenAiService>();
```

**3. Test OpenAI API Key trực tiếp:**

```bash
# Thay YOUR_API_KEY với key thực tế
curl https://api.openai.com/v1/models \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -H "User-Agent: AIMS/1.0"

# Kết quả mong muốn: Danh sách models
```

**4. Kiểm tra logs:**

```bash
# Chạy với verbose logging
cd src/AIMS.BackendServer
dotnet run -- --environment Development

# Hoặc xem logs trong Scalar UI - thử gọi endpoint OpenAI
# Từ Scalar UI: POST /api/ai/generate
```

**5. Lỗi thường gặp:**

| Lỗi                                  | Nguyên nhân                 | Giải pháp                       |
| ------------------------------------ | --------------------------- | ------------------------------- |
| `401 Unauthorized`                   | API Key sai                 | Kiểm tra key trong User Secrets |
| `429 Too Many Requests`              | Rate limit exceeded         | Đợi một chút rồi thử lại        |
| `502 Bad Gateway`                    | OpenAI server down          | Kiểm tra status.openai.com      |
| `HttpRequestException`               | Network error               | Kiểm tra internet connection    |
| `No service for type IOpenAiService` | Service không được register | Thêm vào Program.cs             |

**6. Cách fix cơ bản:**

```bash
# 1. Xóa cache
rm -rf src/AIMS.BackendServer/bin
rm -rf src/AIMS.BackendServer/obj

# 2. Lấy OpenAI key mới từ platform.openai.com
# (Login -> API keys -> Create new secret key)

# 3. Update User Secrets
cd src/AIMS.BackendServer
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-new-key-here"

# 4. Rebuild
dotnet restore
dotnet build

# 5. Chạy lại
dotnet run
```

**7. Kiểm tra endpoint OpenAI:**

Từ Swagger hoặc Scalar UI, tìm endpoint liên quan OpenAI (ví dụ):

- `POST /api/ai/generate` - Tạo text từ OpenAI
- `POST /api/ai/screening` - Screening ứng viên bằng AI
- Etc.

Gọi và kiểm tra response:

```bash
# Ví dụ
curl -X POST http://localhost:5000/api/ai/generate \
  -H "Content-Type: application/json" \
  -d '{"prompt":"Tell me about AI"}' \
  -H "Authorization: Bearer your-jwt-token"
```

---

## 📞 Thông tin liên hệ & Hỗ trợ

| Vấn đề              | Giải pháp                                                                 |
| ------------------- | ------------------------------------------------------------------------- |
| Scalar UI 404       | Kiểm tra Program.cs có `app.MapScalarApiReference()`                      |
| OpenAI API lỗi      | Kiểm tra API Key, test với curl, xem logs                                 |
| Database Connection | Kiểm tra SQL Server, connection string trong appsettings.json             |
| Port Conflicts      | Kiểm tra port đã mở, thay đổi port trong appsettings hoặc launch settings |
| NuGet Restore       | Xóa obj/bin, chạy `dotnet restore` lại                                    |
| Migration Issues    | Xóa migrations gần đây, tạo lại bằng `dotnet ef migrations add`           |
| Docker Issues       | Xóa container/images cũ: `docker system prune -a`                         |

---

## ✅ Checklist trước khi deploy

- [ ] .NET SDK 10.0+ đã cài
- [ ] SQL Server đang chạy (Docker)
- [ ] Database AIMS đã tạo
- [ ] BackendServer API chạy trên port 5000/5001
- [ ] WebPortal chạy trên port 5010/5011
- [ ] Unit tests pass
- [ ] appsettings.json cấu hình đúng
- [ ] Connection string chính xác
- [ ] JWT secret keys đã set
- [ ] Scalar UI hiển thị (`http://localhost:5000/scalar/v1`)
- [ ] Swagger UI hiển thị (`http://localhost:5000/swagger`)
- [ ] OpenAI API Key cấu hình đúng (trong User Secrets)
- [ ] Health Check endpoint hoạt động (`http://localhost:5000/health`)
- [ ] OpenAI endpoints hoạt động (test từ Scalar UI)

---

## 🎉 Hoàn thành!

Nếu bạn thấy Scalar UI, Swagger UI, Web Portal chạy mà không có lỗi, bạn đã cài đặt thành công!

**Bước tiếp theo:**

1. Mở `http://localhost:5000/scalar/v1` để xem giao diện Scalar (Modern)
2. Mở `http://localhost:5000/swagger` để xem API documentation (Swagger)
3. Mở `http://localhost:5010` để xem giao diện Web Portal
4. Test OpenAI integration từ Scalar UI hoặc Swagger
5. Đăng nhập với tài khoản mặc định (nếu có)
6. Bắt đầu phát triển!

**API endpoints quan trọng:**

- `GET /health` - Kiểm tra health
- `POST /api/auth/login` - Đăng nhập
- `GET /api/ai/*` - OpenAI integration endpoints
- Xem đầy đủ từ Scalar UI

---

**Phiên bản hướng dẫn**: v1.0  
**Cập nhật lần cuối**: Tháng 5, 2026  
**Hỗ trợ cho**: .NET 10.0, SQL Server 2022, macOS/Windows/Linux
