-- ============================================================
-- สคริปต์เพิ่มข้อมูลตัวอย่าง "เสื้อผ้าแฟชั่นล้วนๆ" สำหรับหน้าร้าน เสื้อผ้า.com
-- CSI402 Project | Database: CSI402DB
-- สามารถ Copy ไปรันใน MySQL Workbench ได้ทันที
-- ============================================================

USE CSI402DB;

-- 1. เพิ่มหมวดหมู่เสื้อผ้า (Categories - เฉพาะเสื้อผ้าเท่านั้น)
INSERT INTO Categories (CategoryID, CategoryName, Description) VALUES
(1, 'เสื้อยืด (T-Shirts)', 'เสื้อยืดแฟชั่น คอกลม คอวี โอเวอร์ไซส์ ผ้า Cotton 100% สวมใส่สบาย'),
(2, 'เสื้อเชิ้ต (Shirts)', 'เสื้อเชิ้ตทำงานและเสื้อเชิ้ตลำลอง ทั้งแขนสั้นและแขนยาว เนื้อผ้าใส่สบาย'),
(3, 'เสื้อโปโล (Polo Shirts)', 'เสื้อโปโลคลาสสิก ผ้าปีเก้ ทรงสวย ดูดีได้ทั้งทางการและลำลอง'),
(4, 'เสื้อฮู้ด & สเวตเตอร์', 'เสื้อกันหนาวมีฮู้ด สเวตเตอร์ไหมพรม ผ้านุ่ม อบอุ่น สไตล์สตรีท'),
(5, 'เสื้อแจ็กเก็ต & สูทลำลอง', 'เสื้อคลุม เสื้อแจ็กเก็ตยีนส์ และเบลเซอร์แฟชั่น')
ON DUPLICATE KEY UPDATE CategoryName=VALUES(CategoryName), Description=VALUES(Description);

-- 2. เพิ่มสินค้าเสื้อผ้า (Products - เฉพาะเสื้อผ้า)
INSERT INTO Products (ProductID, CategoryID, ProductName, Description, BasePrice, ImageURL, IsActive, CreatedAt) VALUES
-- 1. เสื้อยืด Oversize
(1, 1, 'เสื้อยืด Oversize มินิมอล Cotton 100%', 
 'เสื้อยืดทรงโอเวอร์ไซส์ สไตล์เกาหลี ผลิตจากผ้าฝ้าย Comb 100% เนื้อนุ่ม ไม่ร้อน ระบายอากาศได้ดีเยี่ยม เหมาะกับใส่ในชีวิตประจำวัน แมทช์ง่าย', 
 290.00, 'https://images.unsplash.com/photo-1521572267360-ee0c2909d518?w=800&auto=format&fit=crop&q=80', 1, NOW()),

-- 2. เสื้อยืดกราฟิกสตรีท
(2, 1, 'เสื้อยืดสกรีนลายกราฟิกสตรีท Urban Vibe', 
 'เสื้อยืดสกรีนลายกราฟิกด้านหลัง สไตล์สตรีทแฟชั่น สีสกรีนคมชัดไม่หลุดลอก ซักเครื่องได้ ทรง Regular สวมใส่ได้ทั้งชายและหญิง', 
 350.00, 'https://images.unsplash.com/photo-1503342217505-b0a15ec3261c?w=800&auto=format&fit=crop&q=80', 1, NOW()),

-- 3. เสื้อเชิ้ตแขนยาว Oxford
(3, 2, 'เสื้อเชิ้ตแขนยาว Oxford Slim Fit', 
 'เสื้อเชิ้ตผ้า Oxford ทอละเอียด เนื้อผ้าระบายอากาศได้ดี รีดง่าย ยับยาก ทรงเข้ารูปกำลังดี ใส่ทำงานหรือใส่เที่ยวก็ดูดีมีระดับ', 
 490.00, 'https://images.unsplash.com/photo-1596755094514-f87e34085b2c?w=800&auto=format&fit=crop&q=80', 1, NOW()),

