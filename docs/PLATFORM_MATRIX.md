# Platform Matrix — Tổng quan dịch vụ (thiết kế sau MVP)

> Tham chiếu UX: QuickBite Launch Matrix / Admin visualizer
> (`quickbite-wnkc.onrender.com`, `quickbitemanager.onrender.com`)
> **Không** implement trong Phase 2 Users/Roles. File này định hướng Phase 4+ / khi có nhiều client đã deploy.

## 1. Mục tiêu

Trên **Admin → Tổng quan (Dashboard)** mở rộng:

- Biết **bao nhiêu “web / service”** thuộc hệ sinh thái Platform đang **Healthy / Degraded / Down**
- Bấm vào một node → panel chi tiết (stack, URL, health, liên kết DocForge / Hotel…)
- Phong cách **cyber matrix** (node + cạnh), thống nhất Admin hiện tại (tokens ink/void/cyan)

## 2. Phạm vi Platform vs sản phẩm con

| Layer | Ví dụ | Ai sở hữu health? |
|-------|--------|-------------------|
| Platform | Identity API, Admin SPA | Repo `VzTong.Platform` |
| Client app | DocForge FE, DocForge PyService | Repo DocForge |
| Client app | BookingHotels Web/API | Repo BookingHotels (sau) |

Admin **không** thay health check nội bộ từng repo; chỉ **tổng hợp** endpoint public/health mà mỗi bên expose (hoặc cấu hình tay qua env / JSON registry).

## 3. Mô hình dữ liệu (đề xuất)

```ts
type ServiceStatus = 'healthy' | 'degraded' | 'down' | 'unknown'

interface PlatformNode {
  id: string              // 'identity' | 'admin' | 'docforge-fe' | ...
  name: string
  kind: 'platform' | 'client'
  stack?: string          // '.NET 10 / ABP', 'Vue 3', 'FastAPI'
  baseUrl?: string
  healthUrl?: string      // GET → 200 = healthy
  status: ServiceStatus
  lastCheckedAt?: string
}
```

Registry (env hoặc JSON trên Admin/Identity API sau này), ví dụ:

| id | healthUrl (ví dụ) |
|----|-------------------|
| `identity` | `https://identity.example.com/health` |
| `admin` | (optional — chính SPA) |
| `docforge-fe` | URL FE DocForge |
| `docforge-api` | URL PyService `/health` |
| `booking-web` | placeholder khi có Hotel |

## 4. UX (bám QuickBite, giản lược cho Platform)

1. **Dashboard MVP hiện tại:** số user, portal — **giữ nguyên** Phase 2.
2. **Dashboard + Matrix (sau):**
   - Graph hoặc grid node (Identity trung tâm, client quanh)
   - Màu: cyan = healthy, amber = degraded, magenta/red = down
   - Click node → drawer: URL, stack, “Open”, copy health URL
3. **Boot / telemetry (optional):** tái dùng pattern BootScreen login; ticker trạng thái kiểu “telemetry stream” nếu cần cảm giác “launch matrix”.

## 5. Kỹ thuật gợi ý (free-tier)

| Việc | Cách |
|------|------|
| Health poll | Admin SPA **hoặc** Identity BFF `GET /api/platform/status` (aggregate + cache) |
| CORS | Mỗi service cho phép origin Admin |
| Secret | Chỉ URL public health — **không** nhúng API key vào FE |
| Deploy (Render free) | Cold start → có thể map timeout thành `degraded` |

## 6. Liên hệ DocForge / BookingHotels

- Mỗi repo con: thêm `GET /health` (hoặc `/api/health`), ghi rõ trong README của repo đó.
- Cập nhật `DOCS.md` / `PLAN.md` Phase 4: mục **Service registry**.
- **Không** nhúng nghiệp vụ DocForge (convert) hay Hotel (phòng/đơn) vào matrix — chỉ **liveness + link**.

## 7. Thứ tự làm khi tới phase này

1. Chuẩn hóa `/health` trên Identity Host.
2. Config list node (appsettings / env / JSON).
3. API aggregate status (cache 30–60s).
4. UI matrix trên Dashboard (Vue, tái dùng tokens cyber Admin).
5. Deep-link mở FE client (DocForge, sau này Hotel).

## 8. Ngoài phạm vi

- Thay Kubernetes dashboard / full APM
- Sửa menu, billing, CRUD nghiệp vụ sản phẩm con từ Platform Matrix
- Bắt buộc implement trước khi xong Phase 2 (Users / Roles)

## 9. Liên kết nội bộ

- Admin SPA: `src/Platform.Admin`
- Identity: `src/Platform.Identity.*`
- Kế hoạch tổng: `PLAN.md` — Phase 2 (Admin MVP) → Phase 3 (DocForge) → Phase 4 (củng cố / đa client / matrix)