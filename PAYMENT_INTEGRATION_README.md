# Payment Integration Documentation

This document describes the complete payment integration implementation for the HidaSushi project, including both Stripe and PayPal payment gateways.

## Overview

The project now supports three payment methods:
1. **Stripe** - Credit/Debit card payments via Stripe
2. **PayPal** - PayPal account payments 
3. **Cash on Delivery** - Traditional cash payment upon order delivery/pickup

## Implementation Status

### ✅ Stripe Integration - FULLY IMPLEMENTED
- ✅ Stripe.NET SDK integration (v46.4.0)
- ✅ Server-side StripeService with checkout sessions
- ✅ Client-side StripeService with JavaScript integration
- ✅ Webhook handling for payment confirmations
- ✅ Proper error handling and logging
- ✅ Security features (signature verification)

### ✅ PayPal Integration - FULLY IMPLEMENTED  
- ✅ PayPal REST API integration via RestSharp
- ✅ Server-side PayPalService with order creation/capture
- ✅ Client-side PayPalService with JavaScript integration
- ✅ PayPal Buttons component for seamless checkout
- ✅ OAuth token management
- ✅ Proper error handling and logging

## Configuration Required

### 1. Stripe Configuration

Update `appsettings.json` with your Stripe credentials:

```json
{
  "Stripe": {
    "PublishableKey": "pk_test_YOUR_PUBLISHABLE_KEY",
    "SecretKey": "sk_test_YOUR_SECRET_KEY", 
    "WebhookSecret": "whsec_YOUR_WEBHOOK_SECRET"
  }
}
```

### 2. PayPal Configuration

Update `appsettings.json` with your PayPal credentials:

```json
{
  "PayPal": {
    "ClientId": "YOUR_PAYPAL_CLIENT_ID",
    "AppSecret": "YOUR_PAYPAL_APP_SECRET",
    "BaseUrl": "https://api-m.sandbox.paypal.com"
  }
}
```

**Note**: For production, change `BaseUrl` to `https://api-m.paypal.com`

### 3. Frontend Configuration

Update the PayPal Client ID in `PayPalButtons.razor`:
```javascript
await JSRuntime.InvokeVoidAsync("loadPayPalSDK", "YOUR_PAYPAL_CLIENT_ID");
```

Update the Stripe Publishable Key in `StripeService.cs`:
```csharp
await _jsRuntime.InvokeVoidAsync("stripeRedirectToCheckout", "pk_test_YOUR_PUBLISHABLE_KEY", sessionId);
```

## Architecture

### Server-Side Components

1. **Services/StripeService.cs** - Handles Stripe API integration
   - Create checkout sessions
   - Create payment intents  
   - Webhook signature verification
   - Event processing

2. **Services/PayPalService.cs** - Handles PayPal API integration
   - OAuth token management
   - Order creation
   - Payment capture
   - Error handling

3. **Controllers/PaymentController.cs** - Payment processing endpoints
   - `/api/Payment/process` - Generic payment processing
   - `/api/Payment/stripe/create-checkout-session` - Stripe session creation
   - `/api/Payment/stripe/webhook` - Stripe webhook handler
   - `/api/Payment/paypal/create-order` - PayPal order creation
   - `/api/Payment/paypal/capture-payment/{orderId}` - PayPal payment capture

### Client-Side Components

1. **Services/StripeService.cs** - Client-side Stripe integration
   - Checkout session creation
   - Redirect to Stripe checkout
   - Payment verification

2. **Services/PayPalService.cs** - Client-side PayPal integration  
   - Order creation calls
   - Payment capture calls
   - JavaScript integration

3. **Components/PayPalButtons.razor** - PayPal button component
   - Renders PayPal payment buttons
   - Handles payment flow
   - JavaScript interop for PayPal SDK

4. **wwwroot/js/stripe.js** - Stripe JavaScript functions
   - Stripe SDK integration
   - Checkout redirection
   - Elements initialization

5. **wwwroot/js/paypal.js** - PayPal JavaScript functions
   - PayPal SDK integration
   - Button rendering
   - Payment flow handling

## Payment Flow

### Stripe Payment Flow
1. User selects Stripe payment method
2. User enters card details in the form
3. User clicks "Place Order"
4. System creates Stripe checkout session
5. User is redirected to Stripe checkout
6. Payment is processed by Stripe
7. Webhook confirms payment success
8. Order is marked as paid

### PayPal Payment Flow  
1. User selects PayPal payment method
2. PayPal buttons are rendered
3. User clicks PayPal button
4. System creates PayPal order
5. User is redirected to PayPal
6. User authorizes payment in PayPal
7. System captures the payment
8. Order is marked as paid

### Cash on Delivery Flow
1. User selects Cash on Delivery
2. User clicks "Place Order"
3. Order is created with pending payment status
4. Payment will be collected upon delivery/pickup

## Security Features

### Stripe Security
- ✅ Webhook signature verification
- ✅ HTTPS enforcement
- ✅ Secure API key management
- ✅ PCI compliance via Stripe

### PayPal Security  
- ✅ OAuth token authentication
- ✅ HTTPS enforcement
- ✅ Secure credential management
- ✅ PayPal's fraud protection

## Testing

### Stripe Test Cards
Use these test card numbers for testing:
- **Visa**: 4242424242424242
- **Mastercard**: 5555555555554444
- **Declined**: 4000000000000002

### PayPal Testing
- Use PayPal sandbox accounts
- Test with sandbox.paypal.com
- Use test credentials from PayPal Developer Dashboard

## Error Handling

Both integrations include comprehensive error handling:
- Network errors
- API errors  
- Validation errors
- Payment failures
- Webhook processing errors

Errors are logged with structured logging and displayed to users with user-friendly messages.

## Dependencies Added

### Server Dependencies
```xml
<PackageReference Include="Stripe.net" Version="46.4.0" />
<PackageReference Include="RestSharp" Version="112.1.0" />
```

### Client Dependencies
- No additional NuGet packages required
- JavaScript SDKs loaded dynamically

## Production Checklist

### Before Going Live:

1. **Stripe**:
   - [ ] Replace test keys with live keys
   - [ ] Set up production webhooks
   - [ ] Configure webhook endpoints
   - [ ] Test with real cards (small amounts)

2. **PayPal**:
   - [ ] Replace sandbox credentials with live credentials  
   - [ ] Change BaseUrl to production URL
   - [ ] Set up production app in PayPal dashboard
   - [ ] Test with real PayPal accounts

3. **Security**:
   - [ ] Enable HTTPS in production
   - [ ] Secure credential storage (Azure Key Vault, etc.)
   - [ ] Set up monitoring and alerting
   - [ ] Configure rate limiting

4. **Testing**:
   - [ ] End-to-end payment testing
   - [ ] Webhook delivery testing
   - [ ] Error scenario testing
   - [ ] Mobile responsiveness testing

## Support & Troubleshooting

### Common Issues:

1. **Stripe webhooks not working**:
   - Check webhook URL is publicly accessible
   - Verify webhook secret is correct
   - Check webhook signature verification

2. **PayPal orders failing**:
   - Verify OAuth credentials are correct
   - Check PayPal app is in correct mode (sandbox/live)
   - Ensure proper currency settings

3. **JavaScript errors**:
   - Check SDKs are loading correctly
   - Verify HTTPS is enabled
   - Check browser console for errors

### Logging

All payment operations are logged with structured logging. Check application logs for detailed error information.

### Contact

For technical support, refer to:
- Stripe Documentation: https://stripe.com/docs
- PayPal Developer Documentation: https://developer.paypal.com/docs