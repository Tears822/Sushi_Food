using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HidaSushi.Shared.Models;
using HidaSushi.Server.Services;
using Microsoft.AspNetCore.Identity;

namespace HidaSushi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly CustomerService _customerService;
    private readonly ILogger<CustomerController> _logger;
    private readonly JwtService _jwtService;

    public CustomerController(CustomerService customerService, ILogger<CustomerController> logger, JwtService jwtService)
    {
        _customerService = customerService;
        _logger = logger;
        _jwtService = jwtService;
    }

    // TEST: Debug endpoint to see exactly what we're receiving
    [HttpPost("test-registration")]
    [AllowAnonymous]
    public ActionResult TestRegistration([FromBody] object request)
    {
        try
        {
            _logger.LogInformation("Raw registration test request: {Request}", request?.ToString());
            
            if (request != null)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(request);
                _logger.LogInformation("Serialized test request: {Json}", json);
            }
            
            return Ok(new { Message = "Test endpoint received data", Data = request });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test registration endpoint");
            return StatusCode(500, new { Message = "Test failed", Error = ex.Message });
        }
    }

    // POST: api/Customer/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<CustomerRegistrationResult>> Register([FromBody] CustomerRegistrationRequest request)
    {
        try
        {
            _logger.LogInformation("Customer registration request received");
            
            // Log detailed request information for debugging
            if (request == null)
            {
                _logger.LogError("Registration request is null");
                return BadRequest(new CustomerRegistrationResult 
                { 
                    Success = false, 
                    ErrorMessage = "Invalid request data" 
                });
            }

            _logger.LogInformation("Registration request details - Email: {Email}, FullName: {FullName}, Password: {PasswordPresent}, Phone: {Phone}", 
                request.Email ?? "NULL", 
                request.FullName ?? "NULL", 
                !string.IsNullOrEmpty(request.Password) ? "YES" : "NO",
                request.Phone ?? "NULL");

            // Validate request
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.FullName) || string.IsNullOrEmpty(request.Password))
            {
                _logger.LogWarning("Registration validation failed - Email: {EmailEmpty}, FullName: {FullNameEmpty}, Password: {PasswordEmpty}",
                    string.IsNullOrEmpty(request.Email),
                    string.IsNullOrEmpty(request.FullName),
                    string.IsNullOrEmpty(request.Password));
                    
                return BadRequest(new CustomerRegistrationResult 
                { 
                    Success = false, 
                    ErrorMessage = "Full name, email, and password are required" 
                });
            }

            // Check if customer already exists
            var existingCustomer = await _customerService.GetCustomerByEmailAsync(request.Email);
            if (existingCustomer != null)
            {
                _logger.LogWarning("Registration failed - email already exists: {Email}", request.Email);
                return Conflict(new CustomerRegistrationResult 
                { 
                    Success = false, 
                    ErrorMessage = "Customer with this email already exists" 
                });
            }

            // Register customer using the service (which handles password hashing)
            var customer = await _customerService.RegisterCustomerAsync(
                request.FullName, 
                request.Email, 
                request.Password, 
                request.Phone ?? ""
            );
            
            _logger.LogInformation("Customer registered successfully: {CustomerId} - {Email}", customer.Id, customer.Email);

            return Ok(new CustomerRegistrationResult 
            { 
                Success = true, 
                CustomerId = customer.Id,
                Message = "Registration successful" 
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already registered"))
        {
            _logger.LogWarning("Registration failed - email already registered: {Email}", request?.Email);
            return Conflict(new CustomerRegistrationResult 
            { 
                Success = false, 
                ErrorMessage = "Email already registered" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering customer with email: {Email}", request?.Email);
            return StatusCode(500, new CustomerRegistrationResult 
            { 
                Success = false, 
                ErrorMessage = "Registration failed. Please try again." 
            });
        }
    }

    // POST: api/Customer/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Customer login attempt for email: {Email}", request.Username);
            
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new LoginResponse 
                { 
                    Success = false, 
                    Message = "Email and password are required" 
                });
            }

            // Use the existing ValidateCustomerLoginAsync method
            var customer = await _customerService.ValidateCustomerLoginAsync(request.Username, request.Password);
            if (customer == null)
            {
                _logger.LogWarning("Customer login failed for email: {Email}", request.Username);
                return Unauthorized(new LoginResponse 
                { 
                    Success = false, 
                    Message = "Invalid email or password" 
                });
            }

            // Generate customer token
            var response = _jwtService.GenerateCustomerToken(customer.Email, customer.Id, customer.FullName);
            _logger.LogInformation("Customer {Email} logged in successfully", customer.Email);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during customer login for email: {Email}", request.Username);
            return StatusCode(500, new LoginResponse 
            { 
                Success = false, 
                Message = "Login failed. Please try again." 
            });
        }
    }

    // GET: api/Customer/{email}
    [HttpGet("{email}")]
    [AllowAnonymous]
    public async Task<ActionResult<Customer>> GetCustomer(string email)
    {
        try
        {
            var customer = await _customerService.GetCustomerByEmailAsync(email);
            if (customer == null)
            {
                return NotFound();
            }

            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer: {Email}", email);
            return StatusCode(500, new { message = "Error retrieving customer" });
        }
    }

    // PUT: api/Customer/{id}
    [HttpPut("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Customer>> UpdateCustomer(int id, [FromBody] CustomerUpdateRequest request)
    {
        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            // Update customer properties
            customer.FullName = request.FullName ?? customer.FullName;
            customer.Phone = request.Phone ?? customer.Phone;
            customer.UpdatedAt = DateTime.UtcNow;
            // Note: Address handling would require CustomerAddress entity

            var updatedCustomer = await _customerService.UpdateCustomerAsync(customer);
            
            _logger.LogInformation("Customer updated: {CustomerId}", id);
            return Ok(updatedCustomer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer: {CustomerId}", id);
            return StatusCode(500, new { message = "Error updating customer" });
        }
    }

    // GET: api/Customer/{id}/orders
    [HttpGet("{id}/orders")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Order>>> GetCustomerOrders(int id)
    {
        try
        {
            var orders = await _customerService.GetCustomerOrdersAsync(id);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders for customer: {CustomerId}", id);
            return StatusCode(500, new { message = "Error retrieving orders" });
        }
    }

    // POST: api/Customer/guest-order
    [HttpPost("guest-order")]
    [AllowAnonymous]
    public async Task<ActionResult<GuestOrderResult>> CreateGuestOrder([FromBody] GuestOrderRequest request)
    {
        try
        {
            _logger.LogInformation("Guest order request for: {Email}", request.Email);

            // Create or get existing customer
            var customer = await _customerService.GetCustomerByEmailAsync(request.Email);
            if (customer == null)
            {
                customer = new Customer
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    Phone = request.Phone ?? "",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                    // Note: IsGuest property doesn't exist in current model
                };
                customer = await _customerService.CreateCustomerAsync(customer);
            }

            return Ok(new GuestOrderResult 
            { 
                Success = true, 
                CustomerId = customer.Id,
                Message = "Guest customer created/retrieved successfully" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating guest order for: {Email}", request.Email);
            return StatusCode(500, new GuestOrderResult 
            { 
                Success = false, 
                ErrorMessage = "Failed to process guest order" 
            });
        }
    }
} 