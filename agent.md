# 🤖 AGENT.md — เสื้อผ้า.com E-Commerce System

> **คู่มือนี้ใช้สำหรับ AI Agent หรือนักพัฒนาที่จะมาต่อยอดโปรเจกต์นี้**
> อ่านไฟล์นี้ก่อนเสมอก่อนเริ่มเขียนโค้ด และอ่านคู่กับ `README.md` (เอกสารข้อกำหนดระบบ) ซึ่งเป็น **แหล่งอ้างอิงหลักของ requirement**
> หากไฟล์นี้ขัดกับ README ให้ยึด README แล้วแก้ไฟล์นี้ให้ตรง

> [!NOTE]
> **สถานะระบบปัจจุบัน:**
> 1. `Auth Guards` เปิดใช้งานครอบคลุมทุกบทบาทแล้ว (Admin, Staff, Accounting และตรวจการเข้าสู่ระบบฝั่ง Customer)
> 2. `OrderAutomationService` ทำงานเบื้องหลังทุก 1 นาที คอย Auto-cancel / Restock และ Auto-complete อัตโนมัติ
> 3. ข้อความสถานะใน DB สะกดตามตาราง Status Constants ด้านล่างอย่างถูกต้องครบถ้วน
> 4. ทุก action ฝั่งลูกค้าที่รับ `orderId` ตรวจ IDOR แล้ว (ต้องล็อกอิน + `o.UserId == userId`)
> 5. รหัสผ่านเก็บเป็น Hash (`Services/PasswordHelper.cs`) — รหัสผ่านแบบเก่าใน DB จะถูกอัปเกรดเป็น Hash อัตโนมัติตอนล็อกอินครั้งแรก
> 6. ระบบแจ้งเตือนในเว็บ (Notifications) ใช้งานแล้วทุกบทบาท — ยังไม่มี Email

---

## 📋 ข้อมูลโปรเจกต์

| รายการ | รายละเอียด |
|---|---|
| **ชื่อระบบ** | เสื้อผ้า.com (ระบบเว็บไซต์ซื้อขายเสื้อผ้าออนไลน์) |
| **ประเภท** | E-Commerce (ASP.NET Core MVC) |
| **Framework** | .NET 10, ASP.NET Core MVC |
| **Database** | MySQL 8.0 (EF Core + Pomelo.EntityFrameworkCore.MySql) |
| **Connection** | อ่านจาก `appsettings.json` → `DefaultConnection` (สำหรับ dev ในเครื่อง; **ห้ามใส่รหัสผ่านจริงลงเอกสาร/Git** ให้ย้ายไป User Secrets หรือ environment variable) |
| **Layout หลัก** | `Views/Shared/_Layout2.cshtml` (กำหนดใน `Views/_ViewStart.cshtml`) |
| **Namespace** | `waranon_csi402` |
| **เขตเวลา** | Asia/Bangkok (UTC+7) — ใช้กับโปรโมชั่น Flash Sale และ Auto-cancel |

---

## 🏗️ โครงสร้างโปรเจกต์

```
waranon_csi402/
├── Controllers/
│   ├── AccountController.cs      ✅ Login, Register (ใช้ TempData ไม่ใช่ Session)
│   ├── AdminController.cs        ✅ Users, Products, Variants, Categories, Promotions, Coupons, Orders, SalesReport
│   ├── HomeController.cs         ✅ Index (Storefront+Filter), ProductDetail
│   ├── CartController.cs         ✅ Cart, Checkout, PlaceOrder, OrderSuccess, UploadSlip, MyOrders, OrderDetail
│   ├── AccountingController.cs   ✅ คิวตรวจสลิป, ตรวจสอบรายละเอียด, อนุมัติ/ปฏิเสธ, รายงานสรุปรายรับ (พร้อม Auth Guard)
│   └── StaffController.cs        ✅ แดชบอร์ดออเดอร์, แพ็คของ, จัดส่ง (Tracking), แจ้งเตือน & เติมสต็อก
│
├── Services/
│   ├── OrderAutomationService.cs ✅ BackgroundService: Auto-cancel / Restock / Auto-complete + แจ้งเตือนลูกค้า
│   ├── ShopRules.cs              ✅ เวลาไทย (ThaiNow), ตรวจโปร Flash Sale และช่วงเวลา 00:01–02:00
│   ├── PasswordHelper.cs         ✅ Hash / Verify รหัสผ่าน (PasswordHasher) + อัปเกรดรหัสเก่า
│   └── NotificationHelper.cs     ✅ Notify(userId) / NotifyRole("Staff"|"Accounting") ลงตาราง Notifications
│
├── Models/                       ✅ Scaffolded จาก DB 14 ตาราง + เพิ่มเอง 2 ตาราง
│   ├── Csi402dbContext.cs        (ไฟล์ Scaffold — อย่าแก้มือ)
│   ├── Csi402dbContext.Extra.cs  ✅ partial: PromotionProducts, Banners (แยกไฟล์ไว้ไม่ให้หายตอน Scaffold ใหม่)
│   ├── PromotionProduct.cs       ✅ สินค้าที่ร่วมโปร P4 จับคู่ 2 ชิ้น
│   ├── Banner.cs                 ✅ แบนเนอร์หน้าร้าน
│   ├── User.cs, Address.cs, Category.cs
│   ├── Product.cs, Productvariant.cs
│   ├── Order.cs, Orderitem.cs, Cartitem.cs
│   ├── Promotion.cs, Coupon.cs
│   ├── Review.cs, Wishlist.cs, Notification.cs
│   ├── Labstudent.cs             (ใช้สำหรับงานแล็บ ไม่เกี่ยวกับ E-Commerce)
│   └── DbInitializer.cs          ✅ Seed ข้อมูลเสื้อผ้าตัวอย่างเมื่อ DB ว่าง + EnsureExtraTables() สร้างตารางใหม่ให้อัตโนมัติ
│
├── ViewModels/
│   ├── LoginViewModel.cs         ✅
│   ├── RegisterViewModel.cs      ✅ รองรับ Role field
│   ├── Lab5ViewModel.cs          (งานแล็บ)
│   └── Lab9ViewModel.cs          (งานแล็บ)
│
├── Views/
│   ├── _ViewStart.cshtml         → Layout = "_Layout2"  ← Layout หลัก
│   ├── _ViewImports.cshtml       → @using waranon_csi402.Models
│   ├── Shared/
│   │   ├── _Layout2.cshtml       ✅ Navbar หน้าร้านหลัก (เชื่อมหมวดหมู่ครบ)
│   │   ├── _LayoutAdmin.cshtml   ✅ Admin Layout (Sidebar + Responsive)
│   │   ├── _LayoutStaff.cshtml   ✅ Staff Layout (ธีมเขียว Emerald + Responsive)
│   │   ├── _LayoutAccounting.cshtml ✅ Accounting Layout (ธีม Dark Navy + Print Friendly)
│   │   ├── _SidebarAdmin.cshtml  ✅ มีปุ่มไปหน้าร้าน
│   │   ├── _SidebarStaff.cshtml  ✅ เชื่อมต่อเมนูออเดอร์รอแพ็ค, จัดส่ง, สต็อก
│   │   └── _SidebarAccounting.cshtml ✅ เมนูตรวจสลิป, อนุมัติแล้ว, ไม่ถูกต้อง, รายงานการเงิน
│   ├── Home/
│   │   ├── Index.cshtml          ✅ หน้าร้านหลัก (Catalog + Filter + Search)
│   │   └── ProductDetail.cshtml  ✅ หน้าดูสินค้า + SKU + SizeGuide + Related
│   ├── Cart/                     ✅ Index, Checkout, OrderSuccess, MyOrders, OrderDetail
│   ├── Admin/                    ✅ Index (Dashboard), UserList, AddUser, EditUser, Product*, Category*, Promotion*, Coupon*, Order*, SalesReport
│   ├── Account/
│   │   ├── Login.cshtml          ✅
│   │   └── Register.cshtml       ✅
│   ├── Accounting/
│   │   ├── Index.cshtml          ✅ คิวงานตรวจสอบสลิป + Quick Preview Modal + Quick Reject Modal
│   │   ├── VerifySlip.cshtml     ✅ ตรวจสอบสลิปละเอียด + รูปซูม + เช็กลิสต์ + บัญชีรับโอน + โมดอลปฏิเสธ
│   │   └── FinancialReport.cshtml ✅ รายงานสรุปรายรับ พร้อมฟังก์ชันพิมพ์ (Print Layout & ลายเซ็น)
│   └── Staff/
│       ├── Index.cshtml          ✅ คิวงานจัดส่ง (Paid, Processing, Shipped) + Stat Cards
│       ├── OrderDetail.cshtml    ✅ รายการหยิบของ (Checklist) + ปุ่มเริ่มแพ็ค + คีย์ Tracking
│       ├── PrintSlip.cshtml      ✅ ใบปะหน้ากล่องพัสดุ & Packing Slip พร้อมพิมพ์
│       └── StockAlert.cshtml     ✅ แจ้งเตือนสินค้าใกล้หมด (≤10) + Quick Restock ด่วน
│
├── database/
│   ├── CSI402DB_setup.sql        ✅ Script สร้าง DB + ตารางทั้งหมด (14 tables)
│   ├── sample_clothing_data.sql  ✅ ข้อมูลตัวอย่างเสื้อผ้า + SKU ครบ
│   └── Assessment8_DataDictionary.html
│
├── App_Data/slips/               ✅ เก็บสลิปโอนเงิน (นอก wwwroot, อยู่ใน .gitignore) ดูผ่าน /Cart/SlipImage ที่ตรวจสิทธิ์
├── wwwroot/images/slips/         ⚠️ สลิปเก่าก่อนย้าย (ยังเปิดผ่าน URL ได้ — ลบทิ้งหรือย้ายเมื่อไม่ใช้แล้ว)
├── wwwroot/images/banners/       รูปแบนเนอร์ที่ Admin อัปโหลด (สาธารณะ)
└── Program.cs                    ✅ EnsureExtraTables() + DbInitializer.Seed() ตอนเริ่มแอป
```

