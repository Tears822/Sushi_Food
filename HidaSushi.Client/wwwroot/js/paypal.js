// PayPal integration functions
window.initializePayPalButtons = (container, amount, orderId, componentRef) => {
    if (!window.paypal) {
        console.error('PayPal SDK not loaded');
        return;
    }

    paypal.Buttons({
        style: {
            layout: 'vertical',
            color: 'gold',
            shape: 'rect',
            label: 'pay'
        },
        
        createOrder: async (data, actions) => {
            try {
                // Call the server to create PayPal order
                const order = await componentRef.invokeMethodAsync('CreatePayPalOrder');
                return order.id;
            } catch (error) {
                console.error('Error creating PayPal order:', error);
                throw error;
            }
        },
        
        onApprove: async (data, actions) => {
            try {
                // Call the server to capture payment
                const captureResult = await componentRef.invokeMethodAsync('CapturePayPalPayment', data.orderID);
                
                if (captureResult && captureResult.status === 'COMPLETED') {
                    // Payment successful - notify the component
                    await componentRef.invokeMethodAsync('OnPayPalPaymentSuccess', captureResult);
                } else {
                    throw new Error('Payment capture failed');
                }
            } catch (error) {
                console.error('Error capturing PayPal payment:', error);
                await componentRef.invokeMethodAsync('OnPayPalPaymentError', error.message);
            }
        },
        
        onError: async (err) => {
            console.error('PayPal error:', err);
            await componentRef.invokeMethodAsync('OnPayPalPaymentError', err.toString());
        },
        
        onCancel: async (data) => {
            console.log('PayPal payment cancelled:', data);
            await componentRef.invokeMethodAsync('OnPayPalPaymentCancel');
        }
    }).render(container);
};

// Utility function to load PayPal SDK dynamically
window.loadPayPalSDK = (clientId) => {
    return new Promise((resolve, reject) => {
        if (window.paypal) {
            resolve();
            return;
        }

        const script = document.createElement('script');
        script.src = `https://www.paypal.com/sdk/js?client-id=${clientId}&currency=EUR`;
        script.onload = () => resolve();
        script.onerror = () => reject(new Error('Failed to load PayPal SDK'));
        document.head.appendChild(script);
    });
}; 