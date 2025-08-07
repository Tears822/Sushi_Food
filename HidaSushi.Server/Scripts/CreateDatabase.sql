-- HIDA SUSHI SQL Server Database Creation Script
USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'HidaSushiDb')
BEGIN
    CREATE DATABASE HidaSushiDb;
END
GO

USE HidaSushiDb;
GO

-- Drop existing tables if they exist
IF OBJECT_ID('OrderStatusHistory', 'U') IS NOT NULL DROP TABLE OrderStatusHistory;
IF OBJECT_ID('CustomerAddresses', 'U') IS NOT NULL DROP TABLE CustomerAddresses;
IF OBJECT_ID('OrderItems', 'U') IS NOT NULL DROP TABLE OrderItems;
IF OBJECT_ID('Orders', 'U') IS NOT NULL DROP TABLE Orders;
IF OBJECT_ID('CustomRolls', 'U') IS NOT NULL DROP TABLE CustomRolls;
IF OBJECT_ID('SushiRolls', 'U') IS NOT NULL DROP TABLE SushiRolls;
IF OBJECT_ID('Ingredients', 'U') IS NOT NULL DROP TABLE Ingredients;
IF OBJECT_ID('AdminUsers', 'U') IS NOT NULL DROP TABLE AdminUsers;
IF OBJECT_ID('Customers', 'U') IS NOT NULL DROP TABLE Customers;
IF OBJECT_ID('DailyAnalytics', 'U') IS NOT NULL DROP TABLE DailyAnalytics;
GO

-- Create Customers table
CREATE TABLE Customers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(200) NOT NULL UNIQUE,
    Phone NVARCHAR(20),
    PasswordHash NVARCHAR(500) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    EmailVerified BIT NOT NULL DEFAULT 0,
    PreferencesJson NVARCHAR(MAX),
    TotalOrders INT NOT NULL DEFAULT 0,
    TotalSpent DECIMAL(10,2) NOT NULL DEFAULT 0,
    LoyaltyPoints INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastLoginAt DATETIME2
);

-- Create AdminUsers table
CREATE TABLE AdminUsers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(200) UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    Role NVARCHAR(20) NOT NULL DEFAULT 'Admin',
    FullName NVARCHAR(100),
    IsActive BIT NOT NULL DEFAULT 1,
    PermissionsJson NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastLoginAt DATETIME2
);

-- Create SushiRolls table
CREATE TABLE SushiRolls (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500),
    Price DECIMAL(10,2) NOT NULL,
    IngredientsJson NVARCHAR(MAX),
    AllergensJson NVARCHAR(MAX),
    ImageUrl NVARCHAR(500),
    IsVegetarian BIT NOT NULL DEFAULT 0,
    IsVegan BIT NOT NULL DEFAULT 0,
    IsGlutenFree BIT NOT NULL DEFAULT 0,
    IsSignatureRoll BIT NOT NULL DEFAULT 0,
    IsAvailable BIT NOT NULL DEFAULT 1,
    PreparationTimeMinutes INT NOT NULL DEFAULT 15,
    Calories INT,
    PopularityScore INT NOT NULL DEFAULT 0,
    TimesOrdered INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Create Ingredients table
