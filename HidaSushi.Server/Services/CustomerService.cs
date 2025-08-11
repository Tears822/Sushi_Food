using Microsoft.EntityFrameworkCore;
using HidaSushi.Server.Data;
using HidaSushi.Shared.Models;
using System.Security.Cryptography;
using System.Text;

namespace HidaSushi.Server.Services;

public class CustomerService
{
    private readonly HidaSushiDbContext _context;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(HidaSushiDbContext context, ILogger<CustomerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Customer?> GetCustomerByEmailAsync(string email)
    {
        return await _context.Customers
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Email == email && c.IsActive);
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        return await _context.Customers
            .Include(c => c.Addresses)
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
    }

    public async Task<Customer> RegisterCustomerAsync(string fullName, string email, string password, string phone = "")
    {
        // Check if email already exists
        var existingCustomer = await GetCustomerByEmailAsync(email);
        if (existingCustomer != null)
        {
            throw new InvalidOperationException("Email already registered");
        }

        // Hash password
        var passwordHash = HashPassword(password);

        var customer = new Customer
        {
            FullName = fullName,
            Email = email,
            Phone = phone,
            PasswordHash = passwordHash,
            IsActive = true,
            EmailVerified = false, // Would need email verification in production
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New customer registered: {Email}", email);
        return customer;
    }

    public async Task<Customer?> ValidateCustomerLoginAsync(string email, string password)
    {
        var customer = await GetCustomerByEmailAsync(email);
        if (customer == null)
        {
            return null;
        }

        if (VerifyPassword(password, customer.PasswordHash))
        {
            customer.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return customer;
        }

        return null;
    }

    public async Task<List<CustomerAddress>> GetCustomerAddressesAsync(int customerId)
    {
        return await _context.CustomerAddresses
            .Where(ca => ca.CustomerId == customerId)
            .OrderBy(ca => ca.IsDefault)
            .ThenBy(ca => ca.Label)
            .ToListAsync();
    }

    public async Task<CustomerAddress> AddCustomerAddressAsync(int customerId, CustomerAddress address)
    {
        address.CustomerId = customerId;
        address.CreatedAt = DateTime.UtcNow;

        // If this is the first address, make it default
        var existingAddresses = await GetCustomerAddressesAsync(customerId);
        if (!existingAddresses.Any())
        {
            address.IsDefault = true;
        }

        _context.CustomerAddresses.Add(address);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added address for customer {CustomerId}: {Label}", customerId, address.Label);
        return address;
    }

    public async Task UpdateCustomerPreferencesAsync(int customerId, CustomerPreferences preferences)
    {
        var customer = await GetCustomerByIdAsync(customerId);
        if (customer == null)
        {
            throw new InvalidOperationException("Customer not found");
        }

        customer.PreferencesJson = System.Text.Json.JsonSerializer.Serialize(preferences);
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated preferences for customer {CustomerId}", customerId);
    }

    public async Task<List<Order>> GetCustomerOrdersAsync(int customerId)
    {
        return await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.SushiRoll)
            .Include(o => o.Items)
                .ThenInclude(oi => oi.CustomRoll)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private bool VerifyPassword(string password, string hash)
    {
        var passwordHash = HashPassword(password);
        return passwordHash == hash;
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        try
        {
            customer.CreatedAt = DateTime.UtcNow;
            customer.UpdatedAt = DateTime.UtcNow;
            
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Customer created: {CustomerId} - {Email}", customer.Id, customer.Email);
            return customer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer: {Email}", customer.Email);
            throw;
        }
    }

    public async Task<Customer> UpdateCustomerAsync(Customer customer)
    {
        try
        {
            customer.UpdatedAt = DateTime.UtcNow;
            
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Customer updated: {CustomerId} - {Email}", customer.Id, customer.Email);
            return customer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer: {CustomerId}", customer.Id);
            throw;
        }
    }
} 