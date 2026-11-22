using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using waranon_csi402.Models;
using waranon_csi402.Services;

namespace waranon_csi402.Controllers
{
    public class CartController : Controller
    {
        private readonly Csi402dbContext _db;

        public CartController(Csi402dbContext db)
        {
            _db = db;
        }

        // ==========================================
        // Helper: ดึง UserId ของผู้ใช้ปัจจุบัน
        // คืน null เมื่อไม่ได้ล็อกอิน (Auth Guards อาศัยค่า null นี้ทำ Redirect to Login)
        // ==========================================
        private int? GetCurrentUserId()
        {
            var email = TempData.Peek("LoginEmail") as string;
            if (!string.IsNullOrEmpty(email))
            {
                var user = (from u in _db.Users
                            where u.Email == email
                            select u).FirstOrDefault();
                if (user != null) return user.UserId;
            }

            return null;
        }

        // ==========================================
        // ==========================================
        // Helper: คำนวณส่วนลดที่ดีที่สุด (Best Promotion / Coupon)
        // ตามกฎ README: "เลือกลดโปรโมชั่นที่ให้ส่วนลดสูงสุดเพียง 1 สิทธิ์ต่อ 1 คำสั่งซื้อเท่านั้น"
        // ==========================================
        private (decimal discountAmt, int? promoId, int? couponId, string discountDesc, string? couponMsg, bool isCouponValid) CalculateBestDiscount(
            List<Cartitem> cartItems, decimal subtotal, string? couponCode)
        {
            decimal bestDiscount = 0;
            int? bestPromoId = null;
            int? bestCouponId = null;
            string bestDesc = "ไม่มีส่วนลด";
            string? couponMsg = null;
            bool isCouponValid = false;

            // 1. ดึงโปรโมชั่นอัตโนมัติทั้งหมดที่เปิดใช้งาน (IsActive == true)
            var activePromos = (from p in _db.Promotions
                                where p.IsActive == true
                                select p).ToList();

            DateTime now = DateTime.Now;
            DateTime thaiNow = ShopRules.ThaiNow(); // ใช้ตรวจช่วงเวลา Flash Sale

            // รายการสินค้าที่ร้านกำหนดให้ร่วมโปรจับคู่ (P4) แยกตามโปรโมชั่น
            var promoProducts = (from pp in _db.PromotionProducts
                                 select pp).ToList();

            foreach (var p in activePromos)
            {
                if (p.StartDate.HasValue && now < p.StartDate.Value) continue;
                if (p.EndDate.HasValue && now > p.EndDate.Value) continue;

                decimal currentPromoDiscount = 0;
                string pName = p.PromotionName ?? "";

                // สินค้าที่ผูกกับโปรนี้ (มีเฉพาะโปรจับคู่ P4)
                var pairProductIds = (from pp in promoProducts
                                      where pp.PromotionId == p.PromotionId
                                      select pp.ProductId).ToList();
                bool isPairPromo = pairProductIds.Any()
                                   || pName.Contains("จับคู่")
                                   || pName.Contains("2 ชิ้น");

                // ก) Flash Sale (P5): วันที่ 1 ของเดือน 00:01 - 02:00 น. ตามเวลาไทย
                if (ShopRules.IsFlashSalePromotion(p))
                {
                    if (subtotal >= p.MinOrderAmt && ShopRules.IsInFlashSaleWindow(thaiNow))
                    {
                        currentPromoDiscount = (p.DiscountType == "Fixed")
                            ? p.DiscountValue
                            : subtotal * (p.DiscountValue / 100m);
                    }
                }
                // ข) โปรโมชั่นจับคู่ 2 ชิ้น (P4): เฉพาะสินค้าที่ร้านกำหนด ลดจากราคาของสินค้าที่จับคู่ได้
                else if (isPairPromo)
                {
                    // แตกสินค้าที่ร่วมโปรออกเป็นราคาต่อชิ้น เช่น เสื้อ A x2 ➔ [290, 290]
                    var unitPrices = new List<decimal>();
                    foreach (var item in cartItems)
                    {
                        if (pairProductIds.Contains(item.Variant.ProductId))
                        {
                            decimal price = item.Variant.Product.BasePrice + item.Variant.ExtraPrice;
                            for (int i = 0; i < item.Quantity; i++)
                            {
                                unitPrices.Add(price);
                            }
                        }
                    }

                    // จับคู่ได้กี่คู่ (เศษ 1 ชิ้นที่ไม่มีคู่ไม่ได้ส่วนลด)
                    int pairCount = unitPrices.Count / 2;
                    if (pairCount > 0 && subtotal >= p.MinOrderAmt)
                    {
                        // ใช้ชิ้นราคาสูงสุดมาจับคู่ก่อน เพื่อให้ลูกค้าได้ส่วนลดมากที่สุด
                        decimal pairedTotal = unitPrices.OrderByDescending(x => x)
                                                        .Take(pairCount * 2)
                                                        .Sum();

                        currentPromoDiscount = (p.DiscountType == "Fixed")
                            ? p.DiscountValue * pairCount
                            : pairedTotal * (p.DiscountValue / 100m);
                    }
                }
                // ค) โปรโมชั่นเฉพาะหมวดหมู่ (P3 หรือโปรที่ระบุ CategoryId):
                else if (p.CategoryId.HasValue && p.CategoryId.Value > 0)
                {
                    decimal catSubtotal = 0;
                    foreach (var item in cartItems)
                    {
                        if (item.Variant?.Product?.CategoryId == p.CategoryId.Value)
                        {
                            catSubtotal += (item.Variant.Product.BasePrice + item.Variant.ExtraPrice) * item.Quantity;
                        }
                    }

                    if (catSubtotal >= p.MinOrderAmt && catSubtotal > 0)
                    {
                        currentPromoDiscount = (p.DiscountType == "Fixed")
                            ? p.DiscountValue
                            : catSubtotal * (p.DiscountValue / 100m);
                    }
                }
                // ง) โปรโมชั่นยอดสั่งซื้อทั่วไป (P1, P2 หรือโปรโมชั่นที่สร้างขึ้นเอง):
                else
                {
                    if (subtotal >= p.MinOrderAmt)
                    {
                        currentPromoDiscount = (p.DiscountType == "Fixed")
                            ? p.DiscountValue
                            : subtotal * (p.DiscountValue / 100m);
                    }
                }

                if (currentPromoDiscount > bestDiscount)
                {
                    bestDiscount = currentPromoDiscount;
                    bestPromoId = p.PromotionId;
                    bestCouponId = null;
                    bestDesc = $"{p.PromotionName} (ลด ฿{currentPromoDiscount:N0})";
                }
            }

            // 2. ตรวจสอบคูปองส่วนลดที่ลูกค้าระบุ (ถ้ามี)
            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                string cleanCode = couponCode.Trim().ToUpper();
                var coupon = (from c in _db.Coupons
                              where c.CouponCode == cleanCode
                              select c).FirstOrDefault();

                if (coupon == null)
                {
                    couponMsg = $"ไม่พบคูปองส่วนลดรหัส '{cleanCode}'";
                }
                else if (coupon.IsActive != true)
                {
                    couponMsg = $"คูปอง '{cleanCode}' ถูกระงับการใช้งานชั่วคราว";
                }
                else if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt.Value < DateTime.Now)
                {
                    couponMsg = $"คูปอง '{cleanCode}' หมดอายุแล้ว";
                }
                else if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
                {
                    couponMsg = $"คูปอง '{cleanCode}' ถูกใช้จนครบสิทธิ์แล้ว";
                }
                else if (subtotal < coupon.MinOrderAmt)
                {
                    couponMsg = $"คูปอง '{cleanCode}' กำหนดยอดสั่งซื้อขั้นต่ำ ฿{coupon.MinOrderAmt:N0} (ปัจจุบันยอด ฿{subtotal:N0})";
                }
                else
                {
                    decimal couponDiscount = (coupon.DiscountType == "Fixed")
                        ? coupon.DiscountValue
                        : subtotal * (coupon.DiscountValue / 100m);

                    isCouponValid = true;

                    // กฎ README: เลือกลดโปรโมชั่นหรือคูปองที่ให้ส่วนลดสูงสุดเพียง 1 สิทธิ์ต่อ 1 คำสั่งซื้อ
                    if (couponDiscount > bestDiscount)
                    {
                        bestDiscount = couponDiscount;
                        bestPromoId = null;
                        bestCouponId = coupon.CouponId;
                        bestDesc = $"คูปอง {coupon.CouponCode} (ลด ฿{couponDiscount:N0})";
                        couponMsg = $"ใช้คูปอง '{cleanCode}' สำเร็จ ได้รับส่วนลด ฿{couponDiscount:N0}";
                    }
                    else
                    {
                        couponMsg = $"คูปอง '{cleanCode}' ใช้งานได้ แต่ระบบเลือกโปรโมชั่น '{bestDesc}' ซึ่งให้ส่วนลดมากกว่า";
                    }
                }
            }