---

## ✅ สิ่งที่พัฒนาแล้ว (Completed)

### 1. ฐานข้อมูลและ Models
- ✅ SQL Script ครบทั้ง 14 ตาราง (`CSI402DB_setup.sql`)
- ✅ EF Core Model Scaffold ครบทุก Entity
- ✅ `DbInitializer.cs` — Seed ข้อมูลเสื้อผ้าและหมวดหมู่เมื่อ DB ว่าง
- ✅ ข้อมูลตัวอย่างเสื้อผ้าแฟชั่น 7+ รายการ พร้อม ProductVariants (สี/ไซส์/สต็อก)

### 2. ระบบสมาชิก (User Management — ทำแล้วบางส่วน)
- ✅ Login redirect ตาม Role: Admin→`/Admin/Index`, Staff→`/Staff/Index`, Accounting→`/Accounting/Index`, Customer→`/Home/Index`
- ✅ `AccountController` — Login + Register (ตรวจ Email ซ้ำ)
- ✅ เก็บสถานะ Login ผ่าน `TempData` (key: `"Name"`, `"Role"`, `"LoginEmail"`, `"PhoneNumber"`)
- ✅ `AdminController` — UserList, AddUser (กำหนด Role), EditUser, DeleteUser
- ✅ **Address Book & Profile (เสร็จสมบูรณ์):**
  - ✅ `AccountController.Profile` & `Views/Account/Profile.cshtml` — จัดการโปรไฟล์ส่วนตัว (ชื่อ-นามสกุล, เบอร์โทร), สรุปยอดออเดอร์/Wishlist
  - ✅ `AccountController.ChangePassword` — เปลี่ยนรหัสผ่านพร้อมตรวจสอบรหัสผ่านเดิม
  - ✅ `AccountController.AddAddress`, `EditAddress`, `DeleteAddress` — จัดการสมุดที่อยู่ครบทุกฟังก์ชัน
  - ✅ `AccountController.SetDefaultAddress` — กำหนดที่อยู่จัดส่งเริ่มต้น (Default Address) และเชื่อมโยงกับการเลือกอัตโนมัติในหน้า Checkout
  - ✅ `CartController.TrackOrder` & `Views/Cart/TrackOrder.cshtml` — หน้าค้นหาและติดตามสถานะพัสดุสาธารณะโดยไม่ต้องล็อกอิน (Public Track Order)

