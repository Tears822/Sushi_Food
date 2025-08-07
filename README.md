# 🍣 HIDA SUSHI - Premium Sushi Delivery Platform

> **Sushi by Jonathan** - Where Every Roll Tells a Story

A complete sushi ordering platform built with **Blazor WebAssembly**, **ASP.NET Core**, and **Supabase PostgreSQL**. Features include a pizza-style roll builder, real-time order tracking, and an admin dashboard for kitchen management.

## 🌟 Key Features

### 🎨 **Client Features**
- **Premium UI Design** - Purple & gold royal theme with smooth animations
- **Signature Rolls Menu** - Prime number pricing (€7, €13, €17, €19, €23)
- **Build Your Own Roll** - Pizza-style ingredient selection with real-time pricing
- **Real-time Order Tracking** - Visual timeline showing order progress
- **Mobile Responsive** - Works beautifully on all devices
- **Cart Management** - Floating cart with quantity selectors

### 👨‍🍳 **Admin Features**
- **Live Order Dashboard** - Real-time order management for kitchen staff
- **Order Status Updates** - One-click status changes with visual feedback
- **Daily Analytics** - Revenue, order counts, and performance metrics
- **Auto-refresh** - Updates every 30 seconds automatically

### 🏗️ **Technical Features**
- **Supabase PostgreSQL** - Real database integration with Entity Framework
- **RESTful API** - Complete CRUD operations for all entities
- **Real-time Updates** - SignalR for live order notifications
- **Prime Number Pricing** - Mathematical approach to menu pricing
- **Fallback Support** - Works offline with static data

## 🚀 Quick Start

### Prerequisites
- **.NET 9 SDK**
- **Visual Studio 2022** or **VS Code**
- **Supabase Account** (free tier available)

### 1. Clone the Repository
```bash
git clone https://github.com/your-username/HidaSushi.git
cd HidaSushi
```

### 2. Set Up Supabase Database

