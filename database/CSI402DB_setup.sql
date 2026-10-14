-- ============================================================
--  CSI402DB  -  เสื้อผ้า.com  E-Commerce Database
--  Student : Waranon Soprok  |  Course : CSI402
-- ============================================================

DROP DATABASE IF EXISTS CSI402DB;
CREATE DATABASE CSI402DB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE CSI402DB;

-- TABLE 1 : Labstudent
CREATE TABLE Labstudent (
    StudentID   VARCHAR(10)  NOT NULL,
    FirstName   VARCHAR(50)  NOT NULL,
    LastName    VARCHAR(50)  NOT NULL,
    Email       VARCHAR(100) NOT NULL,
    Phone       VARCHAR(20)  NULL,
    Password    VARCHAR(255) NOT NULL,
    CreatedAt   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (StudentID)
) ENGINE=InnoDB;

-- TABLE 2 : Users
CREATE TABLE Users (
    UserID      INT          NOT NULL AUTO_INCREMENT,
    FirstName   VARCHAR(50)  NOT NULL,
    LastName    VARCHAR(50)  NOT NULL,
    Email       VARCHAR(100) NOT NULL UNIQUE,
    PhoneNumber VARCHAR(20)  NULL,
    Password    VARCHAR(255) NOT NULL,
    Role        ENUM('Customer','Staff','Accounting','Admin') NOT NULL DEFAULT 'Customer',
    IsActive    TINYINT(1)   NOT NULL DEFAULT 1,
    CreatedAt   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (UserID)
) ENGINE=InnoDB;

-- TABLE 3 : Addresses
CREATE TABLE Addresses (
    AddressID    INT          NOT NULL AUTO_INCREMENT,
    UserID       INT          NOT NULL,
    ReceiverName VARCHAR(100) NOT NULL,
    Phone        VARCHAR(20)  NOT NULL,
    AddressLine  VARCHAR(255) NOT NULL,
    District     VARCHAR(100) NOT NULL,
    Province     VARCHAR(100) NOT NULL,
    PostalCode   VARCHAR(10)  NOT NULL,
    IsDefault    TINYINT(1)   NOT NULL DEFAULT 0,
    PRIMARY KEY (AddressID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
) ENGINE=InnoDB;

-- TABLE 4 : Categories
CREATE TABLE Categories (
    CategoryID   INT          NOT NULL AUTO_INCREMENT,
    CategoryName VARCHAR(100) NOT NULL,
    Description  TEXT         NULL,
    PRIMARY KEY (CategoryID)
) ENGINE=InnoDB;

-- TABLE 5 : Products
CREATE TABLE Products (
    ProductID   INT            NOT NULL AUTO_INCREMENT,
    CategoryID  INT            NOT NULL,
    ProductName VARCHAR(200)   NOT NULL,
    Description TEXT           NULL,
    BasePrice   DECIMAL(10,2)  NOT NULL,
    ImageURL    VARCHAR(500)   NULL,
    IsActive    TINYINT(1)     NOT NULL DEFAULT 1,
    CreatedAt   DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (ProductID),
    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
) ENGINE=InnoDB;

-- TABLE 6 : ProductVariants
CREATE TABLE ProductVariants (
    VariantID   INT            NOT NULL AUTO_INCREMENT,
    ProductID   INT            NOT NULL,
    Color       VARCHAR(50)    NOT NULL,
    Size        VARCHAR(20)    NOT NULL,
    Stock       INT            NOT NULL DEFAULT 0,
    ExtraPrice  DECIMAL(10,2)  NOT NULL DEFAULT 0.00,
    ImageURL    VARCHAR(500)   NULL,
    PRIMARY KEY (VariantID),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE
) ENGINE=InnoDB;

-- TABLE 7 : Promotions
CREATE TABLE Promotions (
    PromotionID   INT            NOT NULL AUTO_INCREMENT,
    PromotionName VARCHAR(200)   NOT NULL,
    DiscountType  ENUM('Percent','Fixed') NOT NULL DEFAULT 'Percent',
    DiscountValue DECIMAL(10,2)  NOT NULL,
    MinOrderAmt   DECIMAL(10,2)  NOT NULL DEFAULT 0.00,
    CategoryID    INT            NULL,
    StartDate     DATETIME       NULL,
    EndDate       DATETIME       NULL,
    IsActive      TINYINT(1)     NOT NULL DEFAULT 1,
    PRIMARY KEY (PromotionID),
    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID) ON DELETE SET NULL
) ENGINE=InnoDB;