### 3. หน้าร้านและสั่งซื้อสินค้า (Storefront, Cart & Checkout)
- ✅ `HomeController.Index` & `Views/Home/Index.cshtml` — Hero Banner ค้นหาในตัว, คูปองคลิกคัดลอก, การ์ดหมวดหมู่, ระบบจัดเรียง (ใหม่ล่าสุด, ราคาต่ำ-สูง), การ์ดสินค้าพร้อมป้ายสต็อก
- ✅ `HomeController.ProductDetail` — รายละเอียดสินค้า + Related Products + เลือก SKU ไดนามิก + Size Guide
- ✅ `CartController.Index` — ปรับจำนวน (+/-), ลบสินค้า, คูปองส่วนลด, คำนวณโปรโมชั่นอัตโนมัติที่ดีที่สุด, คำนวณค่าจัดส่ง
- ✅ `CartController.Checkout` — เลือกที่อยู่, เลือกวิธีชำระเงิน, เช็คสต็อกซ้ำก่อนยืนยัน
- ✅ `CartController.PlaceOrder` — ตัดสต็อกระดับ SKU ทันที บันทึก Order สถานะ `Pending Payment` + OrderItems และเคลียร์ตะกร้า
- ✅ `CartController.OrderSuccess` — หน้ายืนยัน ข้อมูลบัญชีโอนเงิน และฟอร์มแนบสลิป
- ✅ `CartController.UploadSlip` — อัปโหลดสลิปเข้า `wwwroot/images/slips/` และเปลี่ยนสถานะเป็น `Verifying Slip`
- ✅ `CartController.MyOrders` / `OrderDetail` — ประวัติคำสั่งซื้อ + ไทม์ไลน์สถานะ + กล่องคัดลอก Tracking Number

**ผลการตรวจกับโค้ดจริง (2026-11-22):**
- [x] ตอน Checkout ถ้า SKU หมด ระบบนำสินค้าออกจากตะกร้าอัตโนมัติ + แจ้งเตือน
- [x] `UploadSlip` รองรับ `Invalid Slip` → `Verifying Slip`
- [x] เลือกโปรหรือคูปองที่ลดสูงสุดเพียง 1 สิทธิ์
- [x] Flash Sale (P5) ใช้เวลา Asia/Bangkok ผ่าน `ShopRules.ThaiNow()` และช่วง 00:01–02:00 (รวมนาที 02:00)
- [x] P4 คิดเฉพาะสินค้าที่ร้านกำหนด (ตาราง `PromotionProducts`) ลดจากราคาของชิ้นที่จับคู่ได้ (ทุก 2 ชิ้น)
- [x] `CartController` ต้องล็อกอิน และเห็น/แก้ได้เฉพาะออเดอร์ของตนเอง (IDOR) รวมถึงที่อยู่ใน `PlaceOrder`
- [x] ผู้ใช้ที่ยังไม่ล็อกอินกด Checkout ถูกบังคับไป Login
- [x] `PlaceOrder` ทำใน Transaction และตัดสต็อกด้วย `UPDATE ... WHERE Stock >= qty` กันสต็อกติดลบ
- [x] `TrackOrder` ต้องกรอกทั้ง OrderId และเบอร์โทรที่ตรงกันทุกตัว
- [x] ลูกค้าขอคืนสินค้า/คืนเงินได้ (`CartController.RequestRefund` → แจ้งฝ่ายบัญชี)

### 4. ระบบจัดการหลังบ้าน (Admin System)
- ✅ `_LayoutAdmin.cshtml` / `_SidebarAdmin.cshtml` — Sidebar, Responsive, Active Route
- ✅ `Admin/Index` — Dashboard สถิติภาพรวม + ตารางสินค้าล่าสุด + เมนูลัด
- ✅ `Admin/UserList`, `AddUser`, `EditUser`, `DeleteUser` — จัดการผู้ใช้งาน
- ✅ `Admin/ProductList`, `AddProduct`, `EditProduct`, `DeleteProduct` + Toggle เปิด/ปิดขาย
- ✅ `Admin/ManageVariants` — จัดการ SKU (สี/ไซส์/สต็อก/ราคาบวกเพิ่ม)
- ✅ `Admin/CategoryList`, `AddCategory`, `EditCategory`, `DeleteCategory`
- ✅ `Admin/PromotionList`, `AddPromotion`, `EditPromotion`, `DeletePromotion`, `TogglePromotionStatus` (P1–P5)
- ✅ `Admin/AddCoupon`, `EditCoupon`, `DeleteCoupon`, `ToggleCouponStatus`
- ✅ `Admin/OrderList`, `AdminOrderDetail`, `UpdateOrderStatus`
- ✅ `Admin/SalesReport` — รายงานยอดขายและสินค้าขายดี
- ✅ Auth Guard ผ่าน `OnActionExecuting` (Role == "Admin") ทุก action
- ✅ `Admin/BannerList`, `AddBanner`, `ToggleBanner`, `DeleteBanner` — แบนเนอร์หน้าร้าน (แสดงเป็น Carousel บนหน้าแรก)
- ✅ `AddPromotion` / `EditPromotion` เลือกสินค้าที่ร่วมโปร P4 ได้ (`_PairProductPicker.cshtml`)
- ✅ `UpdateOrderStatus` ตรวจตาราง State Transition ผ่าน `GetAllowedNextStatuses()` และ dropdown แสดงเฉพาะสถานะที่ไปต่อได้; คืนสต็อกเมื่อ Cancelled
- ✅ ลบข้อมูล (Delete*) เป็น POST + `[ValidateAntiForgeryToken]` ทั้งหมด
- ✅ EditUser ไม่ดึงรหัสผ่านเดิมมาแสดงแล้ว (กรอกเฉพาะเมื่อจะตั้งรหัสใหม่)

### 5. ระบบพนักงานและคลังสินค้า (Staff System)
- ✅ **Auth Guard** — `CheckStaffAuth()` ใช้ในทุก 7 actions (ตรวจ Login + Role == "Staff")
- ✅ `_LayoutStaff.cshtml` / `_SidebarStaff.cshtml`
- ✅ `StaffController.Index` — คิวงานตามสถานะ (Paid, Processing, Shipped, All) + ค้นหา + Stat Cards
- ✅ `StaffController.OrderDetail` — Packing Slip + Checklist
- ✅ `StaffController.PackOrder` — `Paid` ➔ `Processing`
- ✅ `StaffController.ShipOrder` — กรอก Tracking Number, `Processing` ➔ `Shipped` (รับเฉพาะ Processing ต้องกดเริ่มแพ็คก่อน)
- ✅ `StaffController.PrintSlip` — ใบปะหน้าพัสดุ
- ✅ `StaffController.StockAlert` — สินค้าใกล้หมด (≤ 10) และหมด (0)
- ✅ `StaffController.QuickRestock` — เติมสต็อกด่วน
- ℹ️ ขอบเขตสิทธิ์ตาม README: จัดการออเดอร์ได้เฉพาะสถานะ `Paid` เป็นต้นไป และแก้สต็อกได้เฉพาะ "เบื้องต้น" (เพิ่มสต็อก) — ห้ามแก้ราคา/รายละเอียดสินค้า
- ✅ แจ้งเตือนพนักงาน (Notifications) เมื่อมีออเดอร์ใหม่ถึงสถานะ `Paid` และเมื่อ SKU เหลือ ≤ 10 ชิ้นหลังลูกค้าสั่งซื้อ

