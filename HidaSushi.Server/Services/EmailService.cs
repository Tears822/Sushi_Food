using System.Net.Mail;
using System.Net;
using HidaSushi.Shared.Models;

namespace HidaSushi.Server.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendOrderConfirmationAsync(Order order)
    {
        try
        {
            var subject = $"Order Confirmation - {order.OrderNumber}";
            var body = GenerateOrderConfirmationEmail(order);

            await SendEmailAsync(order.CustomerEmail, subject, body);
            
            _logger.LogInformation("Order confirmation email sent for order {OrderNumber}", order.OrderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order confirmation email for order {OrderNumber}", order.OrderNumber);
        }
    }

    public async Task SendOrderStatusUpdateAsync(Order order, string previousStatus, string newStatus)
    {
        try
        {
            var subject = $"Order Update - {order.OrderNumber}";
            var body = GenerateOrderStatusUpdateEmail(order, previousStatus, newStatus);

            await SendEmailAsync(order.CustomerEmail, subject, body);
            
            _logger.LogInformation("Order status update email sent for order {OrderNumber}", order.OrderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order status update email for order {OrderNumber}", order.OrderNumber);
        }
    }

    public async Task SendWelcomeEmailAsync(Customer customer)
    {
        try
        {
            var subject = "Welcome to HIDA SUSHI! 🍣";
            var body = GenerateWelcomeEmail(customer);

            await SendEmailAsync(customer.Email, subject, body);
            
            _logger.LogInformation("Welcome email sent to {Email}", customer.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", customer.Email);
        }
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpServer = _configuration["Email:SmtpServer"];
        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];

        if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            _logger.LogWarning("Email configuration incomplete, skipping email send");
            return;
        }

        using var client = new SmtpClient(smtpServer, smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(username, password)
        };

        var message = new MailMessage
        {
            From = new MailAddress(username, "HIDA SUSHI"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message);
    }

    private string GenerateOrderConfirmationEmail(Order order)
    {
        return $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 20px; }}
                    .order-details {{ background: #f9f9f9; padding: 15px; border-radius: 5px; margin: 20px 0; }}
                    .item {{ margin: 10px 0; padding: 10px; border-left: 3px solid #667eea; }}
                    .total {{ font-weight: bold; font-size: 18px; color: #667eea; }}
                    .footer {{ background: #f5f5f5; padding: 15px; text-align: center; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <h1>🍣 HIDA SUSHI</h1>
                    <h2>Order Confirmation</h2>
                </div>
                <div class='content'>
                    <p>Dear {order.CustomerName},</p>
                    <p>Thank you for your order! We're excited to prepare your delicious sushi.</p>
                    
                    <div class='order-details'>
                        <h3>Order Details</h3>
                        <p><strong>Order Number:</strong> {order.OrderNumber}</p>
                        <p><strong>Order Date:</strong> {order.CreatedAt:g}</p>
                        <p><strong>Order Type:</strong> {order.Type}</p>
                        <p><strong>Estimated Delivery:</strong> {order.EstimatedDeliveryTime:g}</p>
                        
                        <h4>Items:</h4>
                        {string.Join("", order.Items.Select(item => $@"
                            <div class='item'>
                                <strong>{item.Quantity}x</strong> {GetItemName(item)} - €{item.Price:F2}
                            </div>"))}
                        
                        <div class='total'>
                            <p>Subtotal: €{order.SubtotalAmount:F2}</p>
                            <p>Delivery Fee: €{order.DeliveryFee:F2}</p>
                            <p>Tax: €{order.TaxAmount:F2}</p>
                            <p>Total: €{order.TotalAmount:F2}</p>
                        </div>
                    </div>
                    
                    <p>Track your order: <a href='https://hidasushi.com/track/{order.OrderNumber}'>Click here</a></p>
                </div>
                <div class='footer'>
                    <p>HIDA SUSHI - Sushi by Jonathan</p>
                    <p>Questions? Contact us at info@hidasushi.com</p>
                </div>
            </body>
            </html>";
    }

    private string GenerateOrderStatusUpdateEmail(Order order, string previousStatus, string newStatus)
    {
        return $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 20px; }}
                    .status-update {{ background: #e8f5e8; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 5px solid #4caf50; }}
                    .footer {{ background: #f5f5f5; padding: 15px; text-align: center; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <h1>🍣 HIDA SUSHI</h1>
                    <h2>Order Status Update</h2>
                </div>
                <div class='content'>
                    <p>Dear {order.CustomerName},</p>
                    
                    <div class='status-update'>
                        <h3>Your order status has been updated!</h3>
                        <p><strong>Order Number:</strong> {order.OrderNumber}</p>
                        <p><strong>Previous Status:</strong> {previousStatus}</p>
                        <p><strong>New Status:</strong> {newStatus}</p>
                    </div>
                    
                    <p>Track your order: <a href='https://hidasushi.com/track/{order.OrderNumber}'>Click here</a></p>
                </div>
                <div class='footer'>
                    <p>HIDA SUSHI - Sushi by Jonathan</p>
                    <p>Questions? Contact us at info@hidasushi.com</p>
                </div>
            </body>
            </html>";
    }

    private string GenerateWelcomeEmail(Customer customer)
    {
        return $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 20px; }}
                    .welcome {{ background: #f0f8ff; padding: 15px; border-radius: 5px; margin: 20px 0; }}
                    .footer {{ background: #f5f5f5; padding: 15px; text-align: center; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <h1>🍣 HIDA SUSHI</h1>
                    <h2>Welcome to the Family!</h2>
                </div>
                <div class='content'>
                    <p>Dear {customer.FullName},</p>
                    
                    <div class='welcome'>
                        <h3>Welcome to HIDA SUSHI! 🎉</h3>
                        <p>We're thrilled to have you join our sushi family. Get ready for an amazing culinary journey with Chef Jonathan's signature rolls.</p>
                    </div>
                    
                    <p>As a new member, you'll enjoy:</p>
                    <ul>
                        <li>🎨 Build your own custom rolls</li>
                        <li>📱 Real-time order tracking</li>
                        <li>🎁 Loyalty rewards</li>
                        <li>🚚 Fast delivery & pickup</li>
                    </ul>
                    
                    <p>Ready to order? <a href='https://hidasushi.com/menu'>Browse our menu</a></p>
                </div>
                <div class='footer'>
                    <p>HIDA SUSHI - Sushi by Jonathan</p>
                    <p>Questions? Contact us at info@hidasushi.com</p>
                </div>
            </body>
            </html>";
    }

    private string GetItemName(OrderItem item)
    {
        if (item.SushiRoll != null)
            return item.SushiRoll.Name;
        if (item.CustomRoll != null)
            return item.CustomRoll.Name;
        return "Custom Item";
    }
} 