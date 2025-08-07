using HidaSushi.Shared.Models;
using Microsoft.Extensions.Logging;

namespace HidaSushi.Client.Services;

public interface ICartService
{
    event Action? OnCartChanged;
    List<CartItem> GetCartItems();
    void AddItem(SushiRoll roll, int quantity = 1);
    void AddCustomRoll(CustomRoll customRoll, int quantity = 1);
    void RemoveItem(int cartItemId);
    void UpdateQuantity(int cartItemId, int quantity);
    void ClearCart();
    int GetTotalItemCount();
    decimal GetTotalPrice();
    bool HasItems();
}

public class CartService : ICartService
{
    private readonly List<CartItem> _cartItems = new();
    private int _nextId = 1;
    private readonly ILogger<CartService> _logger;

    public CartService(ILogger<CartService> logger)
    {
        _logger = logger;
    }

    public event Action? OnCartChanged;

    public List<CartItem> GetCartItems() 
    {
        try
        {
            return _cartItems.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart items");
            return new List<CartItem>();
        }
    }

    public void AddItem(SushiRoll roll, int quantity = 1)
    {
        try
        {
            if (roll == null)
            {
                _logger.LogWarning("Attempted to add null roll to cart");
                return;
            }

            if (quantity <= 0)
            {
                _logger.LogWarning("Attempted to add invalid quantity {Quantity} for roll {RollName}", quantity, roll.Name);
                return;
            }

            var existingItem = _cartItems.FirstOrDefault(item => 
                item.SushiRoll?.Id == roll.Id && item.CustomRoll == null);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.TotalPrice = existingItem.Quantity * roll.Price;
                _logger.LogInformation("Updated quantity for {RollName} to {Quantity}", roll.Name, existingItem.Quantity);
            }
            else
            {
                _cartItems.Add(new CartItem
                {
                    Id = _nextId++,
                    SushiRoll = roll,
                    Quantity = quantity,
                    UnitPrice = roll.Price,
                    TotalPrice = roll.Price * quantity,
                    Name = roll.Name
                });
                _logger.LogInformation("Added {Quantity}x {RollName} to cart", quantity, roll.Name);
            }

            OnCartChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item {RollName} to cart", roll?.Name ?? "Unknown");
        }
    }

    public void AddCustomRoll(CustomRoll customRoll, int quantity = 1)
    {
        try
        {
            if (customRoll == null)
            {
                _logger.LogWarning("Attempted to add null custom roll to cart");
                return;
            }

            if (quantity <= 0)
            {
                _logger.LogWarning("Attempted to add invalid quantity {Quantity} for custom roll {RollName}", quantity, customRoll.Name);
                return;
            }

            _cartItems.Add(new CartItem
            {
                Id = _nextId++,
                CustomRoll = customRoll,
                Quantity = quantity,
                UnitPrice = customRoll.TotalPrice,
                TotalPrice = customRoll.TotalPrice * quantity,
                Name = customRoll.Name
            });

            _logger.LogInformation("Added {Quantity}x custom roll {RollName} to cart", quantity, customRoll.Name);
            OnCartChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding custom roll {RollName} to cart", customRoll?.Name ?? "Unknown");
        }
    }

    public void RemoveItem(int cartItemId)
    {
        try
        {
            var removedCount = _cartItems.RemoveAll(item => item.Id == cartItemId);
            if (removedCount > 0)
            {
                _logger.LogInformation("Removed cart item {CartItemId}", cartItemId);
                OnCartChanged?.Invoke();
            }
            else
            {
                _logger.LogWarning("Attempted to remove non-existent cart item {CartItemId}", cartItemId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item {CartItemId}", cartItemId);
        }
    }

    public void UpdateQuantity(int cartItemId, int quantity)
    {
        try
        {
            var item = _cartItems.FirstOrDefault(i => i.Id == cartItemId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    RemoveItem(cartItemId);
                }
                else
                {
                    item.Quantity = quantity;
                    item.TotalPrice = item.UnitPrice * quantity;
                    _logger.LogInformation("Updated quantity for cart item {CartItemId} to {Quantity}", cartItemId, quantity);
                    OnCartChanged?.Invoke();
                }
            }
            else
            {
                _logger.LogWarning("Attempted to update quantity for non-existent cart item {CartItemId}", cartItemId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quantity for cart item {CartItemId}", cartItemId);
        }
    }

    public void ClearCart()
    {
        try
        {
            var itemCount = _cartItems.Count;
            _cartItems.Clear();
            _logger.LogInformation("Cleared cart with {ItemCount} items", itemCount);
            OnCartChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
        }
    }

    public int GetTotalItemCount()
    {
        try
        {
            return _cartItems.Sum(item => item.Quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total item count");
            return 0;
        }
    }

    public decimal GetTotalPrice()
    {
        try
        {
            return _cartItems.Sum(item => item.TotalPrice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total price");
            return 0m;
        }
    }

    public bool HasItems()
    {
        try
        {
            return _cartItems.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if cart has items");
            return false;
        }
    }
}

public class CartItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SushiRoll? SushiRoll { get; set; }
    public CustomRoll? CustomRoll { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string Notes { get; set; } = string.Empty;
} 