### 6. ระบบฝ่ายการเงินและบัญชี (Accounting System — ✅ พัฒนาและจัด UX/UI เสร็จสมบูรณ์ 100%)
- ✅ `_LayoutAccounting.cshtml` / `_SidebarAccounting.cshtml` — Layout สไตล์ Dark Navy พร้อมเมนูนำทาง ไฮไลต์แท็บ และสไตล์รองรับการพิมพ์
- ✅ `AccountingController.Index` & `Views/Accounting/Index.cshtml` — คิวงานตรวจสลิป แบ่งตามแท็บ (รอตรวจ, อนุมัติแล้ว, สลิปไม่ถูกต้อง, ทั้งหมด) + 4 Stat Cards สรุปยอดเงิน + Quick Slip Preview Modal (กดดูสลิปภาพใหญ่ได้ทันทีโดยไม่ต้องเปิดแท็บใหม่) + Quick Reject Modal พร้อมปุ่มเลือกเหตุผลยอดฮิต
- ✅ `AccountingController.VerifySlip` & `Views/Accounting/VerifySlip.cshtml` — หน้าตรวจสลิปละเอียด 2 คอลัมน์ (ซ้าย: สลิปซูมได้ + บัญชีรับโอน + เช็กลิสต์การตรวจ, ขวา: กล่องยอดที่ต้องได้รับตัวโต + ปุ่มอนุมัติ/ปฏิเสธ + ข้อมูลลูกค้าและรายการสินค้า)
- ✅ `AccountingController.ApproveSlip` — `Verifying Slip` ➔ `Paid` (รับเฉพาะ Verifying Slip; แจ้งลูกค้า + แจ้ง Staff)
- ✅ `AccountingController.RejectSlip` — `Verifying Slip` ➔ `Invalid Slip` (รับเฉพาะ Verifying Slip); เหตุผลส่งถึงลูกค้าทาง Notification (ไม่มีคอลัมน์เก็บใน Orders)
- ✅ `AccountingController.RefundOrder` — `Paid/Processing/Shipped/Completed` ➔ `Refunded` ต้องระบุเหตุผล, คืนสต็อกเฉพาะเมื่อติ๊ก "ได้รับสินค้าคืนแล้ว"; ฟอร์มอยู่หน้า VerifySlip + แท็บ "คืนเงินแล้ว"
- ✅ `AccountingController.FinancialReport` & `Views/Accounting/FinancialReport.cshtml` — รายงานสรุปรายรับ ยอดขาย ส่วนลด ค่าส่ง พร้อมหน้าพิมพ์รายงานทางราชการ (@media print ซ่อนเมนู มีช่องลงนามผู้อนุมัติ)
- ✅ **Auth Guard** — `OnActionExecuting` ตรวจสอบ Login + Role == "Accounting" หรือ "Admin" ทุก Action ครบถ้วน

### 7. ระบบเสริมลูกค้า (Customer Experience)
- ✅ `HomeController.Wishlist` & `ToggleWishlist` & `Views/Home/Wishlist.cshtml` — บันทึก/ยกเลิกรายการโปรด
- ✅ `HomeController.AddReview` — รีวิวและให้คะแนนดาว (1–5) เฉพาะผู้ที่ล็อกอินและมีออเดอร์ `Completed` ของสินค้านั้น, รีวิวได้คนละ 1 ครั้งต่อสินค้า
- ✅ `AccountController.Notifications` — หน้าการแจ้งเตือน (ใช้ได้ทุกบทบาท) + กระดิ่งจำนวนที่ยังไม่อ่าน (`_NotificationBell.cshtml`) บน Navbar และ Sidebar หลังบ้าน
- ✅ Dynamic Cart Count Badge — แสดงจำนวนสินค้าจริง ซ่อน Badge เมื่อตะกร้าว่าง

---

## 🔴 สิ่งที่ยังไม่ได้พัฒนา (Pending)

เรียงตามลำดับความสำคัญที่แนะนำ

### P0 — ความปลอดภัยและสิทธิ์การเข้าถึง (Auth Guards) — ✅ เปิดใช้งานครบทุกส่วนแล้ว
- ✅ `AdminController.OnActionExecuting` — บังคับ Login และตรวจ Role == "Admin" (หากไม่ใช่ ส่งกลับ `/Home/Index`)
- ✅ `StaffController.OnActionExecuting` — บังคับ Login และตรวจ Role == "Staff" / "Admin"
- ✅ `AccountingController.OnActionExecuting` — บังคับ Login และตรวจ Role == "Accounting" / "Admin"
- ✅ `CartController.GetCurrentUserId` & `AccountController.GetCurrentCustomer` — ลบ Fallback ทดสอบออกแล้ว ผู้ใช้ที่ยังไม่ล็อกอินจะถูกบังคับไปหน้า Login ตามข้อกำหนด
- ✅ `AccountController.Login` — Redirect อัตโนมัติตาม Role: Admin→`/Admin/Index`, Staff→`/Staff/Index`, Accounting→`/Accounting/Index`, Customer→`/Home/Index`

### P1 — ระบบอัตโนมัติ (Automation) — ✅ พัฒนาและลงทะเบียนใช้งานแล้ว
- ✅ พัฒนา `Services/OrderAutomationService.cs` (`BackgroundService`) ลงทะเบียนใน `Program.cs` รันตรวจสอบทุก 1 นาที:
  - **ออเดอร์ปกติไม่แนบสลิป:** `Pending Payment` เกิน 24 ชม. ➔ `Cancelled` + คืนสต็อกเข้า SKU ทันที
  - **ออเดอร์ Flash Sale (P5) ไม่แนบสลิป:** `Pending Payment` เกิน 1 ชม. ➔ `Cancelled` + คืนสต็อก
  - **สลิปไม่ถูกต้องไม่แนบใหม่:** `Invalid Slip` เกิน 12 ชม. ➔ `Cancelled` + คืนสต็อก
  - **Auto-complete:** `Shipped` เกิน 7 วัน ➔ `Completed` อัตโนมัติ

### P2 — วงจรหลังจัดส่งและคืนสินค้า — ✅ ทำแล้ว
- ✅ ปุ่ม "ได้รับสินค้าแล้ว" ฝั่งลูกค้า
- ✅ ลูกค้าส่งคำขอคืน (`Cart/RequestRefund`) → แจ้งฝ่ายบัญชีผ่าน Notification → บัญชีกด `Accounting/RefundOrder` → `Refunded`
- ✅ นโยบายคืนสต็อก: คืนเมื่อ `Cancelled` เสมอ; เมื่อ `Refunded` คืนเฉพาะเมื่อฝ่ายบัญชีติ๊กว่าได้รับสินค้าคืนและตรวจแล้ว
- ℹ️ คำขอคืนเงินไม่มีสถานะ/ตารางของตัวเอง (อยู่ในรูป Notification) ถ้าต้องติดตามคำขอเป็นรายการ ให้เพิ่มตาราง `RefundRequests`