            return (bestDiscount, bestPromoId, bestCouponId, bestDesc, couponMsg, isCouponValid);
        }

        // ==========================================
        // 1. หน้าตะกร้าสินค้า (Cart Index)
        // ==========================================
        [HttpGet]
        public IActionResult Index(string? couponCode, string? status, string? error)
        {
            ViewBag.Status = status;
            ViewBag.Error = error;

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = (from ci in _db.Cartitems
                                 .Include(ci => ci.Variant).ThenInclude(v => v.Product).ThenInclude(p => p.Category)
                             where ci.UserId == userId
                             orderby ci.AddedAt descending
                             select ci).ToList();

            decimal subtotal = 0;
            int totalCount = 0;
            foreach (var item in cartItems)
            {
                decimal itemPrice = item.Variant.Product.BasePrice + item.Variant.ExtraPrice;
                subtotal += itemPrice * item.Quantity;
                totalCount += item.Quantity;
            }

            var (discountAmt, promoId, couponId, discountDesc, couponMsg, isCouponValid) = CalculateBestDiscount(cartItems, subtotal, couponCode);

            // ส่งฟรีเมื่อสั่งซื้อครบ 500 บาท
            decimal shippingFee = (subtotal >= 500 || subtotal == 0) ? 0 : 50;
            decimal grandTotal = Math.Max(0, subtotal - discountAmt) + shippingFee;

            ViewBag.Subtotal = subtotal;
            ViewBag.TotalCount = totalCount;
            ViewBag.CartCount = totalCount;
            ViewBag.DiscountAmt = discountAmt;
            ViewBag.DiscountDesc = discountDesc;
            ViewBag.AppliedPromoId = promoId;
            ViewBag.AppliedCouponId = couponId;
            ViewBag.CouponCode = couponCode;
            ViewBag.CouponMsg = couponMsg;
            ViewBag.IsCouponValid = isCouponValid;
            ViewBag.ShippingFee = shippingFee;
            ViewBag.GrandTotal = grandTotal;

            return View(cartItems);
        }

        // ==========================================
        // 2. เพิ่มสินค้าลงในตะกร้า (Add To Cart)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int variantId, int quantity = 1)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var variant = (from v in _db.Productvariants
                               .Include(v => v.Product)
                           where v.VariantId == variantId
                           select v).FirstOrDefault();

            if (variant == null || variant.Stock <= 0)
            {
                return RedirectToAction("Index", new { error = "ขออภัย สินค้ารายการนี้หมดแล้วชั่วคราว" });
            }

            var existingItem = (from ci in _db.Cartitems
                                where ci.UserId == userId && ci.VariantId == variantId
                                select ci).FirstOrDefault();

            if (existingItem != null)
            {
                int newQty = existingItem.Quantity + quantity;
                existingItem.Quantity = Math.Min(newQty, variant.Stock);
            }
            else
            {
                var newItem = new Cartitem
                {
                    UserId = userId.Value,
                    VariantId = variantId,
                    Quantity = Math.Min(quantity, variant.Stock),
                    AddedAt = DateTime.Now
                };
                _db.Cartitems.Add(newItem);
            }

            _db.SaveChanges();
            return RedirectToAction("Index", new { status = $"เพิ่ม '{variant.Product.ProductName}' ลงในตะกร้าแล้ว" });
        }

        // ==========================================
        // 3. ปรับปรุงจำนวนสินค้า (Update Quantity)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int cartItemId, int quantity)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var item = (from ci in _db.Cartitems
                            .Include(ci => ci.Variant)
                        where ci.CartItemId == cartItemId && ci.UserId == userId
                        select ci).FirstOrDefault();

            if (item != null)
            {
                if (quantity <= 0)
                {
                    _db.Cartitems.Remove(item);
                }
                else
                {
                    item.Quantity = Math.Min(quantity, item.Variant.Stock);
                }
                _db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // ==========================================
        // 4. ลบสินค้าออกจากตะกร้า (Remove Item)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int cartItemId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var item = (from ci in _db.Cartitems
                        where ci.CartItemId == cartItemId && ci.UserId == userId
                        select ci).FirstOrDefault();

            if (item != null)
            {
                _db.Cartitems.Remove(item);
                _db.SaveChanges();
            }

            return RedirectToAction("Index", new { status = "นำสินค้าออกจากตะกร้าแล้ว" });
        }

        // ==========================================
        // 5. หน้าตรวจสอบและสั่งซื้อ (Checkout)
        // ==========================================
        [HttpGet]
        public IActionResult Checkout(string? couponCode, string? error)
        {
            ViewBag.Error = error;

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = (from ci in _db.Cartitems
                                 .Include(ci => ci.Variant).ThenInclude(v => v.Product).ThenInclude(p => p.Category)
                             where ci.UserId == userId
                             select ci).ToList();

            if (!cartItems.Any())
            {
                return RedirectToAction("Index", new { error = "ไม่มีสินค้าในตะกร้า กรุณาเลือกสินค้าก่อนทำการสั่งซื้อ" });
            }

            // ตรวจสอบสต็อกล่าสุด
            foreach (var ci in cartItems)
            {
                if (ci.Variant.Stock <= 0)
                {
                    _db.Cartitems.Remove(ci);
                    _db.SaveChanges();
                    return RedirectToAction("Index", new { error = $"สินค้า '{ci.Variant.Product.ProductName}' หมดสต็อกแล้ว ระบบได้นำออกจากตะกร้าให้โดยอัตโนมัติ" });
                }
            }

            decimal subtotal = 0;
            foreach (var item in cartItems)
            {
                subtotal += (item.Variant.Product.BasePrice + item.Variant.ExtraPrice) * item.Quantity;
            }

            var (discountAmt, promoId, couponId, discountDesc, couponMsg, isCouponValid) = CalculateBestDiscount(cartItems, subtotal, couponCode);
            decimal shippingFee = subtotal >= 500 ? 0 : 50;
            decimal grandTotal = Math.Max(0, subtotal - discountAmt) + shippingFee;

            var addresses = (from a in _db.Addresses
                             where a.UserId == userId
                             orderby a.IsDefault descending, a.AddressId descending
                             select a).ToList();

            ViewBag.Subtotal = subtotal;
            ViewBag.DiscountAmt = discountAmt;
            ViewBag.DiscountDesc = discountDesc;
            ViewBag.AppliedPromoId = promoId;
            ViewBag.AppliedCouponId = couponId;
            ViewBag.CouponCode = couponCode;
            ViewBag.CouponMsg = couponMsg;
            ViewBag.IsCouponValid = isCouponValid;
            ViewBag.ShippingFee = shippingFee;
            ViewBag.GrandTotal = grandTotal;
            ViewBag.Addresses = addresses;

            return View(cartItems);
        }

        // ==========================================
        // 6. ยืนยันการสั่งซื้อ (Place Order)
        // ตัดสต็อกทันที + สร้าง Order ในสถานะ Pending Payment
        // ==========================================
        [HttpGet]
        public IActionResult PlaceOrder()
        {
            return RedirectToAction("Checkout");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlaceOrder(
            int? selectedAddressId,
            string? newReceiverName,
            string? newPhone,
            string? newAddressLine,
            string? newDistrict,
            string? newProvince,
            string? newPostalCode,
            string? paymentMethod,
            string? couponCode)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = (from ci in _db.Cartitems
                                 .Include(ci => ci.Variant).ThenInclude(v => v.Product)
                             where ci.UserId == userId
                             select ci).ToList();

            if (!cartItems.Any())
            {
                return RedirectToAction("Index", new { error = "ไม่มีสินค้าในตะกร้า" });
            }

            // 1. ตรวจสอบสต็อกอีกครั้งก่อนตัดจริง
            foreach (var ci in cartItems)
            {
                if (ci.Quantity > ci.Variant.Stock)
                {
                    return RedirectToAction("Checkout", new { error = $"ขออภัย สินค้า '{ci.Variant.Product.ProductName}' ({ci.Variant.Color}/{ci.Variant.Size}) มีสต็อกคงเหลือไม่เพียงพอ (เหลือ {ci.Variant.Stock} ชิ้น)" });
                }
            }

            try
            {
                // ทำทุกขั้นตอนใน Transaction เดียว: ถ้ามีขั้นไหนพัง ข้อมูลทั้งหมดจะถูกยกเลิก (Rollback)
                // ไม่ต้องสั่ง Rollback เอง — ถ้าออกจาก try โดยยังไม่ Commit ระบบจะยกเลิกให้อัตโนมัติ
                using var transaction = _db.Database.BeginTransaction();

                // 2. จัดการที่อยู่จัดส่ง
                int finalAddressId = 0;
                if (selectedAddressId.HasValue && selectedAddressId.Value > 0)
                {
                    // IDOR: ที่อยู่ที่เลือกต้องเป็นของผู้ใช้คนนี้เท่านั้น
                    var myAddr = (from a in _db.Addresses
                                  where a.AddressId == selectedAddressId.Value && a.UserId == userId
                                  select a).FirstOrDefault();
                    if (myAddr == null)
                    {
                        return RedirectToAction("Checkout", new { error = "ไม่พบที่อยู่จัดส่งที่เลือก กรุณาเลือกใหม่อีกครั้ง" });
                    }
                    finalAddressId = myAddr.AddressId;
                }
                else if (!string.IsNullOrWhiteSpace(newReceiverName) && !string.IsNullOrWhiteSpace(newPhone) && !string.IsNullOrWhiteSpace(newAddressLine))
                {
                    var newAddr = new Address
                    {
                        UserId = userId.Value,
                        ReceiverName = newReceiverName.Trim(),
                        Phone = newPhone.Trim(),
                        AddressLine = newAddressLine.Trim(),
                        District = !string.IsNullOrWhiteSpace(newDistrict) ? newDistrict.Trim() : "เมือง",
                        Province = !string.IsNullOrWhiteSpace(newProvince) ? newProvince.Trim() : "กรุงเทพมหานคร",
                        PostalCode = !string.IsNullOrWhiteSpace(newPostalCode) ? newPostalCode.Trim() : "10110",
                        IsDefault = false
                    };
                    _db.Addresses.Add(newAddr);
                    _db.SaveChanges();
                    finalAddressId = newAddr.AddressId;
                }
                else
                {
                    // ค้นหาที่อยู่เดิมของผู้ใช้ หรือสร้างที่อยู่เริ่มต้นอัตโนมัติ
                    var existingAddr = (from a in _db.Addresses where a.UserId == userId select a).FirstOrDefault();
                    if (existingAddr != null)
                    {
                        finalAddressId = existingAddr.AddressId;
                    }
                    else
                    {
                        var autoAddr = new Address
                        {
                            UserId = userId.Value,
                            ReceiverName = TempData.Peek("Name")?.ToString() ?? "คุณลูกค้า",
                            Phone = TempData.Peek("PhoneNumber")?.ToString() ?? "0812345678",
                            AddressLine = "99/9 ถ.สุขุมวิท",
                            District = "คลองเตย",
                            Province = "กรุงเทพมหานคร",
                            PostalCode = "10110",
                            IsDefault = true
                        };
                        _db.Addresses.Add(autoAddr);
                        _db.SaveChanges();
                        finalAddressId = autoAddr.AddressId;
                    }
                }

                // 3. คำนวณราคาและส่วนลด
                decimal subtotal = 0;
                foreach (var item in cartItems)
                {
                    subtotal += (item.Variant.Product.BasePrice + item.Variant.ExtraPrice) * item.Quantity;
                }

                var (discountAmt, promoId, couponId, _, _, _) = CalculateBestDiscount(cartItems, subtotal, couponCode);
                decimal shippingFee = subtotal >= 500 ? 0 : 50;
                decimal grandTotal = Math.Max(0, subtotal - discountAmt) + shippingFee;

                // 4. แปลง PaymentMethod ให้ตรงกับ MySQL ENUM('Bank Transfer','Credit Card','PromptPay')
                string safePaymentMethod = "PromptPay";
                if (!string.IsNullOrWhiteSpace(paymentMethod))
                {
                    if (paymentMethod.Contains("Credit", StringComparison.OrdinalIgnoreCase))
                        safePaymentMethod = "Credit Card";
                    else if (paymentMethod.Contains("Bank", StringComparison.OrdinalIgnoreCase) || paymentMethod.Contains("Transfer", StringComparison.OrdinalIgnoreCase))
                        safePaymentMethod = "Bank Transfer";
                    else
                        safePaymentMethod = "PromptPay";
                }

                // สร้าง Order ใหม่
                var newOrder = new Order
                {
                    UserId = userId.Value,
                    AddressId = finalAddressId,
                    Subtotal = subtotal,
                    ShippingFee = shippingFee,
                    DiscountAmt = discountAmt,
                    TotalAmount = grandTotal,
                    PromotionId = promoId,
                    CouponId = couponId,
                    Status = "Pending Payment", // รอชำระเงิน
                    PaymentMethod = safePaymentMethod,
                    OrderedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _db.Orders.Add(newOrder);
                _db.SaveChanges(); // เพื่อให้ได้ newOrder.OrderId

                // 5. สร้าง OrderItems และตัดสต็อกสินค้าในระดับ SKU ทันที (Auto-deduct)
                foreach (var item in cartItems)
                {
                    decimal unitPrice = item.Variant.Product.BasePrice + item.Variant.ExtraPrice;
                    var orderItem = new Orderitem
                    {
                        OrderId = newOrder.OrderId,
                        VariantId = item.VariantId,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice
                    };
                    _db.Orderitems.Add(orderItem);

                    // ตัดสต็อกด้วยคำสั่งเดียว "ตัดเฉพาะเมื่อสต็อกยังพอ"
                    // ป้องกันกรณีลูกค้า 2 คนสั่ง SKU เดียวกันพร้อมกันแล้วสต็อกติดลบ
                    int rowsUpdated = _db.Database.ExecuteSqlInterpolated(
                        $"UPDATE productvariants SET Stock = Stock - {item.Quantity} WHERE VariantID = {item.VariantId} AND Stock >= {item.Quantity}");

                    if (rowsUpdated == 0)
                    {
                        // สต็อกไม่พอแล้ว (มีคนสั่งตัดหน้า) ➔ ยกเลิกทั้งออเดอร์
                        return RedirectToAction("Checkout", new { error = $"ขออภัย สินค้า '{item.Variant.Product.ProductName}' ({item.Variant.Color}/{item.Variant.Size}) เพิ่งหมดสต็อก กรุณาปรับจำนวนสินค้าในตะกร้า" });
                    }

                    // แจ้งเตือนพนักงานเมื่อสต็อกเหลือน้อย (≤ 10) หลังตัดสต็อกครั้งนี้
                    int stockBefore = item.Variant.Stock;
                    int stockAfter = stockBefore - item.Quantity;
                    if (stockBefore > 10 && stockAfter <= 10)
                    {
                        NotificationHelper.NotifyRole(_db, "Staff", "สินค้าใกล้หมดสต็อก",
                            $"'{item.Variant.Product.ProductName}' ({item.Variant.Color}/{item.Variant.Size}) เหลือ {stockAfter} ชิ้น");
                    }
                }

                // ถ้าใช้คูปอง ให้อัปเดตจำนวนครั้งที่ใช้
                if (couponId.HasValue)
                {
                    var usedCoupon = (from c in _db.Coupons where c.CouponId == couponId.Value select c).FirstOrDefault();
                    if (usedCoupon != null)
                    {
                        usedCoupon.UsedCount += 1;
                    }
                }

                // 6. เคลียร์ตะกร้าสินค้าของผู้ใช้นี้
                _db.Cartitems.RemoveRange(cartItems);

                // ออเดอร์ Flash Sale ต้องแนบสลิปภายใน 1 ชม. ส่วนออเดอร์ปกติ 24 ชม.
                var appliedPromo = (from pr in _db.Promotions where pr.PromotionId == promoId select pr).FirstOrDefault();
                string payDeadline = ShopRules.IsFlashSalePromotion(appliedPromo) ? "1 ชั่วโมง" : "24 ชั่วโมง";

                NotificationHelper.Notify(_db, userId.Value, "สั่งซื้อสำเร็จ",
                    $"คำสั่งซื้อ #{newOrder.OrderId} ยอด ฿{grandTotal:N0} กรุณาแนบสลิปชำระเงินภายใน {payDeadline}");

                _db.SaveChanges();
                transaction.Commit(); // ยืนยันข้อมูลทั้งหมดลง DB

                return RedirectToAction("OrderSuccess", new { orderId = newOrder.OrderId });
            }
            catch (Exception ex)
            {
                return RedirectToAction("Checkout", new { error = "เกิดข้อผิดพลาดในการบันทึกคำสั่งซื้อ: " + ex.Message });
            }
        }

        // ==========================================
        // 7. หน้ายืนยันคำสั่งซื้อสำเร็จ & ข้อมูลชำระเงิน (Order Success)
        // ==========================================
        [HttpGet]
        public IActionResult OrderSuccess(int orderId, string? error)
        {
            ViewBag.Error = error; // ข้อความผิดพลาดจากการแนบสลิป (ถ้ามี)

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // IDOR: ดึงเฉพาะออเดอร์ที่เป็นของผู้ใช้ปัจจุบันเท่านั้น
            var order = (from o in _db.Orders
                             .Include(o => o.Address)
                             .Include(o => o.Orderitems).ThenInclude(oi => oi.Variant).ThenInclude(v => v.Product)
                         where o.OrderId == orderId && o.UserId == userId
                         select o).FirstOrDefault();

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }

        // ==========================================
        // 8. อัปโหลดสลิปชำระเงิน (Upload Slip)
        // รองรับทั้ง Pending Payment → Verifying Slip
        // และ Invalid Slip → Verifying Slip (แนบสลิปใหม่)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UploadSlip(int orderId, IFormFile? slipImage)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // IDOR: ดึงเฉพาะออเดอร์ที่เป็นของผู้ใช้ปัจจุบันเท่านั้น
            var order = (from o in _db.Orders
                         where o.OrderId == orderId && o.UserId == userId
                         select o).FirstOrDefault();
            if (order == null)
            {
                return RedirectToAction("MyOrders");
            }

            // ตรวจว่าสถานะอนุญาตให้แนบสลิปได้ (Pending Payment หรือ Invalid Slip เท่านั้น)
            if (order.Status != "Pending Payment" && order.Status != "Invalid Slip")
            {
                return RedirectToAction("OrderDetail", new { id = orderId, status = "ไม่สามารถแนบสลิปได้ในสถานะนี้" });
            }

            if (slipImage != null && slipImage.Length > 0)
            {
                // ตรวจประเภทไฟล์ (อนุญาตเฉพาะรูปภาพ)
                var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                string ext = Path.GetExtension(slipImage.FileName).ToLowerInvariant();
                if (!allowedExt.Contains(ext))
                {
                    return RedirectToAction("OrderSuccess", new { orderId = orderId, error = "กรุณาแนบไฟล์รูปภาพ (.jpg, .png, .webp) เท่านั้น" });
                }

                // ตรวจชนิดไฟล์ที่เบราว์เซอร์ส่งมา (Content-Type) ต้องเป็นรูปภาพ
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
                if (!allowedTypes.Contains(slipImage.ContentType.ToLowerInvariant()))
                {
                    return RedirectToAction("OrderSuccess", new { orderId = orderId, error = "ไฟล์ที่แนบไม่ใช่รูปภาพ กรุณาแนบสลิปเป็นไฟล์ .jpg, .png หรือ .webp" });
                }

                // ตรวจขนาดไฟล์ไม่เกิน 5 MB
                if (slipImage.Length > 5 * 1024 * 1024)
                {
                    return RedirectToAction("OrderSuccess", new { orderId = orderId, error = "ขนาดไฟล์ต้องไม่เกิน 5 MB" });
                }

                // เก็บสลิปไว้นอก wwwroot ➔ เปิดผ่าน URL ตรงๆ ไม่ได้ ต้องดูผ่าน action SlipImage ที่ตรวจสิทธิ์
                string slipsFolder = GetSlipsFolder();
                if (!Directory.Exists(slipsFolder))
                {
                    Directory.CreateDirectory(slipsFolder);
                }

                // ใช้ GUID สุ่มชื่อไฟล์ ป้องกันการเดาชื่อ
                string fileName = $"slip_{orderId}_{Guid.NewGuid()}{ext}";
                string filePath = Path.Combine(slipsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    slipImage.CopyTo(stream);
                }

                bool isReupload = order.Status == "Invalid Slip";

                order.SlipImageUrl = $"/Cart/SlipImage?file={fileName}";
                order.Status = "Verifying Slip"; // ทั้งแนบครั้งแรกและแนบใหม่ → รอตรวจสอบสลิป
                order.UpdatedAt = DateTime.Now;

                // แจ้งเตือนลูกค้า และฝ่ายบัญชีว่ามีสลิปรอตรวจ
                NotificationHelper.Notify(_db, order.UserId, "แนบสลิปสำเร็จ",
                    $"ได้รับสลิปของคำสั่งซื้อ #{orderId} แล้ว ฝ่ายบัญชีกำลังตรวจสอบยอดเงิน");
                NotificationHelper.NotifyRole(_db, "Accounting", isReupload ? "มีสลิปแนบใหม่รอตรวจ" : "มีสลิปใหม่รอตรวจ",
                    $"คำสั่งซื้อ #{orderId} ยอด ฿{order.TotalAmount:N0} รอตรวจสอบสลิป");

                _db.SaveChanges();

                return RedirectToAction("OrderDetail", new { id = orderId, status = "แนบสลิปชำระเงินสำเร็จแล้ว ทางร้านจะเร่งตรวจสอบยอดเงินโดยเร็วครับ" });
            }

            return RedirectToAction("OrderSuccess", new { orderId = orderId, error = "กรุณาเลือกไฟล์สลิปหลักฐานการโอนเงิน" });
        }

        // โฟลเดอร์เก็บสลิป (อยู่นอก wwwroot)
        private static string GetSlipsFolder()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "slips");
        }

        // ==========================================
        // 8.1 แสดงรูปสลิป (เฉพาะผู้มีสิทธิ์)
        // ดูได้: เจ้าของออเดอร์, Accounting, Admin, Staff
        // ==========================================
        [HttpGet]
        public IActionResult SlipImage(string? file)
        {
            // กันการใส่ path แปลกๆ เช่น ../../appsettings.json
            if (string.IsNullOrEmpty(file) || file != Path.GetFileName(file))
            {
                return NotFound();
            }

            // หาออเดอร์ที่เป็นเจ้าของไฟล์สลิปนี้
            string slipUrl = $"/Cart/SlipImage?file={file}";
            var order = (from o in _db.Orders where o.SlipImageUrl == slipUrl select o).FirstOrDefault();
            if (order == null)
            {
                return NotFound();
            }

            // ตรวจสิทธิ์
            string? role = TempData.Peek("Role")?.ToString();
            bool isBackOffice = role == "Accounting" || role == "Admin" || role == "Staff";
            bool isOwner = GetCurrentUserId() == order.UserId;
            if (!isBackOffice && !isOwner)
            {
                return RedirectToAction("Login", "Account");
            }

            string filePath = Path.Combine(GetSlipsFolder(), file);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            string ext = Path.GetExtension(file).ToLowerInvariant();
            string contentType = ext == ".png" ? "image/png"
                               : ext == ".webp" ? "image/webp"
                               : "image/jpeg";
            return PhysicalFile(filePath, contentType);
        }

        // ==========================================
        // 9. รายการประวัติคำสั่งซื้อของฉัน (My Orders)
        // ==========================================
        [HttpGet]
        public IActionResult MyOrders(string? status)
        {
            ViewBag.Status = status;

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = (from o in _db.Orders
                              .Include(o => o.Address)
                              .Include(o => o.Orderitems).ThenInclude(oi => oi.Variant).ThenInclude(v => v.Product)
                          where o.UserId == userId
                          orderby o.OrderedAt descending
                          select o).ToList();

            return View(orders);
        }

        // ==========================================
        // 10. หน้ารายละเอียดและติดตามสถานะคำสั่งซื้อ (Order Detail)
        // IDOR: ตรวจว่าออเดอร์เป็นของผู้ใช้ปัจจุบันเสมอ
        // ==========================================
        [HttpGet]
        public IActionResult OrderDetail(int? id, string? status)
        {
            if (id == null)
            {
                return RedirectToAction("MyOrders");
            }

            ViewBag.Status = status;

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = (from o in _db.Orders
                             .Include(o => o.User)
                             .Include(o => o.Address)
                             .Include(o => o.Promotion)
                             .Include(o => o.Coupon)
                             .Include(o => o.Orderitems).ThenInclude(oi => oi.Variant).ThenInclude(v => v.Product)
                         where o.OrderId == id && o.UserId == userId
                         select o).FirstOrDefault();

            if (order == null)
            {
                return RedirectToAction("MyOrders");
            }

            return View(order);
        }

        // ==========================================
        // 11. ลูกค้ายืนยันได้รับสินค้าแล้ว (Confirm Delivery)
        // เปลี่ยนสถานะ Shipped ➔ Completed
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmDelivery(int orderId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = (from o in _db.Orders
                         where o.OrderId == orderId && o.UserId == userId
                         select o).FirstOrDefault();

            if (order != null && order.Status == "Shipped")
            {
                order.Status = "Completed";
                order.UpdatedAt = DateTime.Now;
                _db.SaveChanges();

                return RedirectToAction("OrderDetail", new { id = orderId, status = "ยืนยันการรับสินค้าสำเร็จ ขอบคุณที่ไว้วางใจเสื้อผ้า.com ครับ!" });
            }

            return RedirectToAction("OrderDetail", new { id = orderId });
        }

        // ==========================================
        // 12. ลูกค้ายกเลิกคำสั่งซื้อ (Cancel Order & Auto-Restock)
        // ยกเลิกได้เฉพาะตอนยังไม่ชำระเงิน พร้อมคืนสต็อกสินค้าอัตโนมัติ
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder(int orderId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = (from o in _db.Orders
                             .Include(o => o.Orderitems).ThenInclude(oi => oi.Variant)
                         where o.OrderId == orderId && o.UserId == userId
                         select o).FirstOrDefault();

            if (order != null && (order.Status == "Pending Payment" || order.Status == "Invalid Slip"))
            {
                order.Status = "Cancelled";
                order.UpdatedAt = DateTime.Now;

                // คืนสต็อกสินค้ากลับเข้าคลังอัตโนมัติ (Restock)
                foreach (var item in order.Orderitems)
                {
                    if (item.Variant != null)
                    {
                        item.Variant.Stock += item.Quantity;
                    }
                }

                NotificationHelper.Notify(_db, order.UserId, "ยกเลิกคำสั่งซื้อแล้ว",
                    $"คำสั่งซื้อ #{orderId} ถูกยกเลิกตามคำขอของคุณ");

                _db.SaveChanges();
                return RedirectToAction("OrderDetail", new { id = orderId, status = "ยกเลิกคำสั่งซื้อและคืนสต็อกสินค้าเรียบร้อยแล้ว" });
            }

            return RedirectToAction("OrderDetail", new { id = orderId, status = "ไม่สามารถยกเลิกคำสั่งซื้อในสถานะนี้ได้" });
        }

        // ==========================================
        // 12.1 ลูกค้าขอคืนสินค้า / คืนเงิน (Request Refund)
        // ส่งคำขอไปให้ฝ่ายบัญชีพิจารณา — สถานะยังไม่เปลี่ยน จนกว่าฝ่ายบัญชีจะกดคืนเงิน
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestRefund(int orderId, string? reason)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = (from o in _db.Orders
                         where o.OrderId == orderId && o.UserId == userId
                         select o).FirstOrDefault();
            if (order == null)
            {
                return RedirectToAction("MyOrders");
            }

            // ขอคืนได้เฉพาะออเดอร์ที่ชำระเงินแล้ว
            var refundableStatuses = new[] { "Paid", "Processing", "Shipped", "Completed" };
            if (!refundableStatuses.Contains(order.Status))
            {
                return RedirectToAction("OrderDetail", new { id = orderId, status = "คำสั่งซื้อนี้ไม่สามารถขอคืนเงินได้ในสถานะปัจจุบัน" });
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return RedirectToAction("OrderDetail", new { id = orderId, status = "กรุณาระบุเหตุผลที่ต้องการคืนสินค้า/คืนเงิน" });
            }

            NotificationHelper.NotifyRole(_db, "Accounting", "มีคำขอคืนเงิน",
                $"คำสั่งซื้อ #{orderId} (ยอด ฿{order.TotalAmount:N0}) ขอคืนเงิน เหตุผล: {reason.Trim()}");
            NotificationHelper.Notify(_db, order.UserId, "ส่งคำขอคืนเงินแล้ว",
                $"ฝ่ายบัญชีได้รับคำขอคืนเงินของคำสั่งซื้อ #{orderId} แล้ว และจะติดต่อกลับโดยเร็ว");
            _db.SaveChanges();

            return RedirectToAction("OrderDetail", new { id = orderId, status = "ส่งคำขอคืนสินค้า/คืนเงินเรียบร้อยแล้ว ฝ่ายบัญชีจะติดต่อกลับโดยเร็วครับ" });
        }

        // ==========================================
        // 13. ติดตามสถานะคำสั่งซื้อ & พัสดุ (Public Track Order)
        // ค้นหาได้ทันทีด้วย OrderId (และเบอร์โทร) โดยไม่ต้องล็อกอิน
        // ==========================================
        [HttpGet]
        public IActionResult TrackOrder(int? orderId, string? phone)
        {
            ViewBag.Searched = (orderId != null || !string.IsNullOrWhiteSpace(phone));
            ViewBag.OrderId = orderId;
            ViewBag.Phone = phone;

            // ต้องกรอกทั้ง OrderId และเบอร์โทร เพื่อป้องกันคนอื่นไล่เลขออเดอร์ดูข้อมูลลูกค้า
            if (orderId == null || string.IsNullOrWhiteSpace(phone))
            {
                return View(null);
            }

            string p = phone.Trim();

            // เบอร์โทรต้องตรงกับเบอร์ผู้รับ หรือเบอร์ของเจ้าของบัญชีแบบตรงทุกตัว
            var order = (from o in _db.Orders
                             .Include(o => o.Address)
                             .Include(o => o.User)
                             .Include(o => o.Orderitems).ThenInclude(oi => oi.Variant).ThenInclude(v => v.Product)
                         where o.OrderId == orderId
                               && (o.Address.Phone == p || o.User.PhoneNumber == p)
                         select o).FirstOrDefault();

            return View(order);
        }
    }
}
