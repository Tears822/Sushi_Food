-- HIDA SUSHI Database Creation Script
-- Run this script in your Supabase SQL Editor

-- Drop existing tables if they exist (in correct order to handle foreign keys)
DROP TABLE IF EXISTS "OrderItems" CASCADE;
DROP TABLE IF EXISTS "Orders" CASCADE;
DROP TABLE IF EXISTS "CustomRolls" CASCADE;
DROP TABLE IF EXISTS "SushiRolls" CASCADE;
DROP TABLE IF EXISTS "Ingredients" CASCADE;
DROP TABLE IF EXISTS "AdminUsers" CASCADE;
DROP TABLE IF EXISTS "Customers" CASCADE;

-- Create Customers table for client-side users
CREATE TABLE "Customers" (
    "Id" SERIAL PRIMARY KEY,
    "FullName" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(200) NOT NULL UNIQUE,
    "Phone" VARCHAR(20),
    "PasswordHash" VARCHAR(500) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "DefaultDeliveryAddress" VARCHAR(500),
    "PreferencesJson" TEXT, -- JSON field for dietary preferences, favorite rolls, etc.
    "TotalOrders" INTEGER NOT NULL DEFAULT 0,
    "TotalSpent" DECIMAL(10,2) NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "LastLoginAt" TIMESTAMP
);

-- Create AdminUsers table for admin dashboard users
CREATE TABLE "AdminUsers" (
    "Id" SERIAL PRIMARY KEY,
    "Username" VARCHAR(50) NOT NULL UNIQUE,
    "Email" VARCHAR(200) UNIQUE,
    "PasswordHash" VARCHAR(500) NOT NULL,
    "Role" VARCHAR(20) NOT NULL DEFAULT 'Admin', -- Admin, Chef, Kitchen, Manager
    "FullName" VARCHAR(100),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "Permissions" TEXT, -- JSON field for granular permissions
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "LastLoginAt" TIMESTAMP
);

-- Create SushiRolls table with enhanced features
CREATE TABLE "SushiRolls" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "Description" VARCHAR(500),
    "Price" DECIMAL(10,2) NOT NULL,
    "Ingredients" TEXT, -- Comma-separated list for display
    "Allergens" TEXT, -- Comma-separated list
    "ImageUrl" VARCHAR(500),
    "IsVegetarian" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsVegan" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsGlutenFree" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsSignatureRoll" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsAvailable" BOOLEAN NOT NULL DEFAULT TRUE,
    "PreparationTimeMinutes" INTEGER NOT NULL DEFAULT 15,
    "Calories" INTEGER,
    "PopularityScore" INTEGER NOT NULL DEFAULT 0, -- For analytics
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create Ingredients table with comprehensive details
CREATE TABLE "Ingredients" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(300),
    "Category" VARCHAR(50) NOT NULL, -- Base, Protein, Vegetable, Extra, Topping, Sauce, Wrapper
    "AdditionalPrice" DECIMAL(10,2) NOT NULL DEFAULT 0,
    "IsAvailable" BOOLEAN NOT NULL DEFAULT TRUE,
    "Allergens" TEXT, -- Comma-separated list
    "MaxAllowed" INTEGER NOT NULL DEFAULT 1,
    "ImageUrl" VARCHAR(500),
    "Calories" INTEGER NOT NULL DEFAULT 0,
    "IsVegan" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsGlutenFree" BOOLEAN NOT NULL DEFAULT FALSE,
    "StockQuantity" INTEGER, -- For inventory management
    "PopularityScore" INTEGER NOT NULL DEFAULT 0, -- For analytics
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create Orders table with enhanced tracking
CREATE TABLE "Orders" (
    "Id" SERIAL PRIMARY KEY,
    "OrderNumber" VARCHAR(50) NOT NULL UNIQUE,
    "CustomerId" INTEGER, -- NULL for guest orders
    "CustomerName" VARCHAR(100) NOT NULL,
    "CustomerEmail" VARCHAR(200),
    "CustomerPhone" VARCHAR(20),
    "TotalAmount" DECIMAL(10,2) NOT NULL,
    "Type" VARCHAR(20) NOT NULL DEFAULT 'Pickup', -- Pickup, Delivery
    "Status" VARCHAR(30) NOT NULL DEFAULT 'Received', -- Received, InPreparation, Ready, OutForDelivery, Completed, Cancelled
    "PaymentMethod" VARCHAR(20) NOT NULL DEFAULT 'CashOnDelivery', -- CashOnDelivery, Stripe, GodPay
    "PaymentStatus" VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Paid, Failed, Refunded
    "PaymentIntentId" VARCHAR(200), -- For Stripe integration
    "DeliveryAddress" VARCHAR(500),
    "DeliveryFee" DECIMAL(10,2) NOT NULL DEFAULT 0,
    "Notes" VARCHAR(500),
    "EstimatedDeliveryTime" TIMESTAMP,
    "ActualDeliveryTime" TIMESTAMP,
    "PreparationStartedAt" TIMESTAMP,
    "PreparationCompletedAt" TIMESTAMP,
    "AcceptedAt" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("CustomerId") REFERENCES "Customers"("Id") ON DELETE SET NULL
);