### P2 — การแจ้งเตือน (Notification) — 🟡 In-app ทำแล้ว, Email ยังไม่มี
- ✅ In-app notification ทุกครั้งที่สถานะออเดอร์เปลี่ยน: สั่งซื้อ, แนบสลิป, Paid, Invalid Slip (พร้อมเหตุผล), Processing, Shipped, Completed, Cancelled (ทั้งลูกค้า/ระบบ), Refunded, Admin เปลี่ยนสถานะ
- ✅ แจ้งฝ่ายบัญชีเมื่อมีสลิปใหม่/แนบใหม่ และคำขอคืนเงิน; แจ้งพนักงานเมื่อออเดอร์ถึง `Paid` และสต็อกใกล้หมด
- ❌ **Email** แจ้งลูกค้าตาม README — ยังไม่มี ต้องเลือก SMTP/บริการส่งอีเมลและเก็บ credential นอก Git (ต่อยอดได้ใน `NotificationHelper.Notify`)

### P3 — ระบบเสริมลูกค้า (Customer Experience)
- ✅ ~~Wishlist~~ — ทำแล้ว (ดูหัวข้อ 7)
- ✅ ~~Review & Rating~~ — ทำแล้ว (ดูหัวข้อ 7)
- ✅ ~~Track Order: ติดตามด้วย OrderID แบบสาธารณะไม่ต้องล็อกอิน (`CartController.TrackOrder`)~~ — ทำแล้ว
- ✅ ~~Address Book + ที่อยู่เริ่มต้น + Profile page (`AccountController.Profile`)~~ — ทำแล้ว

### P3 — ส่วนเสริมฝั่ง Admin
- ✅ จัดการแบนเนอร์หน้าร้าน (ตาราง `Banners`)
- ตรวจว่า Dashboard/SalesReport ครอบคลุม "รายงานสถิติ" ตามที่ README ต้องการ
- แบนเนอร์ยังแก้ไขไม่ได้ (มีแค่ เพิ่ม/ซ่อน/ลบ) และลบแบนเนอร์ไม่ได้ลบไฟล์รูปออกจาก `wwwroot/images/banners`

### P3 — หนี้ทางเทคนิคที่เหลือ
- สร้างคลาส `OrderStatus` (const string) แทน string สถานะที่กระจายอยู่ทุก Controller/View
- ย้ายรหัสผ่าน DB ออกจาก `appsettings.json` และ `Csi402dbContext.OnConfiguring` ไป User Secrets
- ย้าย Login จาก `TempData` ไป Cookie Authentication + `[Authorize(Roles = ...)]`
- `Account/Logout` ยังเป็น GET

---

## 🗄️ โครงสร้างฐานข้อมูล (16 Tables)

| # | ชื่อตาราง | ความสัมพันธ์หลัก | สถานะ |
|---|---|---|---|
| 1 | `Labstudent` | - | ใช้สำหรับงานแล็บเท่านั้น |
| 2 | `Users` | - | ✅ ใช้งานแล้ว (CRUD ผ่าน Admin, Login/Register) |
| 3 | `Addresses` | FK → Users | ✅ ใช้งานแล้ว (Checkout + สมุดที่อยู่/ที่อยู่เริ่มต้นใน Profile) |
| 4 | `Categories` | - | ✅ ใช้ Filter หน้าร้าน + จัดการโดย Admin |
| 5 | `Products` | FK → Categories | ✅ ใช้งานแล้ว |
| 6 | `ProductVariants` | FK → Products | ✅ ใช้งานแล้ว (SKU, ตัดสต็อก, Quick Restock) |
| 7 | `Promotions` | FK → Categories | ✅ จัดการโดย Admin + ใช้คำนวณในตะกร้า |
| 8 | `Coupons` | - | ✅ จัดการโดย Admin + ใช้ในตะกร้า |
| 9 | `Orders` | FK → Users, Addresses, Promotions, Coupons | ✅ ใช้งานแล้ว (Cart, Staff, Admin) |
| 10 | `OrderItems` | FK → Orders, ProductVariants | ✅ ใช้งานแล้ว |
| 11 | `CartItems` | FK → Users, ProductVariants | ✅ ใช้งานแล้ว |
| 12 | `Wishlists` | FK → Users, Products | ✅ ใช้งานแล้ว (Wishlist + ToggleWishlist) |
| 13 | `Reviews` | FK → Products, Users | ✅ ใช้งานแล้ว (AddReview + แสดงใน ProductDetail) |
| 14 | `Notifications` | FK → Users | ✅ ใช้งานแล้ว (`NotificationHelper` + หน้า Account/Notifications) |
| 15 | `PromotionProducts` | FK → Promotions, Products | ✅ สินค้าที่ร่วมโปร P4 (PK ร่วม PromotionID + ProductID) |
| 16 | `Banners` | - | ✅ แบนเนอร์หน้าร้าน |

> ตาราง 15–16 อยู่ใน `CSI402DB_setup.sql` แล้ว และ `DbInitializer.EnsureExtraTables()` จะสร้างให้อัตโนมัติ (`CREATE TABLE IF NOT EXISTS`) สำหรับ DB เดิม

---

## 🔐 ระบบ Authentication & Authorization

> [!IMPORTANT]
> โปรเจกต์นี้ **ไม่ได้ใช้** ASP.NET Core Identity หรือ Cookie Authentication ตามมาตรฐาน
> แต่ใช้ **`TempData`** เก็บข้อมูลสถานะผู้ใช้งานแทน

> [!CAUTION]
> **หนี้ทางเทคนิค (Technical Debt):** `TempData` ออกแบบมาเก็บข้อมูลชั่วคราวข้าม redirect ไม่ใช่เก็บสถานะล็อกอิน
> ค่าจะถูกลบเมื่ออ่านด้วย `[]` และอาจหลุดการล็อกอินได้ง่าย (ถ้าลืมใช้ `Peek`) แนะนำให้ย้ายไป Cookie Authentication
> พร้อม `[Authorize(Roles = "...")]` เมื่อมีเวลา และแทน `CheckXxxAuth()` ทั้งหมดด้วยวิธีเดียวกัน
> อย่าสร้างระบบใหม่ที่พึ่งพา `TempData` เพิ่มโดยไม่จำเป็น

