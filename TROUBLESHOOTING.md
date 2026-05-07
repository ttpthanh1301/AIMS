# 🛠️ HƯỚNG DẪN KHẮC PHỤC SỰ CỐ - AIMS Project

Giải pháp cho các lỗi phổ biến khi cài đặt và chạy AIMS.

---

## 📌 Mục lục

1. [Lỗi cài đặt .NET SDK](#lỗi-cài-đặt-net-sdk)
2. [Lỗi SQL Server & Database](#lỗi-sql-server--database)
3. [Lỗi Build & Dependencies](#lỗi-build--dependencies)
4. [Lỗi Runtime](#lỗi-runtime)
   - [Scalar UI 404](#scalar-ui-404-not-found-hoặc-không-hiển-thị)
   - [OpenAI API lỗi](#openai-api-401-unauthorized-hoặc-invalid-api-key)
5. [Lỗi Docker](#lỗi-docker)
6. [Lỗi Port & Network](#lỗi-port--network)
7. [Performance & Debug](#performance--debug)

---

## Lỗi cài đặt .NET SDK

### ❌ "dotnet: command not found"

**Nguyên nhân**: .NET SDK chưa được cài đặt hoặc PATH không đúng

**Giải pháp cho macOS:**

```bash
# Cài đặt qua Homebrew
brew install dotnet-sdk

# Hoặc thêm PATH manually
export PATH=$PATH:~/.dotnet

# Kiểm tra
dotnet --version
```

**Giải pháp cho Windows (PowerShell):**

```powershell
# Cài đặt qua Chocolatey
choco install dotnet-sdk

# Hoặc tải từ
# https://dotnet.microsoft.com/download
```

**Giải pháp cho Linux (Ubuntu):**

```bash
sudo apt-get update
sudo apt-get install dotnet-sdk-10.0

# Hoặc
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version latest
```

---

### ❌ "The current .NET SDK does not support targeting .NET 10.0"

**Nguyên nhân**: Phiên bản .NET SDK cài đặt quá cũ

**Giải pháp:**

```bash
# Kiểm tra phiên bản hiện tại
dotnet --version

# Cấu hình để sử dụng phiên bản mới nhất
dotnet --list-sdks

# Cài đặt .NET 10.0
# Trên macOS
brew install dotnet-sdk@10

# Trên Windows/Linux
# Tải từ https://dotnet.microsoft.com/download
```

---

## Lỗi SQL Server & Database

### ❌ "Cannot open database connection / Login failed"

**Nguyên nhân**: SQL Server không chạy hoặc credentials sai

**Giải pháp:**

```bash
# 1. Kiểm tra SQL Server chạy
docker ps | grep sqlserver

# 2. Nếu không chạy, khởi động
docker-compose -f docker-compose.build.yml up -d sqlserver

# 3. Đợi SQL Server hoàn toàn khởi động
sleep 45

# 4. Kiểm tra logs
docker logs aims-sqlserver

# 5. Kiểm tra connection (cài sqlcmd)
# macOS: brew install mssql-tools18
# Windows: SQL Server Management Studio

sqlcmd -S localhost,1433 -U sa -P AIMS@2025!
```

**Kiểm tra connection string:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AIMS;User Id=sa;Password=AIMS@2025!;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true"
  }
}
```

---

### ❌ "Database 'AIMS' does not exist"

**Nguyên nhân**: Database chưa được tạo

**Giải pháp:**

```bash
# Chạy Entity Framework migrations
dotnet ef database update --project src/AIMS.BackendServer

# Hoặc manual (từ thư mục BackendServer)
cd src/AIMS.BackendServer
dotnet ef database update
cd ../..
```

**Nếu vẫn lỗi:**

```bash
# 1. Tạo database manually
sqlcmd -S localhost,1433 -U sa -P AIMS@2025! -Q "CREATE DATABASE AIMS"

# 2. Chạy lại migrations
dotnet ef database update --project src/AIMS.BackendServer
```

---

### ❌ "Timeout expired. The login timeout period expired."

**Nguyên nhân**: SQL Server chưa sẵn sàng khi migration chạy

**Giải pháp:**

```bash
# Đợi SQL Server hoàn toàn khởi động
docker logs aims-sqlserver
# Chờ tới khi thấy: "SQL Server is now ready for client connections"

sleep 60  # Đợi 60 giây

# Thử lại migration
dotnet ef database update --project src/AIMS.BackendServer
```

---

## Lỗi Build & Dependencies

### ❌ "The requested package does not provide a dll or exe for entry point"

**Nguyên nhân**: NuGet packages không khớp hoặc bị hỏng

**Giải pháp:**

```bash
cd /path/to/AIMS

# 1. Xóa tất cả cache
find . -type d -name obj -exec rm -rf {} + 2>/dev/null
find . -type d -name bin -exec rm -rf {} + 2>/dev/null

# 2. Xóa NuGet cache
dotnet nuget locals all --clear

# 3. Khôi phục lại
dotnet restore

# 4. Build
dotnet build
```

---

### ❌ "Duplicate NuGet imports" (Warning MSB4011)

**Nguyên nhân**: File .nuget.g.props bị trùng lặp

**Giải pháp:**

```bash
cd /path/to/AIMS

# 1. Xóa obj/bin
rm -rf **/obj/*
rm -rf **/bin/*

# 2. Hoặc xóa toàn bộ
find . -type d -name obj -exec rm -rf {} + 2>/dev/null
find . -type d -name bin -exec rm -rf {} + 2>/dev/null

# 3. Restore & Build
dotnet restore
dotnet build
```

---

### ❌ "CsvHelper version conflict: 33.1.0 vs 2.11.0"

**Nguyên nhân**: Unit tests và BackendServer dùng phiên bản khác nhau

**Giải pháp:**

```bash
# Cập nhật file csproj test project
# src/AIMS.BackendServer.UnitTests/AIMS.BackendServer.UnitTests.csproj

# Thay đổi:
# <PackageReference Include="CsvHelper" Version="2.11.0" />
# Thành:
# <PackageReference Include="CsvHelper" Version="33.1.0" />

# Hoặc dùng command:
dotnet add test/AIMS.BackendServer.UnitTests package CsvHelper --version 33.1.0

# Restore lại
dotnet restore
```

---

### ❌ "The dependency resolved to Microsoft.EntityFrameworkCore, but it was not found"

**Nguyên nhân**: EF Core tools không được cài

**Giải pháp:**

```bash
# Cài Entity Framework Core tools globally
dotnet tool install --global dotnet-ef --version 10.0.3

# Hoặc update
dotnet tool update --global dotnet-ef

# Kiểm tra
dotnet ef --version
```

---

## Lỗi Runtime

### ❌ "Null reference exception" hoặc "Object reference not set"

**Giải pháp:**

```bash
# 1. Kiểm tra appsettings.json cấu hình đúng
cat src/AIMS.BackendServer/appsettings.json

# 2. Kiểm tra environment variables
dotnet run -- --environment Development

# 3. Enable detailed logging
# Trong appsettings.json:
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

---

### ❌ "Unable to resolve service for type X while attempting to activate Y"

**Nguyên nhân**: Dependency injection không được register

**Giải pháp:**

1. Kiểm tra `Program.cs` - các service có được register?
2. Kiểm tra tên namespace
3. Rebuild project:

```bash
dotnet clean
dotnet build
```

---

### ❌ "The specified key was not found in the database"

**Nguyên nhân**: Entity không tồn tại trong database

**Giải pháp:**

```bash
# 1. Kiểm tra database và dữ liệu
sqlcmd -S localhost,1433 -U sa -P AIMS@2025! -d AIMS -Q "SELECT * FROM [TableName]"

# 2. Seed data nếu cần
# Chạy SeedData trong Program.cs hoặc Data/SeedData

# 3. Check migrations
dotnet ef migrations list --project src/AIMS.BackendServer
```

---

### ❌ Scalar UI "404 Not Found" hoặc không hiển thị

**Nguyên nhân**: Scalar middleware chưa được cấu hình hoặc sai thứ tự trong Program.cs

**Giải pháp:**

````bash
# 1. Kiểm tra Scalar.AspNetCore package đã cài
dotnet list package | grep -i scalar
# Nếu không có:
dotnet add src/AIMS.BackendServer package Scalar.AspNetCore

# 2. Kiểm tra Program.cs - phải có:
# - app.MapOpenApi()
# - app.MapScalarApiReference()

# Ví dụ Program.cs:
```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);

// Add services
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // ← QUAN TRỌNG
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();
````

# 3. Nếu vẫn 404, rebuild:

```bash
dotnet clean
dotnet build
dotnet run
```

# 4. Kiểm tra URL đúng:

# - Scalar: http://localhost:5000/scalar/v1

# - Swagger: http://localhost:5000/swagger

# - OpenAPI: http://localhost:5000/openapi/v1.json

````

---

### ❌ OpenAI API "401 Unauthorized" hoặc "Invalid API Key"

**Nguyên nhân**: OpenAI API Key sai, expired, hoặc không được cấu hình

**Giải pháp:**

```bash
# 1. Kiểm tra OpenAI package đã cài
dotnet list package | grep -i openai

# 2. Kiểm tra User Secrets được cấu hình
cd src/AIMS.BackendServer
dotnet user-secrets list
# Nếu không có OpenAI:ApiKey, thêm:
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-api-key"

# 3. Test API Key trực tiếp
curl https://api.openai.com/v1/models \
  -H "Authorization: Bearer sk-your-api-key" \
  -H "User-Agent: AIMS/1.0"

# Nếu lỗi, API Key sai hoặc expired - lấy key mới từ platform.openai.com

# 4. Kiểm tra Program.cs - phải có:
```csharp
// Add OpenAI Configuration
var openAiSettings = builder.Configuration.GetSection("OpenAI");
builder.Services.Configure<OpenAiSettings>(openAiSettings);

// Hoặc register service
builder.Services.AddScoped<IOpenAiService, OpenAiService>();
````

# 5. Kiểm trap logs - thêm logging:

```csharp
// Trong appsettings.Development.json:
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "YourNamespace.OpenAi": "Debug"
    }
  }
}
```

# 6. Test OpenAI endpoint từ Scalar UI:

# - POST /api/ai/generate (hoặc endpoint nào đó)

# - Xem response và error message

````

---

### ❌ OpenAI "429 Too Many Requests" hoặc "Rate Limit Exceeded"

**Nguyên nhân**: Vượt quá rate limit của OpenAI API

**Giải pháp:**
```bash
# 1. Đợi một chút rồi thử lại (rate limit reset)
sleep 60

# 2. Kiểm tra plan OpenAI (Free, Paid, etc.)
# https://platform.openai.com/account/billing/overview

# 3. Giảm số requests hoặc thêm delay
# Nếu code cho phép, thêm:
System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));

# 4. Kiểm tra xem có background job nào dùng OpenAI quá nhiều
# Xem logs để tìm tần suất requests
````

---

### ❌ OpenAI "502 Bad Gateway" hoặc OpenAI server down

**Nguyên nhân**: OpenAI API server có vấn đề hoặc down

**Giải pháp:**

```bash
# 1. Kiểm tra status OpenAI
# https://status.openai.com

# 2. Đợi một chút
sleep 60

# 3. Thử lại

# 4. Nếu lâu, kiểm tra internet connection
ping api.openai.com
```

---

## Lỗi Docker

### ❌ "docker: command not found"

**Nguyên nhân**: Docker chưa cài hoặc PATH sai

**Giải pháp:**

```bash
# Cài Docker Desktop
# macOS: https://docs.docker.com/docker-for-mac/install/
# Windows: https://docs.docker.com/docker-for-windows/install/
# Linux: https://docs.docker.com/engine/install/

# Kiểm tra
docker --version
docker ps
```

---

### ❌ "permission denied while trying to connect to Docker daemon"

**Nguyên nhân**: User không có quyền Docker

**Giải pháp (Linux):**

```bash
# Thêm user vào docker group
sudo usermod -aG docker $USER

# Activate changes
newgrp docker

# Hoặc restart Docker
sudo systemctl restart docker

# Kiểm tra
docker ps
```

---

### ❌ "no space left on device" hoặc Docker disk full

**Giải pháp:**

```bash
# Xóa unused images/containers
docker system prune -a

# Hoặc xóa volumes
docker volume prune

# Kiểm tra disk usage
docker system df
```

---

### ❌ SQL Server container không khởi động được

**Giải pháp:**

```bash
# 1. Xóa container cũ
docker stop aims-sqlserver
docker rm aims-sqlserver

# 2. Xóa volume (cảnh báo: mất dữ liệu)
docker volume rm aims_sqldata

# 3. Khởi động lại
docker-compose -f docker-compose.build.yml up -d sqlserver

# 4. Theo dõi logs
docker logs -f aims-sqlserver
```

---

### ❌ "ERROR: for sqlserver Cannot assign requested address"

**Nguyên nhân**: Port 1433 đã được dùng

**Giải pháp:**

```bash
# Kiểm tra port đang dùng
lsof -i :1433

# Kill process
kill -9 <PID>

# Hoặc thay đổi port trong docker-compose.yml
# ports:
#   - "1434:1433"  # Thay 1434 với port khác
```

---

## Lỗi Port & Network

### ❌ "Address already in use" (Port 5000/5001/5010)

**Giải pháp:**

```bash
# Tìm process dùng port
lsof -i :5000
lsof -i :5001
lsof -i :5010

# Kill process
kill -9 <PID>

# Hoặc chạy với port khác
dotnet run -- --urls "http://localhost:8080"
```

---

### ❌ "Unable to connect to http://localhost:5000"

**Giải pháp:**

```bash
# 1. Kiểm tra API chạy
ps aux | grep "dotnet run"

# 2. Kiểm trap logs
# Xem terminal nơi chạy BackendServer

# 3. Thử từ terminal
curl http://localhost:5000/health

# 4. Kiểm tra firewall
# macOS: System Preferences > Security & Privacy > Firewall
# Windows: Settings > Firewall
```

---

## Performance & Debug

### 🐢 Ứng dụng chạy chậm

**Giải pháp:**

```bash
# 1. Chạy với Release configuration
dotnet run -c Release

# 2. Kiểm tra database queries
# Enable SQL logging trong appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Debug"
    }
  }
}

# 3. Kiểm tra memory usage
# macOS/Linux
top
# Windows
tasklist
```

---

### 🔍 Debug mode

**Để debug trong Visual Studio Code:**

1. Cài extension: "C# Dev Kit" by Microsoft
2. Tạo `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/AIMS.BackendServer/bin/Debug/net10.0/AIMS.BackendServer.dll",
      "args": [],
      "stopAtEntry": false,
      "cwd": "${workspaceFolder}/src/AIMS.BackendServer",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

3. Nhấn F5 để start debug

---

### 🧪 Chạy Unit Tests

```bash
# Chạy tất cả tests
dotnet test

# Chạy tests chi tiết
dotnet test --verbosity detailed

# Chạy tests cụ thể
dotnet test --filter "ClassName.MethodName"

# Chạy với code coverage
dotnet test /p:CollectCoverage=true
```

---

## 📞 Khi nào cần reset hoàn toàn

Nếu tất cả giải pháp trên không giúp, thực hiện reset:

```bash
cd /path/to/AIMS

# 1. Dừng tất cả services
docker-compose -f docker-compose.build.yml down

# 2. Xóa volume (mất dữ liệu!)
docker volume rm aims_sqldata

# 3. Xóa tất cả build artifacts
find . -type d -name obj -exec rm -rf {} + 2>/dev/null
find . -type d -name bin -exec rm -rf {} + 2>/dev/null

# 4. Xóa file lock NuGet
rm -rf ~/.nuget/packages/

# 5. Khôi phục từ đầu
dotnet restore
docker-compose -f docker-compose.build.yml up -d sqlserver
sleep 45
dotnet ef database update --project src/AIMS.BackendServer

# 6. Chạy lại
dotnet run --project src/AIMS.BackendServer
```

---

## 🆘 Nếu vẫn không giải quyết được

1. **Kiểm tra logs chi tiết:**

   ```bash
   dotnet run --verbosity diagnostic
   ```

2. **Cài dotnet-diagnose:**

   ```bash
   dotnet tool install --global dotnet-diagnose
   dotnet-diagnose collect
   ```

3. **Tạo issue trên GitHub** với:
   - Output của `dotnet --info`
   - Output của `docker --version`
   - Full error message/logs
   - Các bước bạn đã thử

---

**Phiên bản**: v1.0  
**Cập nhật lần cuối**: Tháng 5, 2026
