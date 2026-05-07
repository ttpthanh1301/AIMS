# ⚡ QUICK START - AIMS Project

Bắt đầu nhanh chóng trong 5 phút!

---

## 📋 Yêu cầu tối thiểu

- ✅ .NET SDK 10.0+
- ✅ Docker (cho SQL Server)
- ✅ Git

---

## 🚀 Khởi động nhanh

### 1. Clone dự án

```bash
git clone <repository-url> AIMS
cd AIMS
```

### 2. Khởi động SQL Server (Docker)

```bash
docker-compose -f docker-compose.build.yml up -d sqlserver
sleep 45  # Đợi SQL Server khởi động
```

### 3. Khôi phục packages và migrations

```bash
dotnet restore
dotnet ef database update --project src/AIMS.BackendServer
```

### 4. Chạy Backend API

```bash
cd src/AIMS.BackendServer
# Cài đặt OpenAI Key (nếu chưa có)
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-key-here"

dotnet run
# 👉 Scalar UI: http://localhost:5000/scalar/v1
# 👉 Swagger UI: http://localhost:5000/swagger
# 👉 Health: http://localhost:5000/health
```

### 5. Chạy Web Portal (Terminal mới)

```bash
cd src/AIMS.WebPortal
dotnet run
# 👉 Truy cập: http://localhost:5010
```

---

## ✅ Kiểm tra

| URL                               | Mục đích                      |
| --------------------------------- | ----------------------------- |
| `http://localhost:5000/scalar/v1` | 🔍 Scalar UI (Modern API Doc) |
| `http://localhost:5000/swagger`   | 📖 Swagger UI (Traditional)   |
| `http://localhost:5000/health`    | ❤️ Health Check               |
| `http://localhost:5010`           | 🌐 Web Portal                 |
| `localhost:1433`                  | 🗄️ SQL Server (sa/AIMS@2025!) |

---

## 🆘 Lỗi thường gặp

### SQL Server không kết nối

```bash
docker logs aims-sqlserver
docker-compose -f docker-compose.build.yml up -d sqlserver
```

### Build lỗi duplicate imports

```bash
find . -type d -name obj -exec rm -rf {} + 2>/dev/null
find . -type d -name bin -exec rm -rf {} + 2>/dev/null
dotnet restore && dotnet build
```

### Scalar UI không hiển thị

```bash
# Kiểm tra Program.cs có app.MapScalarApiReference()
# Nếu không có, thêm vào rồi rebuild
dotnet clean && dotnet build && dotnet run
```

### OpenAI API lỗi 401

```bash
# Cập nhật OpenAI API Key
cd src/AIMS.BackendServer
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-new-key"

# Kiểm tra lại
dotnet user-secrets list
```

### Entity Framework tools

```bash
dotnet tool install --global dotnet-ef
```

---

**📚 Hướng dẫn chi tiết**: Xem [SETUP_GUIDE.md](SETUP_GUIDE.md)