### ข้อมูล TempData ที่ใช้หลังล็อกอิน

| Key | ค่า | ใช้ตรวจสอบ |
|---|---|---|
| `TempData["Name"]` | ชื่อ-นามสกุล | แสดงบน Navbar |
| `TempData["Role"]` | `"Admin"` / `"Staff"` / `"Accounting"` / `"Customer"` | แบ่งสิทธิ์เมนู |
| `TempData["LoginEmail"]` | อีเมล | ใช้ Query ข้อมูลเพิ่มเติม |
| `TempData["PhoneNumber"]` | เบอร์โทร | แสดงโปรไฟล์ |

### วิธีตรวจสอบสิทธิ์ใน View (Pattern ปัจจุบัน)
```razor
@if (TempData.Peek("Role")?.ToString() == "Admin")
{
    <!-- แสดงเฉพาะ Admin -->
}
```

### วิธีตรวจสอบสิทธิ์ใน Controller (Pattern ที่ควรทำ)
```csharp
// ตรวจสอบ Login
if (TempData.Peek("Name") == null)
{
    return RedirectToAction("Login", "Account");
}

// ตรวจสอบ Role
if (TempData.Peek("Role")?.ToString() != "Admin")
{
    return RedirectToAction("Index", "Home");
}
```

### ตารางสิทธิ์ตามบทบาท (ตาม README)

| Role | ทำได้ | เปลี่ยนสถานะออเดอร์ได้ | ห้าม |
|---|---|---|---|
| Customer | ดูสินค้า, ตะกร้า, สั่งซื้อ, แนบสลิป, ดูออเดอร์ของตนเอง | แนบสลิป (`Pending Payment`/`Invalid Slip` → `Verifying Slip`), ยืนยันรับสินค้า (→ `Completed`) | ดูออเดอร์/ข้อมูลของผู้อื่น |
| Accounting | ตรวจสลิป, คืนเงิน | `Verifying Slip` → `Paid` / `Invalid Slip`; → `Refunded/Returned` | แก้สินค้า, แพ็ค/จัดส่ง |
| Staff | แพ็ค/จัดส่ง, แก้ไขสต็อกเบื้องต้น | `Paid` → `Processing` → `Shipped` | ตรวจสลิป, แก้ราคา/โปรโมชั่น |
| Admin | จัดการทุกอย่าง, ดูรายงาน | ทุกสถานะ (ต้องผ่านกฎ transition) | - |
| ระบบอัตโนมัติ | - | `Pending Payment`/`Invalid Slip` → `Cancelled`; `Shipped` → `Completed` | - |

---

## 🛒 Order State Machine

```
สั่งซื้อ → [Pending Payment]   (ตัดสต็อกทันที)
              ↓ ลูกค้าแนบสลิป
         [Verifying Slip]
              ↓ บัญชีอนุมัติ        ↓ บัญชีปฏิเสธ
           [Paid]              [Invalid Slip]
              ↓ พนักงานแพ็คสินค้า       ↓ ลูกค้าแนบสลิปใหม่ (ใน 12 ชม.)
         [Processing]                  ↑ กลับไป Verifying Slip
              ↓ กรอก Tracking Number
           [Shipped]
              ↓ ออเดอร์ถึงมือลูกค้า (ลูกค้ากดยืนยัน หรือ auto ครบ X วัน)
         [Completed]

ยกเลิกอัตโนมัติ:
  - ไม่แนบสลิปใน 24 ชม.    → [Cancelled] + คืนสต็อก
  - Flash Sale ไม่แนบใน 1 ชม. → [Cancelled] + คืนสต็อก
  - Invalid Slip ไม่แนบใน 12 ชม. → [Cancelled] + คืนสต็อก

อื่น ๆ:
  - แอดมินกดยกเลิก → [Cancelled] + คืนสต็อก
  - มีการคืนสินค้า/คืนเงิน → [Refunded/Returned]
```

### Status Constants (ต้องสะกดตรงนี้เท่านั้น)

| ชื่อใน README | ค่าที่เก็บใน DB |
|---|---|
| รอชำระเงิน | `Pending Payment` |
| รอตรวจสอบสลิป | `Verifying Slip` |
| ชำระเงินเรียบร้อย | `Paid` |
| กำลังเตรียมจัดส่ง | `Processing` |
| จัดส่งแล้ว | `Shipped` |
| สำเร็จ | `Completed` |
| ยกเลิก | `Cancelled` |
| สลิปไม่ถูกต้อง | `Invalid Slip` |
| คืนเงิน/คืนสินค้า | `Refunded` |

> คำแนะนำ: ถ้ายังไม่มี ให้สร้างคลาส `OrderStatus` (const string) ไว้ที่เดียวแล้วอ้างอิงจากทุก Controller/Service
> ก่อนเขียนโค้ดใหม่ให้ตรวจ string ที่ใช้อยู่จริงใน `CartController`, `StaffController`, `AdminController` ว่าตรงตารางนี้ (โดยเฉพาะ `Refunded` กับ `Refunded/Returned`)

### State Transition ที่อนุญาต

| จาก | ไป | ผู้ทำ |
|---|---|---|
| Pending Payment | Verifying Slip | Customer |
| Pending Payment | Cancelled | ระบบ (หมดเวลา) / Admin |
| Verifying Slip | Paid | Accounting |
| Verifying Slip | Invalid Slip | Accounting |
| Invalid Slip | Verifying Slip | Customer (แนบใหม่ ≤ 12 ชม.) |
| Invalid Slip | Cancelled | ระบบ (หมดเวลา) / Admin |
| Paid | Processing | Staff |
| Processing | Shipped | Staff (ต้องมี Tracking Number) |
| Shipped | Completed | Customer / ระบบ (ครบ X วัน) |
| Paid / Processing / Shipped / Completed | Refunded | Accounting (ผ่านคำขอคืน) |

การเปลี่ยนสถานะที่ไม่อยู่ในตารางนี้ต้องถูกปฏิเสธ (รวมถึง `Admin/UpdateOrderStatus` ควรตรวจกฎนี้ด้วย)

---

## 💰 กฎการคำนวณโปรโมชั่น

> [!IMPORTANT]
> **กฎสำคัญ:** เลือกโปรโมชั่นที่ให้ส่วนลดสูงสุด **เพียง 1 สิทธิ์** ต่อ 1 คำสั่งซื้อ
> การเปรียบเทียบนับรวม Coupon Code ที่ลูกค้ากรอกด้วย
> **"ยอดสุทธิ"** = Subtotal (ก่อนบวกค่าจัดส่ง) ใช้สำหรับตรวจเงื่อนไขทุกโปรโมชั่น

