// Stripe integration functions
window.stripeRedirectToCheckout = (publishableKey, sessionId) => {
    const stripe = Stripe(publishableKey);
    
    return stripe.redirectToCheckout({
        sessionId: sessionId
    }).then(function (result) {
        if (result.error) {
            console.error('Stripe error:', result.error.message);
            throw new Error(result.error.message);
        }
    });
};

// Additional utility functions for payments
window.showPaymentModal = (message) => {
    // Simple modal for payment status
    alert(message);
};

window.logPaymentSuccess = (sessionId) => {
    console.log('Payment successful for session:', sessionId);
}; 