-- TABLE 8 : Coupons
CREATE TABLE Coupons (
    CouponID      INT            NOT NULL AUTO_INCREMENT,
    CouponCode    VARCHAR(50)    NOT NULL UNIQUE,
    DiscountType  ENUM('Percent','Fixed') NOT NULL DEFAULT 'Percent',
    DiscountValue DECIMAL(10,2)  NOT NULL,
    MinOrderAmt   DECIMAL(10,2)  NOT NULL DEFAULT 0.00,
    UsageLimit    INT            NULL,
    UsedCount     INT            NOT NULL DEFAULT 0,
    ExpiresAt     DATETIME       NULL,
    IsActive      TINYINT(1)     NOT NULL DEFAULT 1,
    PRIMARY KEY (CouponID)
) ENGINE=InnoDB;

-- TABLE 9 : Orders
CREATE TABLE Orders (
    OrderID        INT            NOT NULL AUTO_INCREMENT,
    UserID         INT            NOT NULL,
    AddressID      INT            NOT NULL,
    Subtotal       DECIMAL(10,2)  NOT NULL,
    ShippingFee    DECIMAL(10,2)  NOT NULL DEFAULT 0.00,
    DiscountAmt    DECIMAL(10,2)  NOT NULL DEFAULT 0.00,
    TotalAmount    DECIMAL(10,2)  NOT NULL,
    PromotionID    INT            NULL,
    CouponID       INT            NULL,
    Status         ENUM('Pending Payment','Verifying Slip','Paid','Processing','Shipped','Completed','Cancelled','Invalid Slip','Refunded') NOT NULL DEFAULT 'Pending Payment',
    PaymentMethod  ENUM('Bank Transfer','Credit Card','PromptPay') NOT NULL,
    TrackingNumber VARCHAR(100)   NULL,
    SlipImageURL   VARCHAR(500)   NULL,
    OrderedAt      DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt      DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (OrderID),
    FOREIGN KEY (UserID)      REFERENCES Users(UserID),
    FOREIGN KEY (AddressID)   REFERENCES Addresses(AddressID),
    FOREIGN KEY (PromotionID) REFERENCES Promotions(PromotionID) ON DELETE SET NULL,
    FOREIGN KEY (CouponID)    REFERENCES Coupons(CouponID) ON DELETE SET NULL
) ENGINE=InnoDB;

-- TABLE 10 : OrderItems
CREATE TABLE OrderItems (
    OrderItemID INT            NOT NULL AUTO_INCREMENT,
    OrderID     INT            NOT NULL,
    VariantID   INT            NOT NULL,
    Quantity    INT            NOT NULL,
    UnitPrice   DECIMAL(10,2)  NOT NULL,
    PRIMARY KEY (OrderItemID),
    FOREIGN KEY (OrderID)   REFERENCES Orders(OrderID) ON DELETE CASCADE,
    FOREIGN KEY (VariantID) REFERENCES ProductVariants(VariantID)
) ENGINE=InnoDB;

-- TABLE 11 : CartItems
CREATE TABLE CartItems (
    CartItemID  INT      NOT NULL AUTO_INCREMENT,
    UserID      INT      NOT NULL,
    VariantID   INT      NOT NULL,
    Quantity    INT      NOT NULL DEFAULT 1,
    AddedAt     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (CartItemID),
    FOREIGN KEY (UserID)    REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (VariantID) REFERENCES ProductVariants(VariantID) ON DELETE CASCADE
) ENGINE=InnoDB;

-- TABLE 12 : Wishlists
CREATE TABLE Wishlists (
    WishlistID  INT      NOT NULL AUTO_INCREMENT,
    UserID      INT      NOT NULL,
    ProductID   INT      NOT NULL,
    AddedAt     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (WishlistID),
    UNIQUE KEY UQ_Wishlist (UserID, ProductID),
    FOREIGN KEY (UserID)    REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE
) ENGINE=InnoDB;

-- TABLE 13 : Reviews
CREATE TABLE Reviews (
    ReviewID    INT      NOT NULL AUTO_INCREMENT,
    ProductID   INT      NOT NULL,
    UserID      INT      NOT NULL,
    Rating      TINYINT  NOT NULL,
    Comment     TEXT     NULL,
    CreatedAt   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (ReviewID),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE CASCADE,
    FOREIGN KEY (UserID)    REFERENCES Users(UserID) ON DELETE CASCADE
) ENGINE=InnoDB;

-- TABLE 14 : Notifications
CREATE TABLE Notifications (
    NotificationID  INT          NOT NULL AUTO_INCREMENT,
    UserID          INT          NOT NULL,
    Title           VARCHAR(200) NOT NULL,
    Message         TEXT         NOT NULL,
    IsRead          TINYINT(1)   NOT NULL DEFAULT 0,
    CreatedAt       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (NotificationID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
) ENGINE=InnoDB;

SHOW TABLES;