| โปรโมชั่น | เงื่อนไข | ส่วนลด |
|---|---|---|
| P1 | Subtotal ≥ 1,000 บาท | 10% |
| P2 | Subtotal ≥ 3,000 บาท | 15% |
| P3 | ยอดสินค้าในหมวดหมู่ที่กำหนด ≥ 500 บาท | 15% |
| P4 | สินค้าที่ร้านกำหนด จับคู่ครบ 2 ชิ้นในออเดอร์ | 10% (จากราคาปกติของสินค้าคู่นั้น) |
| P5 (Flash Sale) | วันที่ 1 ของเดือน, สั่งซื้อช่วง 00:01–02:00 น. (เวลาไทย), Subtotal ≥ 2,000 บาท | 20% |

หมายเหตุการ implement (ดู `CartController.CalculateBestDiscount` + `Services/ShopRules.cs`):
- P5: ระบุจากชื่อโปรมีคำว่า "Flash Sale" (`ShopRules.IsFlashSalePromotion`) — **ห้ามเปลี่ยนชื่อโปรให้ไม่มีคำนี้** ไม่งั้นระบบจะไม่รู้ว่าเป็น Flash Sale; ใช้เวลาไทยจาก `ShopRules.ThaiNow()`; Auto-cancel 1 ชม. ดูจาก `Order.Promotion` ของออเดอร์
- P3 ใช้ `Promotions.CategoryID`
- P4: ถือเป็นโปรจับคู่เมื่อมีสินค้าใน `PromotionProducts` หรือชื่อมีคำว่า "จับคู่"/"2 ชิ้น"; แตกสินค้าที่ร่วมโปรเป็นราคาต่อชิ้น จับคู่ทีละ 2 (เลือกชิ้นแพงสุดก่อน) แล้วลด % จากราคาของชิ้นที่จับคู่ได้ (Fixed = ลดต่อคู่) — ถ้าไม่ได้เลือกสินค้าไว้ จะไม่มีส่วนลด
- เก็บส่วนลดและโปรโมชั่น/คูปองที่ถูกเลือกลงใน `Orders` ตอน `PlaceOrder` (ห้ามคำนวณใหม่ภายหลังจากราคาปัจจุบัน)
- เมื่อคำนวณซ้ำตอน `PlaceOrder` ให้ใช้ค่าจากฝั่งเซิร์ฟเวอร์เท่านั้น ห้ามเชื่อยอดหรือส่วนลดที่ส่งมาจากฟอร์ม

---

## 🛡️ ความปลอดภัยและข้อมูลสำคัญ

- ✅ **รหัสผ่านผู้ใช้:** เก็บเป็น Hash ผ่าน `PasswordHelper.Hash()` ทุกจุดที่ตั้งรหัส (Register, AddUser, EditUser, ChangePassword) และตรวจด้วย `PasswordHelper.Verify()` — รหัสแบบเก่าถูกอัปเกรดเป็น Hash ตอนล็อกอิน (ข้อมูลใน `sample_clothing_data.sql` ยังเป็นรหัสธรรมดาได้)
- ⚠️ **Connection string / รหัสผ่านฐานข้อมูล:** ยังอยู่ใน `appsettings.json` และ `Csi402dbContext.OnConfiguring` — ห้ามใช้ค่านี้กับเครื่องจริง ให้ย้ายไป User Secrets / environment variable
- ✅ **สลิปโอนเงิน:**
  - จำกัดนามสกุล (jpg/png/webp) + Content-Type + ขนาด ≤ 5 MB, สุ่มชื่อไฟล์ด้วย GUID
  - เก็บใน `App_Data/slips` (นอก `wwwroot`) และดูผ่าน `/Cart/SlipImage?file=...` ซึ่งตรวจสิทธิ์ (เจ้าของออเดอร์ / Accounting / Admin / Staff) และกัน path traversal
  - ⚠️ สลิปเก่าที่อัปโหลดก่อนแก้ยังอยู่ใน `wwwroot/images/slips` และเปิดผ่าน URL ได้
- ✅ **การตัดสต็อก:** `PlaceOrder` อยู่ใน Transaction และตัดด้วย SQL `UPDATE ... WHERE Stock >= qty` ถ้าตัดไม่ได้จะยกเลิกทั้งออเดอร์
- ✅ **CSRF:** POST ที่เปลี่ยนข้อมูลใช้ `[ValidateAntiForgeryToken]` (form ที่ใช้ `asp-action` + `method="post"` ใส่ token ให้อัตโนมัติ)
- ✅ **IDOR:** action ฝั่งลูกค้าที่รับ `orderId`/`addressId` ตรวจ `UserId` ทุกตัว; `TrackOrder` ต้องใช้เบอร์โทรที่ตรงกัน

---

## 🎨 Style Convention (รูปแบบการเขียนโค้ด)

### C# Controller — ใช้ LINQ Query Syntax
```csharp
// ✅ รูปแบบที่ใช้ในโปรเจกต์นี้ (Query Syntax)
var user = (from u in _db.Users
            where u.Email == email
            select u).FirstOrDefault();

// ❌ ไม่ใช้ Method Syntax แบบนี้
var user = _db.Users.Where(u => u.Email == email).FirstOrDefault();
```

### ส่งสถานะ/ข้อความกลับไป View
```csharp
// ✅ ใช้ ViewBag.Status สำหรับข้อความ Error/Success
ViewBag.Status = "เพิ่มผู้ใช้งานสำเร็จ";

// ✅ ใช้ RedirectToAction พร้อม status query string
return RedirectToAction("UserList", new { status = "ลบสำเร็จ" });
```

### View Layout Declaration
```razor
@{
    ViewData["Title"] = "ชื่อหน้า";
    Layout = "_LayoutAdmin";   // สำหรับหน้า Admin
    // Layout = "_Layout2";    // สำหรับหน้าร้าน (default จาก _ViewStart)
}
```

### Model ที่ส่งเข้า View
```razor
@model IEnumerable<waranon_csi402.Models.User>    // สำหรับ List
@model waranon_csi402.Models.Product               // สำหรับ Single Object
@model waranon_csi402.ViewModels.LoginViewModel    // สำหรับ Form
```

---

## 🚀 วิธีรันโปรเจกต์

### 1. ตั้งค่าฐานข้อมูล
```sql
-- รันใน MySQL Workbench
source d:\67155008\csi402\waranon_csi402\database\CSI402DB_setup.sql

-- จากนั้นเพิ่มข้อมูลตัวอย่างเสื้อผ้า
source d:\67155008\csi402\waranon_csi402\database\sample_clothing_data.sql
```
> หากแก้ schema (เช่นเพิ่มคอลัมน์เวลา, ตาราง `Banners`) ให้แก้ `CSI402DB_setup.sql` และ Model/`Csi402dbContext.cs` ให้ตรงกันเสมอ