-- Create CustomRolls table with detailed configuration
CREATE TABLE "CustomRolls" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(200) DEFAULT 'Custom Roll',
    "RollType" VARCHAR(20) NOT NULL DEFAULT 'Normal', -- Normal, InsideOut, CucumberWrap
    "SelectedIngredientIds" TEXT, -- JSON array of ingredient IDs
    "TotalPrice" DECIMAL(10,2) NOT NULL,
    "Calories" INTEGER,
    "Notes" VARCHAR(500),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create OrderItems table
CREATE TABLE "OrderItems" (
    "Id" SERIAL PRIMARY KEY,
    "OrderId" INTEGER NOT NULL,
    "SushiRollId" INTEGER,
    "CustomRollId" INTEGER,
    "Quantity" INTEGER NOT NULL DEFAULT 1,
    "Price" DECIMAL(10,2) NOT NULL, -- Price at time of order
    "Notes" VARCHAR(500),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("OrderId") REFERENCES "Orders"("Id") ON DELETE CASCADE,
    FOREIGN KEY ("SushiRollId") REFERENCES "SushiRolls"("Id") ON DELETE SET NULL,
    FOREIGN KEY ("CustomRollId") REFERENCES "CustomRolls"("Id") ON DELETE SET NULL
);

-- Create OrderStatusHistory table for tracking
CREATE TABLE "OrderStatusHistory" (
    "Id" SERIAL PRIMARY KEY,
    "OrderId" INTEGER NOT NULL,
    "Status" VARCHAR(30) NOT NULL,
    "ChangedBy" INTEGER, -- AdminUser ID
    "Notes" VARCHAR(500),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("OrderId") REFERENCES "Orders"("Id") ON DELETE CASCADE,
    FOREIGN KEY ("ChangedBy") REFERENCES "AdminUsers"("Id") ON DELETE SET NULL
);

-- Create CustomerAddresses table for multiple addresses
CREATE TABLE "CustomerAddresses" (
    "Id" SERIAL PRIMARY KEY,
    "CustomerId" INTEGER NOT NULL,
    "Label" VARCHAR(50) NOT NULL, -- Home, Work, etc.
    "AddressLine1" VARCHAR(200) NOT NULL,
    "AddressLine2" VARCHAR(200),
    "City" VARCHAR(100) NOT NULL,
    "PostalCode" VARCHAR(20) NOT NULL,
    "Country" VARCHAR(50) NOT NULL DEFAULT 'Belgium',
    "IsDefault" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("CustomerId") REFERENCES "Customers"("Id") ON DELETE CASCADE
);

