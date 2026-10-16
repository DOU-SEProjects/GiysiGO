CREATE PROCEDURE [ordering].[spCompletePaymentAttempt]
    @PaymentAttemptId uniqueidentifier, @ProviderPaymentId nvarchar(200) = NULL, @Succeeded bit, @CompletedAtUtc datetime2(7)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @OrderId uniqueidentifier, @QuoteId uniqueidentifier, @OldStatus nvarchar(40);
    SELECT @OrderId = order_id, @QuoteId = price_quote_id, @OldStatus = status FROM [ordering].payment_attempts WITH (UPDLOCK, HOLDLOCK)
    WHERE id = @PaymentAttemptId;
    IF @OrderId IS NULL THROW 51035, 'Payment attempt not found.', 1;
    IF @OldStatus = CASE WHEN @Succeeded = 1 THEN 'SUCCEEDED' ELSE 'FAILED' END
    BEGIN
        IF @ProviderPaymentId IS NOT NULL AND EXISTS (SELECT 1 FROM [ordering].payment_attempts WHERE id = @PaymentAttemptId AND provider_payment_id IS NOT NULL AND provider_payment_id <> @ProviderPaymentId)
            THROW 51063, 'Provider payment identifier changed on retry.', 1;
        SELECT @OrderId AS OrderId, @QuoteId AS QuoteId, @Succeeded AS Succeeded, CAST(0 AS bit) AS Changed;
        RETURN;
    END;
    IF @OldStatus <> 'INITIATED' THROW 51036, 'Payment attempt is already terminal.', 1;
    UPDATE [ordering].payment_attempts SET status = CASE WHEN @Succeeded = 1 THEN 'SUCCEEDED' ELSE 'FAILED' END,
        provider_payment_id = @ProviderPaymentId, completed_at = @CompletedAtUtc, update_date = SYSUTCDATETIME()
    WHERE id = @PaymentAttemptId;
    IF @Succeeded = 1
    BEGIN
        UPDATE [ordering].orders SET status = 'PROCESSING', version = version + 1, update_date = SYSUTCDATETIME()
        WHERE id = @OrderId AND status = 'AWAITING_PAYMENT';
        IF @@ROWCOUNT <> 1 THROW 51037, 'Order is not awaiting payment.', 1;
        INSERT [ordering].order_status_history (id, order_id, from_status, to_status, source)
        VALUES (NEWID(), @OrderId, 'AWAITING_PAYMENT', 'PROCESSING', 'PAYMENT_PROVIDER');
    END;
    SELECT @OrderId AS OrderId, @QuoteId AS QuoteId, @Succeeded AS Succeeded, CAST(1 AS bit) AS Changed;
END;
GO
