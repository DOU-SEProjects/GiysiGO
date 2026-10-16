CREATE PROCEDURE [ordering].[spBeginPaymentAttempt]
    @OrderId uniqueidentifier, @QuoteId uniqueidentifier, @CustomerId uniqueidentifier,
    @Provider nvarchar(60), @IdempotencyKey nvarchar(200)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ExistingId uniqueidentifier;
    SELECT @ExistingId = id FROM [ordering].payment_attempts WITH (UPDLOCK, HOLDLOCK) WHERE provider = @Provider AND idempotency_key = @IdempotencyKey;
    IF @ExistingId IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [ordering].payment_attempts WHERE id = @ExistingId AND order_id = @OrderId AND price_quote_id = @QuoteId)
            THROW 51039, 'Payment idempotency key was already used for another order or quote.', 1;
        SELECT @ExistingId AS Id;
        RETURN;
    END;
    DECLARE @Amount decimal(12,2);
    SELECT @Amount = q.total_amount FROM [ordering].price_quotes q
    JOIN [ordering].orders o ON o.id = q.order_id
    WHERE q.id = @QuoteId AND q.order_id = @OrderId AND o.customer_id = @CustomerId
      AND q.status = 'CUSTOMER_ACCEPTED' AND o.status = 'AWAITING_PAYMENT';
    IF @Amount IS NULL THROW 51034, 'Only the accepted quote for an order can be paid.', 1;
    DECLARE @PaymentAttemptId uniqueidentifier = NEWID();
    INSERT [ordering].payment_attempts (id, order_id, price_quote_id, provider, idempotency_key, amount, currency, status)
    VALUES (@PaymentAttemptId, @OrderId, @QuoteId, @Provider, @IdempotencyKey, @Amount, 'TRY', 'INITIATED');
    SELECT @PaymentAttemptId AS Id;
END;
GO
