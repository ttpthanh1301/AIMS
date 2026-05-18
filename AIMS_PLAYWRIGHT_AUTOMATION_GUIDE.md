# Playwright Automation Test Guide cho AIMS

> **Dành cho:** QA/Developer đã biết lập trình và muốn xây dựng bộ automation test cho dự án AIMS  
> **Ứng dụng thực hành:** AIMS - Hệ thống Quản lý Thực tập sinh Thông minh  
> **Stack dự án:** ASP.NET Core WebPortal + Backend API + SQL Server  
> **Công cụ test:** Playwright + JavaScript

---

## Mục lục

- [1. Tổng quan dự án AIMS](#1-tổng-quan-dự-án-aims)
- [2. Môi trường chạy test](#2-môi-trường-chạy-test)
- [3. Tài khoản seed dùng cho automation](#3-tài-khoản-seed-dùng-cho-automation)
- [4. Cấu trúc Playwright đề xuất](#4-cấu-trúc-playwright-đề-xuất)
- [5. Chiến lược test theo role](#5-chiến-lược-test-theo-role)
- [6. Test case nền tảng](#6-test-case-nền-tảng)
- [7. Locator strategy cho AIMS](#7-locator-strategy-cho-aims)
- [8. Page Object Model](#8-page-object-model)
- [9. Fixtures và authentication](#9-fixtures-và-authentication)
- [10. API testing](#10-api-testing)
- [11. Reporting, trace và artifact](#11-reporting-trace-và-artifact)
- [12. CI/CD](#12-cicd)
- [13. Traceability matrix](#13-traceability-matrix)
- [14. Lộ trình triển khai](#14-lộ-trình-triển-khai)

---

## 1. Tổng quan dự án AIMS

AIMS gồm 2 ứng dụng chính:

| Thành phần | Đường dẫn | Vai trò |
| --- | --- | --- |
| Backend API | `src/AIMS.BackendServer` | REST API, Identity, JWT, EF Core, SQL Server |
| Web Portal | `src/AIMS.WebPortal` | Giao diện Razor MVC, cookie auth, gọi Backend API |
| Unit/Integration Tests | `test/AIMS.BackendServer.UnitTests` | Test backend hiện có |

Các area chính trong Web Portal:

| Area | Role | Module chính |
| --- | --- | --- |
| Admin | Admin | Dashboard, Users, Roles, Internship Periods, Intern Assignments |
| HR | HR | Job Descriptions, Screening CV, Ranking, Application status |
| Mentor | Mentor | Dashboard, Tasks, Courses, Lessons, Quiz, Progress, Daily Reports, Timesheets |
| Intern | Intern | Task board, Task detail, LMS, Quiz, Timesheet, Daily Report, Apply |

Luồng đăng nhập nằm tại:

```text
/Account/Login
```

Sau khi đăng nhập, hệ thống redirect theo role:

| Role | Redirect kỳ vọng |
| --- | --- |
| Admin | `/Admin/Dashboard` |
| HR | `/HR/JobDescription` |
| Mentor | `/Mentor/Dashboard` |
| Intern | `/Intern/Task` |

---

## 2. Môi trường chạy test

### Yêu cầu

- .NET SDK 10.0+
- Docker
- Node.js LTS
- Playwright

### Chạy AIMS local bằng `dotnet run`

Terminal 1 - SQL Server:

```bash
docker-compose -f docker-compose.build.yml up -d sqlserver
```

Terminal 2 - Backend API:

```bash
cd src/AIMS.BackendServer
dotnet run
```

Backend mặc định theo `launchSettings.json`:

```text
http://localhost:5291
```

Terminal 3 - Web Portal:

```bash
cd src/AIMS.WebPortal
dotnet run
```

WebPortal mặc định theo `launchSettings.json`:

```text
http://localhost:5162
```

### Chạy bằng Docker Compose

```bash
docker-compose -f docker-compose.build.yml up -d --build
```

URL Docker:

| Service | URL |
| --- | --- |
| Web Portal | `http://localhost:5005` |
| Backend API | `http://localhost:5001` |
| SQL Server | `localhost:1433` |

### Khởi tạo Playwright project

Tạo folder automation ở root:

```bash
mkdir e2e
cd e2e
npm init playwright@latest
```

Chọn:

```text
JavaScript
tests
No GitHub Actions
Yes install browsers
```

---

## 3. Tài khoản seed dùng cho automation

Dữ liệu seed nằm trong:

```text
src/AIMS.BackendServer/Data/SeedData/DbInitializer.cs
```

Tài khoản mặc định:

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@deha.vn` | `Admin@2025!` |
| HR | `hr.minh@deha.vn` | `Hr@2025!` |
| Mentor | `hoang@deha.vn` | `Mentor@2025!` |
| Intern | `thanh@sv.vn` | `Intern@2025!` |

Tạo file `.env` trong folder `e2e`:

```env
AIMS_WEB_URL=http://localhost:5162
AIMS_API_URL=http://localhost:5291

AIMS_ADMIN_EMAIL=admin@deha.vn
AIMS_ADMIN_PASSWORD=Admin@2025!

AIMS_HR_EMAIL=hr.minh@deha.vn
AIMS_HR_PASSWORD=Hr@2025!

AIMS_MENTOR_EMAIL=hoang@deha.vn
AIMS_MENTOR_PASSWORD=Mentor@2025!

AIMS_INTERN_EMAIL=thanh@sv.vn
AIMS_INTERN_PASSWORD=Intern@2025!
```

Không commit `.env`.

---

## 4. Cấu trúc Playwright đề xuất

```text
e2e/
├── fixtures/
│   └── aimsFixtures.js
├── pages/
│   ├── LoginPage.js
│   ├── AdminDashboardPage.js
│   ├── AdminUsersPage.js
│   ├── HrJobDescriptionPage.js
│   ├── HrScreeningPage.js
│   ├── MentorTaskPage.js
│   ├── MentorCoursePage.js
│   ├── InternTaskPage.js
│   ├── InternLmsPage.js
│   └── InternTimesheetPage.js
├── services/
│   ├── AuthApi.js
│   ├── UsersApi.js
│   ├── JobDescriptionsApi.js
│   ├── TasksApi.js
│   └── CoursesApi.js
├── test-data/
│   ├── users.json
│   ├── roles.json
│   ├── job-descriptions.json
│   └── test-cases/
│       ├── TC-AUTH.md
│       ├── TC-ADMIN.md
│       ├── TC-HR.md
│       ├── TC-MENTOR.md
│       ├── TC-INTERN.md
│       └── traceability-matrix.md
├── tests/
│   ├── auth.spec.js
│   ├── admin-users.spec.js
│   ├── admin-roles.spec.js
│   ├── hr-job-description.spec.js
│   ├── hr-screening.spec.js
│   ├── mentor-task.spec.js
│   ├── mentor-course.spec.js
│   ├── intern-task.spec.js
│   ├── intern-lms.spec.js
│   ├── intern-timesheet.spec.js
│   └── api.spec.js
├── utils/
│   └── env.js
├── playwright.config.js
└── package.json
```

### `playwright.config.js`

```javascript
const { defineConfig, devices } = require("@playwright/test");
require("dotenv").config();

module.exports = defineConfig({
  testDir: "./tests",
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  timeout: 60_000,
  expect: {
    timeout: 10_000,
  },
  reporter: [
    ["list"],
    ["html", { outputFolder: "playwright-report", open: "never" }],
  ],
  use: {
    baseURL: process.env.AIMS_WEB_URL || "http://localhost:5162",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
    trace: "on-first-retry",
    headless: true,
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
```

---

## 5. Chiến lược test theo role

### Smoke test bắt buộc

| Test ID | Mục tiêu | Role |
| --- | --- | --- |
| TC-AUTH-001 | Admin login và redirect đúng dashboard | Admin |
| TC-AUTH-002 | HR login và redirect đúng màn Job Description | HR |
| TC-AUTH-003 | Mentor login và redirect đúng dashboard | Mentor |
| TC-AUTH-004 | Intern login và redirect đúng Task board | Intern |
| TC-AUTH-005 | Sai mật khẩu hiển thị lỗi | Any |
| TC-AUTH-006 | Logout xong quay về Login | Any |

### Regression theo module

| Module | Nên ưu tiên |
| --- | --- |
| Admin Users | Tạo user, lọc user, sửa role, deactivate |
| Admin Roles | Tạo role, sửa role, xóa role không dùng |
| Internship Period | Tạo kỳ thực tập, activate, close |
| HR Job Description | Tạo JD, sửa JD, xóa JD |
| HR Screening | Upload CV, chạy screening, xem ranking, đổi status application |
| Mentor Task | Tạo task, sửa task, đổi trạng thái Kanban, xóa task |
| Mentor Course | Tạo course, add chapter, add lesson, publish course |
| Mentor Quiz | Tạo quiz, thêm câu hỏi, sửa quiz, xóa quiz |
| Intern Task | Xem danh sách task, mở chi tiết, cập nhật trạng thái |
| Intern LMS | Enroll course, học lesson, complete lesson |
| Intern Quiz | Start quiz, submit quiz, xem result |
| Intern Timesheet | Log time, validate giờ, xóa entry nếu có |
| Daily Report | Intern gửi report, Mentor feedback |

---

## 6. Test case nền tảng

### TC-AUTH-001 - Admin login thành công

```text
Feature      : Authentication
Priority     : High
Type         : Positive
Precondition : User admin@deha.vn đã được seed

Steps:
  1. Mở /Account/Login
  2. Nhập Email: admin@deha.vn
  3. Nhập Mật khẩu: Admin@2025!
  4. Click Đăng nhập

Expected Result:
  - Redirect đến /Admin/Dashboard
  - Sidebar/menu Admin hiển thị
  - Không còn ở trang /Account/Login
```

### TC-AUTH-005 - Login sai mật khẩu

```text
Feature      : Authentication
Priority     : High
Type         : Negative

Steps:
  1. Mở /Account/Login
  2. Nhập Email hợp lệ
  3. Nhập mật khẩu sai
  4. Click Đăng nhập

Expected Result:
  - Vẫn ở /Account/Login
  - Hiển thị lỗi "Email hoặc mật khẩu không đúng."
```

### TC-ADMIN-USER-001 - Tạo user mới

```text
Feature      : Admin / Users
Priority     : High
Type         : Positive
Precondition : Đã login Admin

Steps:
  1. Mở /Admin/User/Create
  2. Nhập thông tin user hợp lệ
  3. Chọn role
  4. Submit form

Expected Result:
  - Redirect về danh sách user
  - User mới xuất hiện trong danh sách
  - API /api/users trả về user mới nếu kiểm tra qua API
```

### TC-HR-JD-001 - Tạo Job Description

```text
Feature      : HR / Job Description
Priority     : High
Type         : Positive
Precondition : Đã login HR

Steps:
  1. Mở /HR/JobDescription/Create
  2. Nhập tiêu đề, mô tả, yêu cầu, vị trí
  3. Submit form

Expected Result:
  - Redirect về /HR/JobDescription
  - JD mới hiển thị trong danh sách
```

### TC-HR-SCREEN-001 - Upload CV cho JD

```text
Feature      : HR / Screening
Priority     : High
Type         : Positive
Precondition : Đã có Job Description

Steps:
  1. Mở /HR/Screening
  2. Chọn JD
  3. Upload file CV PDF
  4. Submit upload

Expected Result:
  - Upload thành công
  - Application mới xuất hiện
  - Có thể chạy screening hoặc xem chi tiết CV
```

### TC-MENTOR-TASK-001 - Tạo task cho intern

```text
Feature      : Mentor / Task
Priority     : High
Type         : Positive
Precondition : Đã login Mentor, đã có intern được assign

Steps:
  1. Mở /Mentor/Task/CreateTask
  2. Nhập title, description, deadline, assignee
  3. Submit form

Expected Result:
  - Redirect về task board/list
  - Task mới hiển thị
  - Intern được assign có thể thấy task
```

### TC-INTERN-TASK-001 - Intern xem task detail

```text
Feature      : Intern / Task
Priority     : High
Type         : Positive
Precondition : Intern đã có task được assign

Steps:
  1. Login Intern
  2. Mở /Intern/Task
  3. Click một task trong danh sách

Expected Result:
  - Mở trang /Intern/Task/Details/{id}
  - Hiển thị title, description, status, deadline
```

### TC-INTERN-TIME-001 - Intern log timesheet

```text
Feature      : Intern / Timesheet
Priority     : Medium
Type         : Positive
Precondition : Đã login Intern

Steps:
  1. Mở /Intern/Timesheet
  2. Chọn ngày
  3. Nhập số giờ hợp lệ
  4. Nhập mô tả công việc
  5. Submit

Expected Result:
  - Timesheet entry được tạo
  - Entry hiển thị trong danh sách
```

---

## 7. Locator strategy cho AIMS

Ưu tiên locator theo thứ tự:

```text
1. getByRole()
2. getByLabel()
3. getByPlaceholder()
4. getByText()
5. getByTestId()
6. CSS locator ngắn, ổn định
```

Ví dụ cho trang login hiện tại:

```javascript
await page.goto("/Account/Login");
await page.getByLabel("Email").fill("admin@deha.vn");
await page.getByLabel("Mật khẩu").fill("Admin@2025!");
await page.getByRole("button", { name: /Đăng nhập/i }).click();
```

Nếu Razor view chưa có label liên kết chuẩn với input, có thể dùng name/id:

```javascript
await page.locator('input[name="Email"]').fill("admin@deha.vn");
await page.locator('input[name="Password"]').fill("Admin@2025!");
```

Khuyến nghị thêm `data-testid` cho các màn quan trọng:

```html
<button data-testid="login-submit" type="submit">Đăng nhập</button>
```

Trong test:

```javascript
await page.getByTestId("login-submit").click();
```

Quy ước test id:

```text
auth-login-email
auth-login-password
auth-login-submit
admin-user-create-button
admin-user-role-select
hr-jd-create-submit
mentor-task-status-column
intern-timesheet-submit
```

---

## 8. Page Object Model

### `pages/LoginPage.js`

```javascript
exports.LoginPage = class LoginPage {
  constructor(page) {
    this.page = page;
    this.emailInput = page.locator('input[name="Email"]');
    this.passwordInput = page.locator('input[name="Password"]');
    this.submitButton = page.getByRole("button", { name: /Đăng nhập/i });
    this.errorAlert = page.locator(".alert-danger");
  }

  async goto() {
    await this.page.goto("/Account/Login");
  }

  async login(email, password) {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.submitButton.click();
  }
};
```

### `pages/AdminUsersPage.js`

```javascript
exports.AdminUsersPage = class AdminUsersPage {
  constructor(page) {
    this.page = page;
    this.createButton = page.getByRole("link", { name: /Tạo|Thêm|Create/i });
  }

  async goto() {
    await this.page.goto("/Admin/User");
  }

  async gotoCreate() {
    await this.page.goto("/Admin/User/Create");
  }

  async searchByEmail(email) {
    const searchInput = this.page.locator('input[name="Keyword"], input[type="search"]').first();
    await searchInput.fill(email);
    await searchInput.press("Enter");
  }
};
```

### `tests/auth.spec.js`

```javascript
const { test, expect } = require("@playwright/test");
const { LoginPage } = require("../pages/LoginPage");

test.describe("AIMS - Authentication", () => {
  test("[TC-AUTH-001] Admin login redirects to Admin dashboard", async ({ page }) => {
    const loginPage = new LoginPage(page);

    await loginPage.goto();
    await loginPage.login(process.env.AIMS_ADMIN_EMAIL, process.env.AIMS_ADMIN_PASSWORD);

    await expect(page).toHaveURL(/\/Admin\/Dashboard/i);
  });

  test("[TC-AUTH-005] Invalid password shows error", async ({ page }) => {
    const loginPage = new LoginPage(page);

    await loginPage.goto();
    await loginPage.login(process.env.AIMS_ADMIN_EMAIL, "WrongPassword123");

    await expect(page).toHaveURL(/\/Account\/Login/i);
    await expect(loginPage.errorAlert).toContainText("Email hoặc mật khẩu không đúng.");
  });
});
```

---

## 9. Fixtures và authentication

### `fixtures/aimsFixtures.js`

```javascript
const { test: base, expect } = require("@playwright/test");
const { LoginPage } = require("../pages/LoginPage");

async function loginAs(page, email, password, expectedUrl) {
  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await loginPage.login(email, password);
  await expect(page).toHaveURL(expectedUrl);
  return page;
}

exports.test = base.extend({
  adminPage: async ({ page }, use) => {
    await loginAs(
      page,
      process.env.AIMS_ADMIN_EMAIL,
      process.env.AIMS_ADMIN_PASSWORD,
      /\/Admin\/Dashboard/i,
    );
    await use(page);
  },

  hrPage: async ({ page }, use) => {
    await loginAs(
      page,
      process.env.AIMS_HR_EMAIL,
      process.env.AIMS_HR_PASSWORD,
      /\/HR\/JobDescription/i,
    );
    await use(page);
  },

  mentorPage: async ({ page }, use) => {
    await loginAs(
      page,
      process.env.AIMS_MENTOR_EMAIL,
      process.env.AIMS_MENTOR_PASSWORD,
      /\/Mentor\/Dashboard/i,
    );
    await use(page);
  },

  internPage: async ({ page }, use) => {
    await loginAs(
      page,
      process.env.AIMS_INTERN_EMAIL,
      process.env.AIMS_INTERN_PASSWORD,
      /\/Intern\/Task/i,
    );
    await use(page);
  },
});

exports.expect = expect;
```

### Ví dụ dùng fixture

```javascript
const { test, expect } = require("../fixtures/aimsFixtures");

test("[TC-INTERN-TASK-001] Intern opens task board", async ({ internPage }) => {
  await internPage.goto("/Intern/Task");
  await expect(internPage).toHaveURL(/\/Intern\/Task/i);
});
```

---

## 10. API testing

Backend API dùng JWT. Login endpoint:

```text
POST /api/auth/login
```

### `services/AuthApi.js`

```javascript
exports.AuthApi = class AuthApi {
  constructor(request) {
    this.request = request;
    this.apiURL = process.env.AIMS_API_URL || "http://localhost:5291";
  }

  async login(email, password) {
    const response = await this.request.post(`${this.apiURL}/api/auth/login`, {
      data: { email, password },
    });

    if (!response.ok()) {
      throw new Error(`Login failed: ${response.status()} ${await response.text()}`);
    }

    return response.json();
  }
};
```

### API smoke test

```javascript
const { test, expect } = require("@playwright/test");
const { AuthApi } = require("../services/AuthApi");

test("[TC-API-AUTH-001] Login API returns JWT", async ({ request }) => {
  const authApi = new AuthApi(request);
  const auth = await authApi.login(
    process.env.AIMS_ADMIN_EMAIL,
    process.env.AIMS_ADMIN_PASSWORD,
  );

  expect(auth.accessToken).toBeTruthy();
  expect(auth.email).toBe(process.env.AIMS_ADMIN_EMAIL);
  expect(auth.roles).toContain("Admin");
});
```

### API checklist đề xuất

| Test ID | Endpoint | Expected |
| --- | --- | --- |
| TC-API-AUTH-001 | `POST /api/auth/login` | 200, có `accessToken` |
| TC-API-AUTH-002 | `GET /api/auth/me` | 200 khi có Bearer token |
| TC-API-USER-001 | `GET /api/users` | Admin lấy được danh sách user |
| TC-API-USER-002 | `POST /api/users` | Admin tạo user mới |
| TC-API-JD-001 | `POST /api/jobdescriptions` | HR tạo JD |
| TC-API-TASK-001 | `POST /api/tasks` | Mentor tạo task |
| TC-API-COURSE-001 | `POST /api/courses` | Mentor tạo course |
| TC-API-TIME-001 | `POST /api/timesheets` | Intern log time |

---

## 11. Reporting, trace và artifact

Chạy toàn bộ test:

```bash
cd e2e
npx playwright test
```

Chạy theo role:

```bash
npx playwright test -g "TC-AUTH"
npx playwright test -g "TC-ADMIN"
npx playwright test -g "TC-HR"
npx playwright test -g "TC-MENTOR"
npx playwright test -g "TC-INTERN"
```

Debug:

```bash
npx playwright test tests/auth.spec.js --debug
```

Xem report:

```bash
npx playwright show-report
```

Khi test fail, cần kiểm tra:

| Artifact | Dùng để |
| --- | --- |
| Screenshot | Xem UI tại thời điểm fail |
| Video | Xem flow thao tác |
| Trace | Debug từng action, locator, network request |
| Network tab | Xem Backend API trả lỗi gì |

---

## 12. CI/CD

### GitHub Actions mẫu

```yaml
name: AIMS Playwright E2E

on:
  pull_request:
  push:
    branches: [main]

jobs:
  e2e:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-node@v4
        with:
          node-version: 22

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"

      - name: Start SQL Server
        run: docker-compose -f docker-compose.build.yml up -d sqlserver

      - name: Restore .NET
        run: dotnet restore

      - name: Build .NET
        run: dotnet build --no-restore

      - name: Install Playwright
        working-directory: e2e
        run: |
          npm ci
          npx playwright install --with-deps chromium

      - name: Run Playwright
        working-directory: e2e
        env:
          AIMS_WEB_URL: http://localhost:5162
          AIMS_API_URL: http://localhost:5291
          AIMS_ADMIN_EMAIL: admin@deha.vn
          AIMS_ADMIN_PASSWORD: Admin@2025!
          AIMS_HR_EMAIL: hr.minh@deha.vn
          AIMS_HR_PASSWORD: Hr@2025!
          AIMS_MENTOR_EMAIL: hoang@deha.vn
          AIMS_MENTOR_PASSWORD: Mentor@2025!
          AIMS_INTERN_EMAIL: thanh@sv.vn
          AIMS_INTERN_PASSWORD: Intern@2025!
        run: npx playwright test --project=chromium

      - name: Upload report
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: playwright-report
          path: e2e/playwright-report/
```

Lưu ý: workflow trên cần bổ sung bước start Backend và WebPortal ở background. Nếu dùng Docker image `aims-api` và `aims-web`, có thể chạy `docker-compose -f docker-compose.build.yml up -d --build` rồi trỏ `AIMS_WEB_URL=http://localhost:5005`, `AIMS_API_URL=http://localhost:5001`.

---

## 13. Traceability matrix

Tạo file:

```text
e2e/test-data/test-cases/traceability-matrix.md
```

Mẫu:

| Feature | Test Case ID | Mô tả | Automation File | Role | Status |
| --- | --- | --- | --- | --- | --- |
| Auth | TC-AUTH-001 | Admin login redirect dashboard | `auth.spec.js` | Admin | Todo |
| Auth | TC-AUTH-002 | HR login redirect Job Description | `auth.spec.js` | HR | Todo |
| Auth | TC-AUTH-003 | Mentor login redirect dashboard | `auth.spec.js` | Mentor | Todo |
| Auth | TC-AUTH-004 | Intern login redirect Task board | `auth.spec.js` | Intern | Todo |
| Auth | TC-AUTH-005 | Login sai mật khẩu hiển thị lỗi | `auth.spec.js` | Any | Todo |
| Admin Users | TC-ADMIN-USER-001 | Tạo user mới | `admin-users.spec.js` | Admin | Todo |
| Admin Roles | TC-ADMIN-ROLE-001 | Tạo role mới | `admin-roles.spec.js` | Admin | Todo |
| Internship Period | TC-ADMIN-PERIOD-001 | Tạo kỳ thực tập | `admin-period.spec.js` | Admin | Todo |
| HR JD | TC-HR-JD-001 | Tạo Job Description | `hr-job-description.spec.js` | HR | Todo |
| HR Screening | TC-HR-SCREEN-001 | Upload CV | `hr-screening.spec.js` | HR | Todo |
| HR Screening | TC-HR-SCREEN-002 | Chạy batch screening | `hr-screening.spec.js` | HR | Todo |
| Mentor Task | TC-MENTOR-TASK-001 | Tạo task | `mentor-task.spec.js` | Mentor | Todo |
| Mentor Task | TC-MENTOR-TASK-002 | Đổi status task | `mentor-task.spec.js` | Mentor | Todo |
| Mentor Course | TC-MENTOR-COURSE-001 | Tạo course | `mentor-course.spec.js` | Mentor | Todo |
| Mentor Course | TC-MENTOR-LESSON-001 | Thêm chapter và lesson | `mentor-course.spec.js` | Mentor | Todo |
| Mentor Quiz | TC-MENTOR-QUIZ-001 | Tạo quiz | `mentor-quiz.spec.js` | Mentor | Todo |
| Intern Task | TC-INTERN-TASK-001 | Xem task detail | `intern-task.spec.js` | Intern | Todo |
| Intern LMS | TC-INTERN-LMS-001 | Enroll course | `intern-lms.spec.js` | Intern | Todo |
| Intern Quiz | TC-INTERN-QUIZ-001 | Làm quiz và submit | `intern-quiz.spec.js` | Intern | Todo |
| Intern Timesheet | TC-INTERN-TIME-001 | Log timesheet | `intern-timesheet.spec.js` | Intern | Todo |
| Daily Report | TC-DAILY-001 | Intern gửi daily report | `daily-report.spec.js` | Intern | Todo |
| Daily Report | TC-DAILY-002 | Mentor feedback report | `daily-report.spec.js` | Mentor | Todo |
| API Auth | TC-API-AUTH-001 | Login API trả JWT | `api.spec.js` | Any | Todo |

---

## 14. Lộ trình triển khai

### Giai đoạn 1 - Smoke test

Mục tiêu: Chứng minh Playwright chạy được với AIMS.

- Setup `e2e`
- Viết `LoginPage`
- Viết `auth.spec.js`
- Chạy được 4 role login
- Có HTML report

### Giai đoạn 2 - Fixtures và POM

Mục tiêu: Test không lặp login code.

- Tạo `aimsFixtures.js`
- Tạo page object cho Admin, HR, Mentor, Intern
- Viết test đọc được theo business flow

### Giai đoạn 3 - CRUD quan trọng

Mục tiêu: Cover luồng chính của hệ thống.

- Admin tạo user/role/period
- HR tạo JD và upload CV
- Mentor tạo task/course/quiz
- Intern xem task, học LMS, log timesheet

### Giai đoạn 4 - API setup + UI verify

Mục tiêu: Test nhanh và ổn định hơn.

- Dùng API tạo dữ liệu nền
- Dùng UI verify luồng người dùng
- Dọn dữ liệu bằng API sau test

### Giai đoạn 5 - CI/CD

Mục tiêu: Tự động chạy regression trên PR.

- Chạy app bằng Docker Compose
- Chạy Playwright headless
- Upload report, screenshot, trace
- Fail pipeline khi smoke/regression fail

---

## Quy tắc viết test cho AIMS

1. Tên test phải có Test Case ID: `[TC-AUTH-001] Admin login redirects to dashboard`.
2. Không dùng `waitForTimeout()` trừ khi đang debug tạm thời.
3. Không phụ thuộc dữ liệu ngẫu nhiên nếu không tự tạo và tự dọn.
4. Test CRUD phải dùng dữ liệu có prefix, ví dụ `AUTO_E2E_`.
5. Luồng destructive như delete/deactivate chỉ chạy trên dữ liệu do test tạo.
6. Ưu tiên API để setup data, UI để verify trải nghiệm người dùng.
7. Mỗi role nên có fixture riêng.
8. Khi UI thiếu locator ổn định, bổ sung `data-testid` vào Razor view.

---

## Checklist hoàn thành bước đầu

```text
[ ] Tạo folder e2e
[ ] Cài Playwright
[ ] Tạo .env
[ ] Tạo playwright.config.js
[ ] Tạo LoginPage.js
[ ] Tạo auth.spec.js
[ ] Chạy pass TC-AUTH-001 đến TC-AUTH-005
[ ] Tạo aimsFixtures.js
[ ] Tạo traceability-matrix.md
[ ] Bổ sung data-testid cho các form quan trọng nếu locator chưa ổn định
```