-- 4. เสื้อเชิ้ตแขนสั้นลายปริ้นท์ฮาวาย
(4, 2, 'เสื้อเชิ้ตแขนสั้น Cuban Collar สไตล์รีสอร์ท', 
 'เสื้อเชิ้ตคอปกบัวสไตล์คิวบา ผ้าเรยอนพริ้วไหว สวมใส่สบาย ระบายอากาศดีเยี่ยม เหมาะกับวันพักผ่อนและอากาศเมืองไทย', 
 420.00, 'https://images.unsplash.com/photo-1598033129183-c4f50c736f10?w=800&auto=format&fit=crop&q=80', 1, NOW()),

-- 5. เสื้อโปโลคลาสสิก
(5, 3, 'เสื้อโปโล Cotton Pique Classic Fit', 
 'เสื้อโปโลทรงคลาสสิก ทอจากผ้าคอตตอนปีเก้ ปกเสื้ออยู่ทรงสวย ไม่ย้วย ปลายแขนจั๊มเบาๆ ให้ลุคเรียบหรู กึ่งทางการ', 
 390.00, 'https://images.unsplash.com/photo-1581655353564-df123a1eb820?w=800&auto=format&fit=crop&q=80', 1, NOW()),

-- 6. เสื้อฮู้ดกันหนาว
(6, 4, 'เสื้อฮู้ด Fleece Hoodie ผ้านุ่มพิเศษ สไตล์สตรีท', 
 'เสื้อกันหนาวมีฮู้ด ด้านในบุผ้าฟลีซสัมผัสนุ่ม อบอุ่น สบายตัว มีกระเป๋าจิงโจ้ด้านหน้าและเชือกรูดฮู้ด หนานุ่มกำลังดี', 
 690.00, 'https://images.unsplash.com/photo-1556905055-8f358a7a47b2?w=800&auto=format&fit=crop&q=80', 1, NOW()),

-- 7. เสื้อสเวตเตอร์ไหมพรม
(7, 4, 'เสื้อสเวตเตอร์ถัก Knit Sweater แขนยาว มินิมอล', 
 'เสื้อสเวตเตอร์ไหมพรมถักละเอียด เนื้อผ้านุ่ม ไม่ระคายเคืองผิว คอกลม ดีไซน์เรียบง่ายสไตล์มินิมอล แมทช์ได้ทุกลุค', 
 590.00, 'https://images.unsplash.com/photo-1620799140408-edc6dcb6d633?w=800&auto=format&fit=crop&q=80', 1, NOW()),

-- 8. แจ็กเก็ตยีนส์วินเทจ
(8, 5, 'เสื้อแจ็กเก็ตยีนส์ Vintage Denim Jacket', 
 'เสื้อแจ็กเก็ตยีนส์ทรงคลาสสิก ฟอกสีสไตล์วินเทจ ตัดเย็บด้วยผ้ายีนส์หนา 14 Oz มีกระเป๋าหน้า 2 ช่อง คลุมทับได้ทุกชุด เพิ่มความเท่', 
 890.00, 'https://images.unsplash.com/photo-1576995853123-5a10305d93c0?w=800&auto=format&fit=crop&q=80', 1, NOW())
ON DUPLICATE KEY UPDATE ProductName=VALUES(ProductName), BasePrice=VALUES(BasePrice), ImageURL=VALUES(ImageURL), Description=VALUES(Description);

-- 3. เพิ่มตัวเลือกสินค้า SKU (สี / ไซส์ / สต็อก / ส่วนต่างราคา)
INSERT INTO ProductVariants (VariantID, ProductID, Color, Size, Stock, ExtraPrice, ImageURL) VALUES
-- เสื้อยืด Oversize (ProductID: 1)
(1, 1, 'สีขาว (White)', 'M', 30, 0.00, 'https://images.unsplash.com/photo-1521572267360-ee0c2909d518?w=800&auto=format&fit=crop&q=80'),
(2, 1, 'สีขาว (White)', 'L', 25, 0.00, 'https://images.unsplash.com/photo-1521572267360-ee0c2909d518?w=800&auto=format&fit=crop&q=80'),
(3, 1, 'สีดำ (Black)', 'M', 20, 0.00, 'https://images.unsplash.com/photo-1503342217505-b0a15ec3261c?w=800&auto=format&fit=crop&q=80'),
(4, 1, 'สีดำ (Black)', 'XL', 15, 20.00, 'https://images.unsplash.com/photo-1503342217505-b0a15ec3261c?w=800&auto=format&fit=crop&q=80'),