CREATE TABLE Ingredients (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(300),
    Category NVARCHAR(50) NOT NULL,
    AdditionalPrice DECIMAL(10,2) NOT NULL DEFAULT 0,
    IsAvailable BIT NOT NULL DEFAULT 1,
    AllergensJson NVARCHAR(MAX),
    MaxAllowed INT NOT NULL DEFAULT 1,
    ImageUrl NVARCHAR(500),
    Calories INT NOT NULL DEFAULT 0,
    Protein DECIMAL(5,2),
    Carbs DECIMAL(5,2),
    Fat DECIMAL(5,2),
    IsVegan BIT NOT NULL DEFAULT 0,
    IsGlutenFree BIT NOT NULL DEFAULT 0,
    StockQuantity INT,
    MinStockLevel INT NOT NULL DEFAULT 10,
    PopularityScore INT NOT NULL DEFAULT 0,
    TimesUsed INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Create Orders table
CREATE TABLE Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderNumber NVARCHAR(50) NOT NULL UNIQUE,
    CustomerId INT,
    CustomerName NVARCHAR(100) NOT NULL,
    CustomerEmail NVARCHAR(200),
    CustomerPhone NVARCHAR(20),
    SubtotalAmount DECIMAL(10,2) NOT NULL,
    DeliveryFee DECIMAL(10,2) NOT NULL DEFAULT 0,
    TaxAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
    TotalAmount DECIMAL(10,2) NOT NULL,
    Type NVARCHAR(20) NOT NULL DEFAULT 'Pickup',
    Status NVARCHAR(30) NOT NULL DEFAULT 'Received',
    PaymentMethod NVARCHAR(20) NOT NULL DEFAULT 'CashOnDelivery',
    PaymentStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    PaymentIntentId NVARCHAR(200),
    DeliveryAddress NVARCHAR(500),
    DeliveryInstructions NVARCHAR(500),
    Notes NVARCHAR(500),
    EstimatedDeliveryTime DATETIME2,
    ActualDeliveryTime DATETIME2,
    AcceptedAt DATETIME2,
    AcceptedBy INT,
    PreparationStartedAt DATETIME2,
    PreparationCompletedAt DATETIME2,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE SET NULL,
    FOREIGN KEY (AcceptedBy) REFERENCES AdminUsers(Id) ON DELETE SET NULL
);

-- Create CustomRolls table
CREATE TABLE CustomRolls (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) DEFAULT 'Custom Roll',
    RollType NVARCHAR(20) NOT NULL DEFAULT 'Normal',
    SelectedIngredientsJson NVARCHAR(MAX),
    TotalPrice DECIMAL(10,2) NOT NULL,
    Calories INT,
    AllergensJson NVARCHAR(MAX),
    Notes NVARCHAR(500),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Create OrderItems table
CREATE TABLE OrderItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    SushiRollId INT,
    CustomRollId INT,
    Quantity INT NOT NULL DEFAULT 1,
    UnitPrice DECIMAL(10,2) NOT NULL,
    TotalPrice DECIMAL(10,2) NOT NULL,
    SpecialInstructions NVARCHAR(500),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
    FOREIGN KEY (SushiRollId) REFERENCES SushiRolls(Id) ON DELETE SET NULL,
    FOREIGN KEY (CustomRollId) REFERENCES CustomRolls(Id) ON DELETE SET NULL
);

-- Create OrderStatusHistory table
CREATE TABLE OrderStatusHistory (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    PreviousStatus NVARCHAR(30),
    NewStatus NVARCHAR(30) NOT NULL,
    ChangedBy INT,
    Notes NVARCHAR(500),
    EstimatedCompletionTime DATETIME2,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
    FOREIGN KEY (ChangedBy) REFERENCES AdminUsers(Id) ON DELETE SET NULL
);

-- Create CustomerAddresses table
CREATE TABLE CustomerAddresses (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    Label NVARCHAR(50) NOT NULL,
    AddressLine1 NVARCHAR(200) NOT NULL,
    AddressLine2 NVARCHAR(200),
    City NVARCHAR(100) NOT NULL,
    PostalCode NVARCHAR(20) NOT NULL,
    Country NVARCHAR(50) NOT NULL DEFAULT 'Belgium',
    IsDefault BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE
);