-- Create Analytics table for daily statistics
CREATE TABLE "DailyAnalytics" (
    "Id" SERIAL PRIMARY KEY,
    "Date" DATE NOT NULL UNIQUE,
    "TotalOrders" INTEGER NOT NULL DEFAULT 0,
    "TotalRevenue" DECIMAL(10,2) NOT NULL DEFAULT 0,
    "DeliveryOrders" INTEGER NOT NULL DEFAULT 0,
    "PickupOrders" INTEGER NOT NULL DEFAULT 0,
    "CompletedOrders" INTEGER NOT NULL DEFAULT 0,
    "CancelledOrders" INTEGER NOT NULL DEFAULT 0,
    "AverageOrderValue" DECIMAL(10,2) NOT NULL DEFAULT 0,
    "AveragePrepTimeMinutes" INTEGER NOT NULL DEFAULT 0,
    "PopularRollsJson" TEXT, -- JSON array of {rollId, name, count}
    "PopularIngredientsJson" TEXT, -- JSON array of {ingredientId, name, count}
    "HourlyOrderCountsJson" TEXT, -- JSON object of {hour: count}
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Insert Enhanced Signature Rolls
INSERT INTO "SushiRolls" ("Name", "Description", "Price", "Ingredients", "Allergens", "IsVegetarian", "IsSignatureRoll", "PreparationTimeMinutes", "Calories") VALUES
('Taylor Swift – Tortured Poets Dragon Roll', '🐉 Avocado top, double tuna, spicy crunch, seaweed pearls, tempura shrimp', 13.00, 'Tuna,Avocado,Tempura Shrimp,Seaweed,Spicy Mayo', 'Fish,Shellfish,Gluten', FALSE, TRUE, 20, 520),
('Blackbird Rainbow Roll', '🐦 5-piece nigiri symphony: salmon, tuna, scallop, eel, yellowtail', 17.00, 'Salmon,Tuna,Scallop,Eel,Yellowtail,Sushi Rice', 'Fish,Shellfish', FALSE, TRUE, 25, 480),
('M&M "Beautiful" Roll', '👑 Big, bold, meaty — salmon + wagyu + wasabi aioli', 19.00, 'Salmon,Wagyu Beef,Wasabi,Sushi Rice', 'Fish', FALSE, TRUE, 22, 590),
('Joker Laughing Volcano Roll', '🃏 Spicy tuna, cream cheese, topped with flamed jalapeño mayo', 23.00, 'Spicy Tuna,Cream Cheese,Jalapeño,Spicy Mayo', 'Fish,Dairy', FALSE, TRUE, 18, 610),
('Garden of Eden Veggie Roll', '🌱 Fresh vegetables, avocado, cucumber, perfect for plant-based lovers', 7.00, 'Avocado,Cucumber,Carrot,Lettuce,Sushi Rice', '', TRUE, FALSE, 12, 280);

-- Insert Enhanced Ingredients
INSERT INTO "Ingredients" ("Name", "Description", "Category", "AdditionalPrice", "Allergens", "MaxAllowed", "Calories", "IsVegan", "IsGlutenFree", "StockQuantity") VALUES
-- Bases
('Sushi Rice', 'Premium short-grain rice', 'Base', 0.00, '', 1, 130, TRUE, TRUE, 1000),
('Brown Rice', 'Healthy brown rice option', 'Base', 1.00, '', 1, 110, TRUE, TRUE, 500),

-- Proteins
('Tuna', 'Fresh yellowfin tuna', 'Protein', 3.00, 'Fish', 3, 150, FALSE, TRUE, 50),
('Salmon', 'Norwegian salmon', 'Protein', 3.00, 'Fish', 3, 180, FALSE, TRUE, 60),
('Yellowtail', 'Buttery yellowtail', 'Protein', 4.00, 'Fish', 2, 160, FALSE, TRUE, 30),
('Eel', 'Grilled freshwater eel', 'Protein', 4.00, 'Fish', 2, 200, FALSE, TRUE, 25),
('Shrimp', 'Cooked tiger shrimp', 'Protein', 2.50, 'Shellfish', 3, 80, FALSE, TRUE, 40),
('Crab', 'Sweet crab meat', 'Protein', 3.50, 'Shellfish', 2, 90, FALSE, TRUE, 30),
('Scallop', 'Pan-seared scallop', 'Protein', 4.50, 'Shellfish', 2, 95, FALSE, TRUE, 20),
('Tofu', 'Marinated tofu (vegan)', 'Protein', 1.50, '', 3, 70, TRUE, TRUE, 100),

-- Vegetables
('Avocado', 'Creamy Hass avocado', 'Vegetable', 1.00, '', 2, 160, TRUE, TRUE, 80),
('Cucumber', 'Crisp cucumber', 'Vegetable', 0.50, '', 2, 8, TRUE, TRUE, 120),
('Carrot', 'Julienned carrots', 'Vegetable', 0.50, '', 2, 25, TRUE, TRUE, 100),
('Lettuce', 'Fresh butter lettuce', 'Vegetable', 0.50, '', 2, 5, TRUE, TRUE, 150),
('Asparagus', 'Grilled asparagus', 'Vegetable', 1.00, '', 1, 20, TRUE, TRUE, 60),

-- Premium Extras
('Cream Cheese', 'Rich cream cheese', 'Extra', 1.00, 'Dairy', 1, 100, FALSE, TRUE, 40),
('Goat Cheese', 'Tangy goat cheese', 'Extra', 1.50, 'Dairy', 1, 75, FALSE, TRUE, 20),
('Mango', 'Sweet tropical mango', 'Extra', 1.00, '', 1, 60, TRUE, TRUE, 30),
('Jalapeño', 'Spicy jalapeño slices', 'Extra', 0.50, '', 2, 4, TRUE, TRUE, 50),

-- Toppings
('Sesame Seeds', 'Toasted sesame seeds', 'Topping', 0.00, 'Sesame', 1, 50, TRUE, TRUE, 200),
('Teriyaki Glaze', 'Sweet teriyaki sauce', 'Topping', 0.50, 'Soy', 1, 15, TRUE, FALSE, 100),
('Ikura', 'Salmon roe caviar', 'Topping', 2.00, 'Fish', 1, 40, FALSE, TRUE, 15),
('Tempura Flakes', 'Crispy tempura bits', 'Topping', 0.50, 'Gluten', 1, 25, FALSE, FALSE, 80),

-- Sauces
('Spicy Mayo', 'Creamy spicy sauce', 'Sauce', 0.00, 'Eggs', 1, 50, FALSE, TRUE, 100),
('Wasabi', 'Traditional wasabi', 'Sauce', 0.00, '', 1, 5, TRUE, TRUE, 50),
('Eel Sauce', 'Sweet eel glaze', 'Sauce', 0.50, 'Soy', 1, 20, TRUE, FALSE, 60),
('Soy Sauce', 'Traditional soy sauce', 'Sauce', 0.00, 'Soy', 1, 8, TRUE, FALSE, 100),

-- Wrappers
('Nori Seaweed', 'Premium nori sheets', 'Wrapper', 0.00, '', 1, 10, TRUE, TRUE, 300),
('Soy Paper', 'Colored soy wrapper', 'Wrapper', 0.50, 'Soy', 1, 15, TRUE, FALSE, 100);

-- Insert Admin Users (for authentication)
-- Note: These are placeholder hashes - implement proper BCrypt hashing in production
INSERT INTO "AdminUsers" ("Username", "Email", "PasswordHash", "Role", "FullName", "Permissions") VALUES
('admin', 'admin@hidasushi.net', '$2a$11$dummy.hash.for.HidaSushi2024!', 'Admin', 'System Administrator', '["all"]'),
('jonathan', 'jonathan@hidasushi.net', '$2a$11$dummy.hash.for.ChefJonathan123!', 'Chef', 'Chef Jonathan', '["orders", "menu", "analytics"]'),
('kitchen', 'kitchen@hidasushi.net', '$2a$11$dummy.hash.for.Kitchen2024!', 'Kitchen', 'Kitchen Staff', '["orders"]');

-- Insert Demo Customer
INSERT INTO "Customers" ("FullName", "Email", "Phone", "PasswordHash", "DefaultDeliveryAddress") VALUES
('Demo Customer', 'demo@customer.com', '+32 470 12 34 56', '$2a$11$dummy.hash.for.demo123!', '123 Demo Street, Brussels, Belgium');

-- Create indexes for better performance
CREATE INDEX idx_orders_status ON "Orders"("Status");
CREATE INDEX idx_orders_created_at ON "Orders"("CreatedAt");
CREATE INDEX idx_orders_order_number ON "Orders"("OrderNumber");
CREATE INDEX idx_orders_customer_id ON "Orders"("CustomerId");
CREATE INDEX idx_order_items_order_id ON "OrderItems"("OrderId");
CREATE INDEX idx_sushi_rolls_available ON "SushiRolls"("IsAvailable");
CREATE INDEX idx_sushi_rolls_signature ON "SushiRolls"("IsSignatureRoll");
CREATE INDEX idx_ingredients_category ON "Ingredients"("Category");
CREATE INDEX idx_ingredients_available ON "Ingredients"("IsAvailable");
CREATE INDEX idx_admin_users_username ON "AdminUsers"("Username");
CREATE INDEX idx_admin_users_email ON "AdminUsers"("Email");
CREATE INDEX idx_customers_email ON "Customers"("Email");
CREATE INDEX idx_order_status_history_order_id ON "OrderStatusHistory"("OrderId");
CREATE INDEX idx_customer_addresses_customer_id ON "CustomerAddresses"("CustomerId");
CREATE INDEX idx_daily_analytics_date ON "DailyAnalytics"("Date");

-- Create triggers for updating timestamps
CREATE OR REPLACE FUNCTION update_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_customers_updated_at BEFORE UPDATE ON "Customers" FOR EACH ROW EXECUTE FUNCTION update_updated_at();
CREATE TRIGGER trigger_admin_users_updated_at BEFORE UPDATE ON "AdminUsers" FOR EACH ROW EXECUTE FUNCTION update_updated_at();
CREATE TRIGGER trigger_sushi_rolls_updated_at BEFORE UPDATE ON "SushiRolls" FOR EACH ROW EXECUTE FUNCTION update_updated_at();
CREATE TRIGGER trigger_ingredients_updated_at BEFORE UPDATE ON "Ingredients" FOR EACH ROW EXECUTE FUNCTION update_updated_at();
CREATE TRIGGER trigger_orders_updated_at BEFORE UPDATE ON "Orders" FOR EACH ROW EXECUTE FUNCTION update_updated_at();
CREATE TRIGGER trigger_daily_analytics_updated_at BEFORE UPDATE ON "DailyAnalytics" FOR EACH ROW EXECUTE FUNCTION update_updated_at();

-- Display success message
DO $$
BEGIN
    RAISE NOTICE '🎉 HIDA SUSHI Enhanced Database Created Successfully!';
    RAISE NOTICE '📊 Tables created: Customers, AdminUsers, SushiRolls, Ingredients, Orders, OrderItems, CustomRolls, OrderStatusHistory, CustomerAddresses, DailyAnalytics';
    RAISE NOTICE '🌱 Seed data inserted: % signature rolls, % ingredients, % admin users, % customers', 
        (SELECT COUNT(*) FROM "SushiRolls"), 
        (SELECT COUNT(*) FROM "Ingredients"),
        (SELECT COUNT(*) FROM "AdminUsers"),
        (SELECT COUNT(*) FROM "Customers");
    RAISE NOTICE '🔐 Admin credentials: admin/HidaSushi2024!, jonathan/ChefJonathan123!, kitchen/Kitchen2024!';
    RAISE NOTICE '👥 Customer demo: demo@customer.com/demo123!';
    RAISE NOTICE '🍣 Your enhanced HIDA SUSHI platform is ready for production!';
END $$; 