-- เสื้อยืดสตรีท Urban (ProductID: 2)
(5, 2, 'สีดำ (Black)', 'M', 18, 0.00, 'https://images.unsplash.com/photo-1503342217505-b0a15ec3261c?w=800&auto=format&fit=crop&q=80'),
(6, 2, 'สีดำ (Black)', 'L', 22, 0.00, 'https://images.unsplash.com/photo-1503342217505-b0a15ec3261c?w=800&auto=format&fit=crop&q=80'),
(7, 2, 'สีขาว (White)', 'XL', 12, 20.00, 'https://images.unsplash.com/photo-1521572267360-ee0c2909d518?w=800&auto=format&fit=crop&q=80'),

-- เสื้อเชิ้ตแขนยาว Oxford (ProductID: 3)
(8, 3, 'สีฟ้าคราม (Light Blue)', 'S', 15, 0.00, 'https://images.unsplash.com/photo-1596755094514-f87e34085b2c?w=800&auto=format&fit=crop&q=80'),
(9, 3, 'สีฟ้าคราม (Light Blue)', 'M', 20, 0.00, 'https://images.unsplash.com/photo-1596755094514-f87e34085b2c?w=800&auto=format&fit=crop&q=80'),
(10, 3, 'สีขาว (White)', 'L', 15, 0.00, 'https://images.unsplash.com/photo-1602810318383-e386cc2a3ccf?w=800&auto=format&fit=crop&q=80'),
(11, 3, 'สีขาว (White)', 'XL', 10, 30.00, 'https://images.unsplash.com/photo-1602810318383-e386cc2a3ccf?w=800&auto=format&fit=crop&q=80'),

-- เสื้อเชิ้ตคิวบา แขนสั้น (ProductID: 4)
(12, 4, 'สีกรมท่า (Navy)', 'M', 15, 0.00, 'https://images.unsplash.com/photo-1598033129183-c4f50c736f10?w=800&auto=format&fit=crop&q=80'),
(13, 4, 'สีกรมท่า (Navy)', 'L', 12, 0.00, 'https://images.unsplash.com/photo-1598033129183-c4f50c736f10?w=800&auto=format&fit=crop&q=80'),

-- เสื้อโปโลคลาสสิก (ProductID: 5)
(14, 5, 'สีกรมท่า (Navy)', 'M', 20, 0.00, 'https://images.unsplash.com/photo-1625910513413-7223b5d27d58?w=800&auto=format&fit=crop&q=80'),
(15, 5, 'สีดำ (Black)', 'L', 18, 0.00, 'https://images.unsplash.com/photo-1625910513413-7223b5d27d58?w=800&auto=format&fit=crop&q=80'),
(16, 5, 'สีขาว (White)', 'XL', 14, 20.00, 'https://images.unsplash.com/photo-1625910513413-7223b5d27d58?w=800&auto=format&fit=crop&q=80'),

-- เสื้อฮู้ด Fleece Hoodie (ProductID: 6)
(17, 6, 'สีเทา (Heather Grey)', 'M', 15, 0.00, 'https://images.unsplash.com/photo-1556905055-8f358a7a47b2?w=800&auto=format&fit=crop&q=80'),
(18, 6, 'สีเทา (Heather Grey)', 'L', 10, 0.00, 'https://images.unsplash.com/photo-1556905055-8f358a7a47b2?w=800&auto=format&fit=crop&q=80'),
(19, 6, 'สีดำ (Black)', 'XL', 12, 30.00, 'https://images.unsplash.com/photo-1509967419530-da38b4704bc6?w=800&auto=format&fit=crop&q=80'),

