using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace waranon_csi402.Models
{
    public static class DbInitializer
    {
        public static void Seed(Csi402dbContext db)
        {
            Category? catTshirt = null;

            // 1. เพิ่มหมวดหมู่และสินค้าเสื้อผ้าเริ่มต้น หากยังไม่มี
            if (!db.Categories.Any())
            {
                catTshirt = new Category { CategoryName = "เสื้อยืด (T-Shirts)", Description = "เสื้อยืดแฟชั่น คอกลม คอวี โอเวอร์ไซส์ ผ้า Cotton 100%" };
                var catShirt = new Category { CategoryName = "เสื้อเชิ้ต (Shirts)", Description = "เสื้อเชิ้ตทำงานและเสื้อเชิ้ตลำลอง ทั้งแขนสั้นและแขนยาว" };
                var catPolo = new Category { CategoryName = "เสื้อโปโล (Polo Shirts)", Description = "เสื้อโปโลคลาสสิก ผ้าปีเก้ ทรงสวย ดูดีทั้งทางการและลำลอง" };
                var catHoodie = new Category { CategoryName = "เสื้อฮู้ด & สเวตเตอร์", Description = "เสื้อกันหนาวมีฮู้ด สเวตเตอร์ไหมพรม ผ้านุ่ม อบอุ่น" };
                var catJacket = new Category { CategoryName = "เสื้อแจ็กเก็ต & สูทลำลอง", Description = "เสื้อคลุม เสื้อแจ็กเก็ตยีนส์ และเบลเซอร์แฟชั่น" };

                db.Categories.AddRange(catTshirt, catShirt, catPolo, catHoodie, catJacket);
                db.SaveChanges();

            // 2. เพิ่มสินค้าเสื้อผ้า
            var p1 = new Product
            {
                CategoryId = catTshirt.CategoryId,
                ProductName = "เสื้อยืด Oversize มินิมอล Cotton 100%",
                Description = "เสื้อยืดทรงโอเวอร์ไซส์ สไตล์เกาหลี ผลิตจากผ้าฝ้าย Comb 100% เนื้อนุ่ม ไม่ร้อน ระบายอากาศได้ดีเยี่ยม เหมาะกับใส่ในชีวิตประจำวัน",
                BasePrice = 290.00m,
                ImageUrl = "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?w=800&auto=format&fit=crop&q=80",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var p2 = new Product
            {
                CategoryId = catTshirt.CategoryId,
                ProductName = "เสื้อยืดสกรีนลายกราฟิกสตรีท Urban Vibe",
                Description = "เสื้อยืดสกรีนลายกราฟิกด้านหลัง สไตล์สตรีทแฟชั่น สีสกรีนคมชัดไม่หลุดลอก ซักเครื่องได้ ทรง Regular สวมใส่ได้ทั้งชายและหญิง",
                BasePrice = 350.00m,
                ImageUrl = "https://images.unsplash.com/photo-1503342217505-b0a15ec3261c?w=800&auto=format&fit=crop&q=80",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var p3 = new Product
            {
                CategoryId = catShirt.CategoryId,
                ProductName = "เสื้อเชิ้ตแขนยาว Oxford Slim Fit",
                Description = "เสื้อเชิ้ตผ้า Oxford ทอละเอียด เนื้อผ้าระบายอากาศได้ดี รีดง่าย ยับยาก ทรงเข้ารูปกำลังดี ใส่ทำงานหรือใส่เที่ยวก็ดูดีมีระดับ",
                BasePrice = 490.00m,
                ImageUrl = "https://images.unsplash.com/photo-1596755094514-f87e34085b2c?w=800&auto=format&fit=crop&q=80",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var p4 = new Product
            {
                CategoryId = catShirt.CategoryId,
                ProductName = "เสื้อเชิ้ตแขนสั้น Cuban Collar สไตล์รีสอร์ท",
                Description = "เสื้อเชิ้ตคอปกบัวสไตล์คิวบา ผ้าเรยอนพริ้วไหว สวมใส่สบาย ระบายอากาศดีเยี่ยม เหมาะกับวันพักผ่อนและอากาศเมืองไทย",
                BasePrice = 420.00m,
                ImageUrl = "https://images.unsplash.com/photo-1598033129183-c4f50c736f10?w=800&auto=format&fit=crop&q=80",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var p5 = new Product
            {
                CategoryId = catPolo.CategoryId,
                ProductName = "เสื้อโปโล Cotton Pique Classic Fit",
                Description = "เสื้อโปโลทรงคลาสสิก ทอจากผ้าคอตตอนปีเก้ ปกเสื้ออยู่ทรงสวย ไม่ย้วย ปลายแขนจั๊มเบาๆ ให้ลุคเรียบหรู กึ่งทางการ",
                BasePrice = 390.00m,
                ImageUrl = "https://images.unsplash.com/photo-1581655353564-df123a1eb820?w=800&auto=format&fit=crop&q=80",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var p6 = new Product
            {
                CategoryId = catHoodie.CategoryId,
                ProductName = "เสื้อฮู้ด Fleece Hoodie ผ้านุ่มพิเศษ สไตล์สตรีท",
                Description = "เสื้อกันหนาวมีฮู้ด ด้านในบุผ้าฟลีซสัมผัสนุ่ม อบอุ่น สบายตัว มีกระเป๋าจิงโจ้ด้านหน้าและเชือกรูดฮู้ด หนานุ่มกำลังดี",
                BasePrice = 690.00m,
                ImageUrl = "https://images.unsplash.com/photo-1556905055-8f358a7a47b2?w=800&auto=format&fit=crop&q=80",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var p7 = new Product
            {
                CategoryId = catJacket.CategoryId,
                ProductName = "เสื้อแจ็กเก็ตยีนส์ Vintage Denim Jacket",
                Description = "เสื้อแจ็กเก็ตยีนส์ทรงคลาสสิก ฟอกสีสไตล์วินเทจ ตัดเย็บด้วยผ้ายีนส์หนา 14 Oz มีกระเป๋าหน้า 2 ช่อง คลุมทับได้ทุกชุด เพิ่มความเท่",
                BasePrice = 890.00m,
                ImageUrl = "https://images.unsplash.com/photo-1576995853123-5a10305d93c0?w=800&auto=format&fit=crop&q=80",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Products.AddRange(p1, p2, p3, p4, p5, p6, p7);
            db.SaveChanges();

            // 3. เพิ่มตัวเลือกสินค้า (ProductVariants)
            db.Productvariants.AddRange(
                // p1: เสื้อยืด Oversize
                new Productvariant { ProductId = p1.ProductId, Color = "สีขาว (White)", Size = "M", Stock = 30, ExtraPrice = 0 },
                new Productvariant { ProductId = p1.ProductId, Color = "สีขาว (White)", Size = "L", Stock = 25, ExtraPrice = 0 },
                new Productvariant { ProductId = p1.ProductId, Color = "สีดำ (Black)", Size = "M", Stock = 20, ExtraPrice = 0 },
                new Productvariant { ProductId = p1.ProductId, Color = "สีดำ (Black)", Size = "XL", Stock = 15, ExtraPrice = 20 },

                // p2: เสื้อยืดกราฟิก
                new Productvariant { ProductId = p2.ProductId, Color = "สีดำ (Black)", Size = "M", Stock = 18, ExtraPrice = 0 },
                new Productvariant { ProductId = p2.ProductId, Color = "สีดำ (Black)", Size = "L", Stock = 22, ExtraPrice = 0 },

                // p3: เสื้อเชิ้ต Oxford
                new Productvariant { ProductId = p3.ProductId, Color = "สีฟ้าคราม (Light Blue)", Size = "S", Stock = 15, ExtraPrice = 0 },
                new Productvariant { ProductId = p3.ProductId, Color = "สีฟ้าคราม (Light Blue)", Size = "M", Stock = 20, ExtraPrice = 0 },
                new Productvariant { ProductId = p3.ProductId, Color = "สีขาว (White)", Size = "L", Stock = 15, ExtraPrice = 0 },

                // p4: เสื้อเชิ้ตคิวบา
                new Productvariant { ProductId = p4.ProductId, Color = "สีกรมท่า (Navy)", Size = "M", Stock = 15, ExtraPrice = 0 },
                new Productvariant { ProductId = p4.ProductId, Color = "สีกรมท่า (Navy)", Size = "L", Stock = 12, ExtraPrice = 0 },

                // p5: เสื้อโปโล
                new Productvariant { ProductId = p5.ProductId, Color = "สีกรมท่า (Navy)", Size = "M", Stock = 20, ExtraPrice = 0 },
                new Productvariant { ProductId = p5.ProductId, Color = "สีดำ (Black)", Size = "L", Stock = 18, ExtraPrice = 0 },
                new Productvariant { ProductId = p5.ProductId, Color = "สีขาว (White)", Size = "XL", Stock = 14, ExtraPrice = 20 },

                // p6: เสื้อฮู้ด
                new Productvariant { ProductId = p6.ProductId, Color = "สีเทา (Heather Grey)", Size = "M", Stock = 15, ExtraPrice = 0 },
                new Productvariant { ProductId = p6.ProductId, Color = "สีเทา (Heather Grey)", Size = "L", Stock = 10, ExtraPrice = 0 },
                new Productvariant { ProductId = p6.ProductId, Color = "สีดำ (Black)", Size = "XL", Stock = 12, ExtraPrice = 30 },

                // p7: เสื้อแจ็กเก็ตยีนส์
                new Productvariant { ProductId = p7.ProductId, Color = "สียีนส์เข้ม (Dark Denim)", Size = "M", Stock = 10, ExtraPrice = 0 },
                new Productvariant { ProductId = p7.ProductId, Color = "สียีนส์เข้ม (Dark Denim)", Size = "L", Stock = 8, ExtraPrice = 0 }
            );

            db.SaveChanges();
            } // ปิด if (!db.Categories.Any())

            // หาหมวดหมู่เสื้อยืดเพื่อใช้ผูกกับโปรโมชั่น P3
            if (catTshirt == null)
            {
                catTshirt = db.Categories.FirstOrDefault(c => c.CategoryName.Contains("เสื้อยืด"));
            }

            // 4. เพิ่มโปรโมชั่นเริ่มต้นตามข้อกำหนดใน README.md (P1 - P5)
            if (!db.Promotions.Any())
            {
                db.Promotions.AddRange(
                    new Promotion
                    {
                        PromotionName = "P1: ช้อปครบ 1,000 บาท รับส่วนลด 10%",
                        DiscountType = "Percent",
                        DiscountValue = 10,
                        MinOrderAmt = 1000,
                        IsActive = true
                    },
                    new Promotion
                    {
                        PromotionName = "P2: ช้อปครบ 3,000 บาท รับส่วนลด 15%",
                        DiscountType = "Percent",
                        DiscountValue = 15,
                        MinOrderAmt = 3000,
                        IsActive = true
                    },
                    new Promotion
                    {
                        PromotionName = "P3: หมวดหมู่เสื้อยืด ซื้อครบ 500 บาท ลด 15%",
                        DiscountType = "Percent",
                        DiscountValue = 15,
                        MinOrderAmt = 500,
                        CategoryId = catTshirt?.CategoryId ?? 1,
                        IsActive = true
                    },
                    new Promotion
                    {
                        PromotionName = "P4: โปรโมชั่นจับคู่ 2 ชิ้น ลด 10%",
                        DiscountType = "Percent",
                        DiscountValue = 10,
                        MinOrderAmt = 0,
                        IsActive = true
                    },
                    new Promotion
                    {
                        PromotionName = "P5: Flash Sale ทุกวันที่ 1 ของเดือน ลด 20%",
                        DiscountType = "Percent",
                        DiscountValue = 20,
                        MinOrderAmt = 2000,
                        IsActive = true
                    }
                );
                db.SaveChanges();
            }

            // 5. เพิ่มโค้ดส่วนลดคูปองเริ่มต้น (Coupons)
            if (!db.Coupons.Any())
            {
                db.Coupons.AddRange(
                    new Coupon
                    {
                        CouponCode = "SAVE10",
                        DiscountType = "Percent",
                        DiscountValue = 10,
                        MinOrderAmt = 500,
                        UsageLimit = 100,
                        UsedCount = 0,
                        ExpiresAt = DateTime.Now.AddMonths(6),
                        IsActive = true
                    },
                    new Coupon
                    {
                        CouponCode = "NEW50",
                        DiscountType = "Fixed",
                        DiscountValue = 50,
                        MinOrderAmt = 300,
                        UsageLimit = 200,
                        UsedCount = 0,
                        ExpiresAt = DateTime.Now.AddMonths(6),
                        IsActive = true
                    },
                    new Coupon
                    {
                        CouponCode = "VIP100",
                        DiscountType = "Fixed",
                        DiscountValue = 100,
                        MinOrderAmt = 1000,
                        UsageLimit = 50,
                        UsedCount = 0,
                        ExpiresAt = DateTime.Now.AddMonths(6),
                        IsActive = true
                    }
                );
                db.SaveChanges();
            }


            // 6. เพิ่มข้อมูลคำสั่งซื้อตัวอย่าง (Sample Orders) หากยังไม่มีออเดอร์ในระบบ
            if (!db.Orders.Any())
            {
                var customer = db.Users.FirstOrDefault();
                if (customer != null)
                {
                    var address = db.Addresses.FirstOrDefault(a => a.UserId == customer.UserId);
                    if (address == null)
                    {
                        address = new Address
                        {
                            UserId = customer.UserId,
                            ReceiverName = customer.FirstName + " " + customer.LastName,
                            Phone = customer.PhoneNumber ?? "081-234-5678",
                            AddressLine = "99/123 หมู่บ้านสุขสราญ ถนนแฟชั่น",
                            District = "สไตล์",
                            Province = "กรุงเทพมหานคร",
                            PostalCode = "10110",
                            IsDefault = true
                        };
                        db.Addresses.Add(address);
                        db.SaveChanges();
                    }

                    var var1 = db.Productvariants.FirstOrDefault();
                    var var2 = db.Productvariants.Skip(2).FirstOrDefault();

                    if (var1 != null && address != null)
                    {
                        var sampleOrder1 = new Order
                        {
                            UserId = customer.UserId,
                            AddressId = address.AddressId,
                            Subtotal = 1040.00m,
                            ShippingFee = 0.00m,
                            DiscountAmt = 104.00m,
                            TotalAmount = 936.00m,
                            PromotionId = 1, // P1: ลด 10%
                            Status = "Paid",
                            PaymentMethod = "PromptPay",
                            TrackingNumber = "TH0987654321",
                            SlipImageUrl = "https://images.unsplash.com/photo-1554224155-8d04cb21cd6c?w=600&auto=format&fit=crop&q=80",
                            OrderedAt = DateTime.Now.AddDays(-1),
                            UpdatedAt = DateTime.Now.AddHours(-12)
                        };
                        db.Orders.Add(sampleOrder1);
                        db.SaveChanges();

                        db.Orderitems.AddRange(
                            new Orderitem { OrderId = sampleOrder1.OrderId, VariantId = var1.VariantId, Quantity = 2, UnitPrice = 290.00m },
                            new Orderitem { OrderId = sampleOrder1.OrderId, VariantId = (var2?.VariantId ?? var1.VariantId), Quantity = 1, UnitPrice = 460.00m }
                        );

                        var sampleOrder2 = new Order
                        {
                            UserId = customer.UserId,
                            AddressId = address.AddressId,
                            Subtotal = 690.00m,
                            ShippingFee = 0.00m,
                            DiscountAmt = 0.00m,
                            TotalAmount = 690.00m,
                            Status = "Verifying Slip",
                            PaymentMethod = "Bank Transfer",
                            SlipImageUrl = "https://images.unsplash.com/photo-1554224155-8d04cb21cd6c?w=600&auto=format&fit=crop&q=80",
                            OrderedAt = DateTime.Now.AddHours(-2),
                            UpdatedAt = DateTime.Now.AddHours(-2)
                        };
                        db.Orders.Add(sampleOrder2);
                        db.SaveChanges();

                        db.Orderitems.Add(
                            new Orderitem { OrderId = sampleOrder2.OrderId, VariantId = var1.VariantId, Quantity = 1, UnitPrice = 690.00m }
                        );
                        db.SaveChanges();
                    }
                }
            }
        }

        // ==========================================
        // สร้างตารางที่เพิ่มภายหลังให้อัตโนมัติ ถ้ายังไม่มีใน DB
        // (DB เดิมที่สร้างจาก CSI402DB_setup.sql เวอร์ชันเก่าจะใช้งานต่อได้ทันที)
        // ==========================================
        public static void EnsureExtraTables(Csi402dbContext db)
        {
            // P4: สินค้าที่ร้านกำหนดให้ร่วมโปรจับคู่ 2 ชิ้น
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS PromotionProducts (
                    PromotionID INT NOT NULL,
                    ProductID   INT NOT NULL,
                    PRIMARY KEY (PromotionID, ProductID),
                    FOREIGN KEY (PromotionID) REFERENCES Promotions(PromotionID) ON DELETE CASCADE,
                    FOREIGN KEY (ProductID)   REFERENCES Products(ProductID)     ON DELETE CASCADE
                ) ENGINE=InnoDB;");

            // แบนเนอร์หน้าร้าน
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS Banners (
                    BannerID  INT          NOT NULL AUTO_INCREMENT,
                    Title     VARCHAR(200) NOT NULL,
                    Subtitle  VARCHAR(300) NULL,
                    ImageURL  VARCHAR(500) NOT NULL,
                    LinkURL   VARCHAR(500) NULL,
                    SortOrder INT          NOT NULL DEFAULT 0,
                    IsActive  TINYINT(1)   NOT NULL DEFAULT 1,
                    CreatedAt DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (BannerID)
                ) ENGINE=InnoDB;");
        }
    }
}