### 2. ตรวจสอบ Connection String
```json
// appsettings.json (dev ในเครื่องเท่านั้น — อย่าคอมมิตรหัสผ่านจริง)
"DefaultConnection": "server=localhost;port=3306;database=csi402db;user=<user>;password=<password>;SslMode=None;AllowPublicKeyRetrieval=True"
```

### 3. รันโปรเจกต์
```bash
dotnet run
# หรือกด F5 ใน Visual Studio
```

### 4. URL หลัก
| URL | หน้า | สถานะ |
|---|---|---|
| `/` หรือ `/Home/Index` | หน้าร้านหลัก (Catalog) | ✅ |
| `/Home/ProductDetail/{id}` | รายละเอียดสินค้า | ✅ |
| `/Account/Login` | หน้า Login | ✅ |
| `/Account/Register` | หน้าสมัครสมาชิก | ✅ |
| `/Cart/Index` | ตะกร้าสินค้า | ✅ |
| `/Cart/MyOrders` | ประวัติคำสั่งซื้อของฉัน | ✅ |
| `/Cart/TrackOrder` | ติดตามพัสดุ (OrderId + เบอร์โทร) | ✅ |
| `/Account/Notifications` | การแจ้งเตือน (ทุกบทบาท) | ✅ |
| `/Admin/Index` | Dashboard (Admin) | ✅ |
| `/Admin/UserList` | จัดการผู้ใช้งาน (Admin) | ✅ |
| `/Admin/BannerList` | แบนเนอร์หน้าร้าน (Admin) | ✅ |
| `/Staff/Index` | คิวงานพนักงาน | ✅ |
| `/Staff/StockAlert` | สินค้าใกล้หมด | ✅ |
| `/Accounting/Index` | ระบบบัญชี | ✅ พัฒนาแล้วครบ (VerifySlip, Approve, Reject, FinancialReport) |

---

## 📊 สถานะความคืบหน้า (Progress)

> ตัวเลขเป็นค่าประมาณจาก requirement ใน README ไม่ใช่จากการทดสอบ — อัปเดตเมื่อปิดรายการใน Pending

```
Overall Progress: ~96%

[██████████████████████████████] Database & Models           100%  ✅ (16 ตาราง รวม PromotionProducts, Banners)
[█████████████████████████████░] Admin System                ~95%  ✅ (แบนเนอร์ยังแก้ไขไม่ได้)
[██████████████████████████████] Staff System                100%  ✅ (แจ้งเตือนออเดอร์ใหม่ + สต็อกใกล้หมด)
[██████████████████████████████] Storefront & Cart           100%  ✅ (P4 สินค้าที่กำหนด, P5 เวลาไทย, Transaction ตัดสต็อก, IDOR)
[██████████████████████████████] User Management             100%  ✅ (Profile, Address Book, Password Hash, Auth Guards)
[██████████████████████████████] Accounting System          100%  ✅ (ตรวจสลิป, ปฏิเสธพร้อมเหตุผล, คืนเงิน, รายงาน)
[██████████████████████████████] Automation (Auto-cancel)   100%  ✅ (OrderAutomationService ทุก 1 นาที + แจ้งเตือน)
[███████████████░░░░░░░░░░░░░░░] Notification (In-app + Email) ~50% 🟡 (In-app ครบ, Email ยังไม่มี)
[██████████████████████████████] Customer Features (Wishlist/Review/Refund) 100% ✅
```

---

## 📌 หมายเหตุสำคัญสำหรับ Agent

1. **อย่าลบหรือแก้ไขไฟล์ Lab** (`HomeController` ส่วน Lab9/Lab10, `Views/Home/lab4.cshtml`, `Views/Home/Lab9.cshtml`, `Views/Account/Lab5*.cshtml`) — ไฟล์เหล่านี้เป็นงานแล็บที่ส่งอาจารย์แล้ว

2. **Layout ต้องถูกต้องตามบทบาท:**
   - หน้าร้าน (รวมหน้า Cart) → ใช้ `_Layout2` (default)
   - Admin → ใช้ `_LayoutAdmin`
   - Staff → ใช้ `_LayoutStaff`
   - Accounting → ใช้ `_LayoutAccounting`
   - Admin UserList/AddUser → ใช้ `_LayoutAdmin` (อ่าน `Layout = "_LayoutAdmin"` ใน View)

3. **ตรวจสอบ Build ก่อนเสมอ:** `dotnet build` — ต้องได้ `0 Error(s)`
   และทดสอบ flow ด้วยมือตามลำดับ: สมัคร → สั่งซื้อ → แนบสลิป → บัญชีอนุมัติ → พนักงานแพ็ค/จัดส่ง → ลูกค้ายืนยันรับ (ยังไม่มี test project; ถ้าเพิ่ม ให้เริ่มจากตรรกะคำนวณโปรโมชั่นและ state transition)

4. **ข้อมูลตัวอย่าง:** หากรันแล้วหน้าร้านยังไม่มีสินค้า ให้รัน `sample_clothing_data.sql` ใน MySQL Workbench ก่อน หรือตรวจสอบว่า `DbInitializer.Seed()` ทำงานสำเร็จ

5. **TempData หายหลัง Redirect:** หากต้องการ Peek ข้อมูลในหลายหน้า ต้องใช้ `TempData.Peek("Key")` ไม่ใช่ `TempData["Key"]` (การใช้ `[]` จะลบค่าออกทันที)

6. **ก่อนเริ่มงานใหม่ทุกครั้ง:** เทียบ requirement กับ `README.md` และตรวจโค้ดจริงของหัวข้อที่เกี่ยวข้อง (โดยเฉพาะรายการ "จุดที่ยังต้องตรวจสอบ") ห้ามเชื่อสถานะ ✅ ในไฟล์นี้โดยไม่ยืนยัน

7. **เมื่อทำงานเสร็จ:** อัปเดตไฟล์นี้ให้ตรง (สถานะ, ตาราง DB, URL, Progress) และแก้วันที่ด้านล่าง

---

*อัปเดตล่าสุด: 2026-11-22 | แก้ IDOR ฝั่งลูกค้า + TrackOrder, State Transition ใน Admin/Accounting/Staff, Review เฉพาะ Completed, P5 เวลาไทย, P4 สินค้าที่กำหนด, Transaction ตัดสต็อก, CSRF, สลิปนอก wwwroot, Password Hash, In-app Notification, Refund flow, Banners*