-- เสื้อสเวตเตอร์ไหมพรม (ProductID: 7)
(20, 7, 'สีครีมเบจ (Beige)', 'M', 14, 0.00, 'https://images.unsplash.com/photo-1620799140408-edc6dcb6d633?w=800&auto=format&fit=crop&q=80'),
(21, 7, 'สีครีมเบจ (Beige)', 'L', 10, 0.00, 'https://images.unsplash.com/photo-1620799140408-edc6dcb6d633?w=800&auto=format&fit=crop&q=80'),

-- เสื้อแจ็กเก็ตยีนส์ (ProductID: 8)
(22, 8, 'สียีนส์เข้ม (Dark Denim)', 'M', 10, 0.00, 'https://images.unsplash.com/photo-1576995853123-5a10305d93c0?w=800&auto=format&fit=crop&q=80'),
(23, 8, 'สียีนส์เข้ม (Dark Denim)', 'L', 8, 0.00, 'https://images.unsplash.com/photo-1576995853123-5a10305d93c0?w=800&auto=format&fit=crop&q=80'),
(24, 8, 'สียีนส์ฟอก (Light Wash)', 'XL', 6, 50.00, 'https://images.unsplash.com/photo-1551537482-f2075a1d41f2?w=800&auto=format&fit=crop&q=80')
ON DUPLICATE KEY UPDATE Stock=VALUES(Stock), ExtraPrice=VALUES(ExtraPrice);

-- 4. เพิ่มโปรโมชั่นเริ่มต้นตามข้อกำหนดใน README.md (P1 - P5)
INSERT INTO Promotions (PromotionID, PromotionName, DiscountType, DiscountValue, MinOrderAmt, CategoryID, IsActive) VALUES
(1, 'P1: ช้อปครบ 1,000 บาท รับส่วนลด 10%', 'Percent', 10.00, 1000.00, NULL, 1),
(2, 'P2: ช้อปครบ 3,000 บาท รับส่วนลด 15%', 'Percent', 15.00, 3000.00, NULL, 1),
(3, 'P3: หมวดหมู่เสื้อยืด ซื้อครบ 500 บาท ลด 15%', 'Percent', 15.00, 500.00, 1, 1),
(4, 'P4: โปรโมชั่นจับคู่ 2 ชิ้น ลด 10%', 'Percent', 10.00, 0.00, NULL, 1),
(5, 'P5: Flash Sale ทุกวันที่ 1 ของเดือน ลด 20%', 'Percent', 20.00, 2000.00, NULL, 1)
ON DUPLICATE KEY UPDATE PromotionName=VALUES(PromotionName), DiscountValue=VALUES(DiscountValue), MinOrderAmt=VALUES(MinOrderAmt), IsActive=VALUES(IsActive);

-- 5. เพิ่มโค้ดคูปองส่วนลดเริ่มต้น (Coupons)
INSERT INTO Coupons (CouponID, CouponCode, DiscountType, DiscountValue, MinOrderAmt, UsageLimit, UsedCount, ExpiresAt, IsActive) VALUES
(1, 'SAVE10', 'Percent', 10.00, 500.00, 100, 0, DATE_ADD(NOW(), INTERVAL 6 MONTH), 1),
(2, 'NEW50', 'Fixed', 50.00, 300.00, 200, 0, DATE_ADD(NOW(), INTERVAL 6 MONTH), 1),
(3, 'VIP100', 'Fixed', 100.00, 1000.00, 50, 0, DATE_ADD(NOW(), INTERVAL 6 MONTH), 1)
ON DUPLICATE KEY UPDATE DiscountValue=VALUES(DiscountValue), MinOrderAmt=VALUES(MinOrderAmt), IsActive=VALUES(IsActive);

SELECT 'เพิ่มข้อมูลเสื้อผ้า โปรโมชั่น และคูปองตัวอย่างครบถ้วนเรียบร้อยแล้ว!' AS Status;