-- Create DailyAnalytics table
CREATE TABLE DailyAnalytics (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Date DATE NOT NULL UNIQUE,
    TotalOrders INT NOT NULL DEFAULT 0,
    TotalRevenue DECIMAL(10,2) NOT NULL DEFAULT 0,
    DeliveryOrders INT NOT NULL DEFAULT 0,
    PickupOrders INT NOT NULL DEFAULT 0,
    CompletedOrders INT NOT NULL DEFAULT 0,
    CancelledOrders INT NOT NULL DEFAULT 0,
    AverageOrderValue DECIMAL(10,2) NOT NULL DEFAULT 0,
    AveragePrepTimeMinutes INT NOT NULL DEFAULT 0,
    PopularRollsJson NVARCHAR(MAX),
    PopularIngredientsJson NVARCHAR(MAX),
    HourlyOrderCountsJson NVARCHAR(MAX),
    CustomerRetentionRate DECIMAL(5,2),
    NewCustomers INT NOT NULL DEFAULT 0,
    ReturningCustomers INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Insert seed data
INSERT INTO AdminUsers (Username, Email, PasswordHash, Role, FullName, PermissionsJson) VALUES
('admin', 'admin@hidasushi.net', '$2a$11$dummy.hash.for.HidaSushi2024!', 'Admin', 'System Administrator', '["all"]'),
('jonathan', 'jonathan@hidasushi.net', '$2a$11$dummy.hash.for.ChefJonathan123!', 'Chef', 'Chef Jonathan', '["orders", "menu", "analytics", "ingredients"]'),
('kitchen', 'kitchen@hidasushi.net', '$2a$11$dummy.hash.for.Kitchen2024!', 'Kitchen', 'Kitchen Staff', '["orders", "order_status"]');

INSERT INTO SushiRolls (Name, Description, Price, IngredientsJson, AllergensJson, IsVegetarian, IsSignatureRoll, PreparationTimeMinutes, Calories) VALUES
('Taylor Swift – Tortured Poets Dragon Roll', '🐉 Avocado top, double tuna, spicy crunch, seaweed pearls, tempura shrimp', 13.00, '["Tuna", "Avocado", "Tempura Shrimp", "Seaweed", "Spicy Mayo"]', '["Fish", "Shellfish", "Gluten"]', 0, 1, 20, 520),
('Blackbird Rainbow Roll', '🐦 5-piece nigiri symphony: salmon, tuna, scallop, eel, yellowtail', 17.00, '["Salmon", "Tuna", "Scallop", "Eel", "Yellowtail", "Sushi Rice"]', '["Fish", "Shellfish"]', 0, 1, 25, 480),
('Garden of Eden Veggie Roll', '🌱 Fresh vegetables, avocado, cucumber, perfect for plant-based lovers', 7.00, '["Avocado", "Cucumber", "Carrot", "Lettuce", "Sushi Rice"]', '[]', 1, 0, 12, 280);

INSERT INTO Ingredients (Name, Description, Category, AdditionalPrice, AllergensJson, MaxAllowed, Calories, Protein, Carbs, Fat, IsVegan, IsGlutenFree, StockQuantity) VALUES
('Sushi Rice', 'Premium short-grain rice', 'Base', 0.00, '[]', 1, 130, 2.7, 28.0, 0.3, 1, 1, 1000),
('Fresh Tuna', 'Premium yellowfin tuna', 'Protein', 3.00, '["Fish"]', 3, 150, 32.0, 0.0, 1.0, 0, 1, 50),
('Norwegian Salmon', 'Fresh Atlantic salmon', 'Protein', 3.00, '["Fish"]', 3, 180, 25.0, 0.0, 8.0, 0, 1, 60),
('Hass Avocado', 'Creamy ripe avocado', 'Vegetable', 1.00, '[]', 2, 160, 2.0, 9.0, 15.0, 1, 1, 80),
('Spicy Mayo', 'House-made spicy mayonnaise', 'Sauce', 0.00, '["Eggs"]', 1, 50, 0.1, 0.5, 5.5, 0, 1, 100);

PRINT '🎉 HIDA SUSHI SQL Server Database Created Successfully!';
PRINT '🔐 Admin Access: admin/HidaSushi2024!, jonathan/ChefJonathan123!, kitchen/Kitchen2024!';
GO 