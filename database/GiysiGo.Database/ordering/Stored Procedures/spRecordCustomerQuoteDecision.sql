CREATE PROCEDURE [ordering].[spRecordCustomerQuoteDecision]
    @QuoteId uniqueidentifier, @CustomerId uniqueidentifier, @Accepted bit
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @OrderId uniqueidentifier;
    DECLARE @CurrentStatus nvarchar(40);
    SELECT @OrderId = order_id, @CurrentStatus = status FROM [ordering].price_quotes WITH (UPDLOCK, HOLDLOCK)
    WHERE id = @QuoteId AND delete_date IS NULL;
    IF @OrderId IS NULL THROW 51032, 'Published quote not found.', 1;
    IF NOT EXISTS (SELECT 1 FROM [ordering].orders WHERE id = @OrderId AND customer_id = @CustomerId)
        THROW 51033, 'Quote is not actionable by this customer.', 1;
    IF @CurrentStatus = CASE WHEN @Accepted = 1 THEN 'CUSTOMER_ACCEPTED' ELSE 'CUSTOMER_REJECTED' END
    BEGIN SELECT @OrderId AS OrderId, @QuoteId AS QuoteId, @Accepted AS Accepted, CAST(0 AS bit) AS Changed; RETURN; END;
    IF @CurrentStatus <> 'PUBLISHED' THROW 51038, 'Quote is no longer actionable.', 1;
    IF EXISTS (SELECT 1 FROM [ordering].price_quotes WHERE id = @QuoteId AND expires_at IS NOT NULL AND expires_at <= SYSUTCDATETIME())
        THROW 51060, 'Quote has expired.', 1;
    IF NOT EXISTS (SELECT 1 FROM [ordering].orders WHERE id = @OrderId AND customer_id = @CustomerId AND status = 'AWAITING_CUSTOMER_APPROVAL')
        THROW 51033, 'Quote is not actionable by this customer.', 1;
    UPDATE [ordering].price_quotes SET status = CASE WHEN @Accepted = 1 THEN 'CUSTOMER_ACCEPTED' ELSE 'CUSTOMER_REJECTED' END,
        customer_decision_by = @CustomerId, customer_decision_at = SYSUTCDATETIME(), update_date = SYSUTCDATETIME()
    WHERE id = @QuoteId AND status = 'PUBLISHED';
    UPDATE [ordering].orders SET status = CASE WHEN @Accepted = 1 THEN 'AWAITING_PAYMENT' ELSE 'RETURN_REQUESTED' END,
        version = version + 1, update_date = SYSUTCDATETIME() WHERE id = @OrderId;
    INSERT [ordering].order_status_history (id, order_id, from_status, to_status, actor_user_id, source)
    VALUES (NEWID(), @OrderId, 'AWAITING_CUSTOMER_APPROVAL', CASE WHEN @Accepted = 1 THEN 'AWAITING_PAYMENT' ELSE 'RETURN_REQUESTED' END, @CustomerId, 'CUSTOMER');
    SELECT @OrderId AS OrderId, @QuoteId AS QuoteId, @Accepted AS Accepted, CAST(1 AS bit) AS Changed;
END;
GO
