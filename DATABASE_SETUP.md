# 🍣 HIDA SUSHI Database Setup Guide

## 📋 Prerequisites

1. **SQL Server Express Installation**
   - Download SQL Server from: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
   - Choose "Express" edition
   - Install with default settings
   - Ensure SQL Server Express service is running

2. **SQL Server Command Line Tools**
   - Download from: https://docs.microsoft.com/en-us/sql/tools/sqlcmd-utility
   - Or install via SQL Server Management Studio (SSMS)

## 🚀 Quick Setup

### Option 1: Using PowerShell Script (Recommended)
```powershell
cd HidaSushi/HidaSushi.Server/Scripts
.\RunDatabaseSetup.ps1
```

### Option 2: Using Batch Script
```cmd
cd HidaSushi/HidaSushi.Server/Scripts
RunDatabaseSetup.bat
```

### Option 3: Manual Setup
1. Open SQL Server Management Studio (SSMS)
2. Connect to your SQL Server Express instance: `localhost\SQLEXPRESS`
3. Open the file: `HidaSushi/HidaSushi.Server/Scripts/CreateDatabase.sql`
4. Execute the script

## 🔐 Admin Credentials

After successful setup, you can log in with these credentials:

| Username | Password | Role |
|----------|----------|------|
| `admin` | `HidaSushi2024!` | System Administrator |
| `jonathan` | `ChefJonathan123!` | Chef |
| `kitchen` | `Kitchen2024!` | Kitchen Staff |

## 🗄️ Database Structure

The database includes the following tables:

- **Customers** - Customer information and preferences
- **CustomerAddresses** - Multiple delivery addresses per customer
- **AdminUsers** - Admin staff accounts with role-based permissions
- **SushiRolls** - Menu items with nutrition and analytics data
- **Ingredients** - Available ingredients with inventory tracking
- **Orders** - Order management with comprehensive tracking
- **OrderItems** - Individual items in orders
- **CustomRolls** - Build-your-own roll configurations
- **OrderStatusHistory** - Detailed order status tracking
- **DailyAnalytics** - Business intelligence and reporting

## 🔧 Troubleshooting

### Connection Issues
- Ensure SQL Server Express service is running
- Check that SQL Server Express is accessible on `localhost\SQLEXPRESS`
- Verify Windows Authentication is enabled
- Try using `(local)\SQLEXPRESS` instead of `localhost\SQLEXPRESS` in connection string

### Permission Issues
- Run scripts as Administrator
- Ensure your Windows account has SQL Server permissions
- Check SQL Server login settings

### Entity Framework Issues
- Clear any existing migrations: `dotnet ef database drop`
- Update database: `dotnet ef database update`
- Or let Entity Framework create the database: `dotnet run`

## 🚀 Running the Application

After database setup:

1. **Start the Server:**
   ```bash
   cd HidaSushi/HidaSushi.Server
   dotnet run
   ```

2. **Start the Admin Dashboard:**
   ```bash
   cd HidaSushi/HidaSushi.Admin
   dotnet run
   ```

3. **Start the Client Application:**
   ```bash
   cd HidaSushi/HidaSushi.Client
   dotnet run
   ```

## 📊 Features Included

- ✅ **Customer Management** - Registration, login, address management
- ✅ **Order Tracking** - Real-time order status updates
- ✅ **Menu Management** - Sushi rolls and ingredients
- ✅ **Analytics** - Business intelligence and reporting
- ✅ **Inventory Management** - Stock tracking and alerts
- ✅ **Payment Integration** - Stripe, Cash on Delivery, GodPay
- ✅ **Real-time Updates** - SignalR for live notifications
- ✅ **Role-based Access** - Admin, Chef, Kitchen staff permissions

## 🆘 Support

If you encounter issues:

1. Check SQL Server logs in Event Viewer
2. Verify connection string in `appsettings.json`
3. Ensure all required services are running
4. Check firewall settings for SQL Server port (1433)

---

**🍣 Your HIDA SUSHI platform is ready to serve!** 