// Stripe integration functions
window.stripeRedirectToCheckout = (publishableKey, sessionId) => {
    if (!window.Stripe) {
        console.error('Stripe SDK not loaded');
        throw new Error('Stripe SDK not loaded');
    }

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

// Initialize Stripe Elements for card input
window.initializeStripeElements = (publishableKey, clientSecret, elementId) => {
    if (!window.Stripe) {
        console.error('Stripe SDK not loaded');
        return null;
    }

    const stripe = Stripe(publishableKey);
    const elements = stripe.elements({
        clientSecret: clientSecret
    });

    const cardElement = elements.create('card', {
        style: {
            base: {
                fontSize: '16px',
                color: '#424770',
                '::placeholder': {
                    color: '#aab7c4',
                },
            },
        },
    });

    cardElement.mount(`#${elementId}`);
    
    return {
        stripe: stripe,
        elements: elements,
        cardElement: cardElement
    };
};

// Confirm payment with Stripe Elements
window.confirmStripePayment = async (stripe, clientSecret, cardElement) => {
    try {
        const { error, paymentIntent } = await stripe.confirmCardPayment(clientSecret, {
            payment_method: {
                card: cardElement,
            }
        });

        if (error) {
            console.error('Stripe payment error:', error);
            return { success: false, error: error.message };
        }

        return { success: true, paymentIntent: paymentIntent };
    } catch (error) {
        console.error('Stripe payment confirmation error:', error);
        return { success: false, error: error.message };
    }
};

// Load Stripe SDK dynamically
window.loadStripeSDK = () => {
    return new Promise((resolve, reject) => {
        if (window.Stripe) {
            resolve();
            return;
        }

        const script = document.createElement('script');
        script.src = 'https://js.stripe.com/v3/';
        script.onload = () => resolve();
        script.onerror = () => reject(new Error('Failed to load Stripe SDK'));
        document.head.appendChild(script);
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