1. **Create Supabase Project**
   - Go to [supabase.com](https://supabase.com)
   - Create a new project
   - Note your project URL and anon key

2. **Update Connection String**
   
   Edit `HidaSushi.Server/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=aws-0-eu-central-1.pooler.supabase.com;Port=6543;Database=postgres;User Id=postgres.YOUR_PROJECT_ID;Password=YOUR_PASSWORD;"
     },
     "Supabase": {
       "Url": "https://YOUR_PROJECT_ID.supabase.co",
       "Key": "YOUR_ANON_KEY"
     }
   }
   ```

3. **Database Auto-Setup**
   - The application automatically creates tables on first run
   - Seed data is populated automatically

### 3. Run the Application

**Option A: Visual Studio**
1. Open `HidaSushi.sln`
2. Set multiple startup projects:
   - `HidaSushi.Server` (API)
   - `HidaSushi.Client` (Frontend)
   - `HidaSushi.Admin` (Admin Dashboard)
3. Press F5

**Option B: Command Line**
```bash
# Terminal 1 - API Server
cd HidaSushi.Server
dotnet run

# Terminal 2 - Client App
cd HidaSushi.Client
dotnet run

# Terminal 3 - Admin Dashboard
cd HidaSushi.Admin
dotnet run
```

## 📋 Project Structure

```
HidaSushi/
├── HidaSushi.Client/           # Blazor WebAssembly frontend
│   ├── Components/Pages/       # Page components
│   ├── Services/              # API service layer
│   └── wwwroot/               # Static assets & CSS
├── HidaSushi.Server/          # ASP.NET Core Web API
│   ├── Controllers/           # API controllers
│   ├── Data/                  # Database context
│   └── appsettings.json       # Configuration
├── HidaSushi.Admin/           # Blazor Server admin dashboard
│   └── Components/Pages/      # Admin pages
├── HidaSushi.Shared/          # Shared models & contracts
│   └── Models/                # Data models
└── README.md
```

## 🍕 Signature Rolls

The menu features prime number pricing with story-driven names:

| Roll | Price | Description |
|------|-------|-------------|
| **Taylor Swift – Tortured Poets Dragon Roll** | €13 | 🐉 Avocado top, double tuna, spicy crunch, seaweed pearls, tempura shrimp |
| **Blackbird Rainbow Roll** | €17 | 🐦 5-piece nigiri symphony: salmon, tuna, scallop, eel, yellowtail |
| **M&M "Beautiful" Roll** | €19 | 👑 Big, bold, meaty — salmon + wagyu + wasabi aioli |
| **Joker Laughing Volcano Roll** | €23 | 🃏 Spicy tuna, cream cheese, topped with flamed jalapeño mayo |
| **Garden of Eden Veggie Roll** | €7 | 🌱 Fresh vegetables, avocado, cucumber, perfect for plant-based lovers |

## 🎨 Build Your Own Roll

Pizza-style configurator with 7 steps:
1. **Roll Type** - Normal, Inside-Out, Cucumber Wrap
2. **Base** - Sushi Rice, Brown Rice (€15 base price)
3. **Proteins** - Tuna, Salmon, Shrimp, Tofu, etc. (max 3)
4. **Vegetables** - Avocado, Cucumber, Carrot, Lettuce
5. **Premium Extras** - Goat Cheese (+€1), Mango (+€1), etc.
6. **Toppings** - Sesame Seeds, Teriyaki Glaze, Ikura (+€2)
7. **Sauces** - Spicy Mayo, Wasabi, Eel Sauce

Real-time price calculation with allergen warnings.

## 📦 Order Tracking

Visual timeline showing order progress:
- ✅ **Received** - Order confirmed
- 👨‍🍳 **In Preparation** - Kitchen is cooking
- 📦 **Ready** - Available for pickup
- 🛵 **Out for Delivery** - On the way (delivery only)
- ✔️ **Completed** - Delivered/picked up

## 🔧 API Endpoints

### Sushi Rolls
- `GET /api/SushiRolls` - Get all available rolls
- `GET /api/SushiRolls/signature` - Get signature rolls only
- `GET /api/SushiRolls/vegetarian` - Get vegetarian options

### Ingredients
- `GET /api/Ingredients` - Get all ingredients
- `GET /api/Ingredients/category/{category}` - Get by category
- `POST /api/Ingredients/calculate-price` - Calculate custom roll price

### Orders
- `POST /api/Orders` - Create new order
- `GET /api/Orders/track/{orderNumber}` - Track order
- `GET /api/Orders/pending` - Get pending orders (admin)
- `PUT /api/Orders/{id}/status` - Update order status

## 🏪 Admin Dashboard

Kitchen staff interface at `/admin`:
- **Live Order Feed** - Real-time incoming orders
- **One-Click Status Updates** - Move orders through workflow
- **Daily Statistics** - Revenue and order metrics
- **Auto-refresh** - Updates every 30 seconds
- **Order Age Tracking** - See how long orders have been pending

## 🎭 Design Philosophy

### Prime Number Pricing
Every price is a prime number (7, 13, 17, 19, 23) representing:
- **Mathematical Beauty** - Irreducible and elegant
- **Artistic Intention** - Each price reflects craftsmanship
- **Memorable Pricing** - Easy to remember and unique

### Purple & Gold Theme
- **Royal Purple** (#6B46C1) - Premium, sophisticated
- **Royal Gold** (#F59E0B) - Luxury, warmth
- **Smooth Gradients** - Modern, elegant transitions
- **Story-driven Names** - Each roll tells a tale

## 🌍 Global Vision

Designed for worldwide expansion:
- **Multi-location Support** - Built into data models
- **Consistent Experience** - Same quality everywhere
- **Technology-enabled** - Real-time tracking and management
- **Scalable Architecture** - Ready for multiple markets

## 🤝 Social Impact

**"Every Roll Feeds Two"**
- **Employment for Rebuilding Lives** - Hiring people getting back on their feet
- **Community Focus** - Food brings people together
- **Second Chances** - Using business for social good

## 🛠️ Technology Stack

- **Frontend**: Blazor WebAssembly, TailwindCSS
- **Backend**: ASP.NET Core Web API (.NET 9)
- **Database**: Supabase PostgreSQL
- **ORM**: Entity Framework Core
- **Authentication**: JWT (for admin)
- **Real-time**: SignalR (planned)
- **Payments**: Stripe integration (planned)
- **Hosting**: Azure App Service ready

## 🚧 Roadmap

### Phase 1 (MVP) ✅
- [x] Signature rolls menu
- [x] Build-your-own roll configurator
- [x] Order tracking
- [x] Admin dashboard
- [x] Supabase integration

### Phase 2 (Enhancement)
- [ ] Stripe payment integration
- [ ] Real-time SignalR notifications
- [ ] Email order confirmations
- [ ] Mobile app (MAUI)
- [ ] Multiple location support

### Phase 3 (Scale)
- [ ] Advanced analytics
- [ ] Inventory management
- [ ] Customer accounts
- [ ] Loyalty program
- [ ] Multi-language support

## 📞 Contact

**Chef Jonathan Lurié**
- 📧 Email: info@hidasushi.net
- 📱 Phone: +32 470 42 82 90
- 📍 Location: Brussels, Belgium
- 🌐 Website: https://hidasushi.net

## 📝 License

This project is proprietary software for HIDA SUSHI operations.

---

## 🎯 Quick Commands

```bash
# Start development servers
dotnet run --project HidaSushi.Server
dotnet run --project HidaSushi.Client
dotnet run --project HidaSushi.Admin

# Build for production
dotnet publish HidaSushi.Server -c Release
dotnet publish HidaSushi.Client -c Release

# Database migrations (if needed)
dotnet ef migrations add InitialCreate --project HidaSushi.Server
dotnet ef database update --project HidaSushi.Server
```

**Ready to serve premium sushi with technology! 🍣✨** 