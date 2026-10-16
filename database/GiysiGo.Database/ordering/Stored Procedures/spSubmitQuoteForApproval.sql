CREATE PROCEDURE [ordering].[spSubmitQuoteForApproval]
    @QuoteId uniqueidentifier, @StoreId uniqueidentifier, @ActorUserId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @OrderId uniqueidentifier;
    SELECT @OrderId = q.order_id FROM [ordering].price_quotes q
    JOIN [ordering].orders o ON o.id = q.order_id
    WHERE q.id = @QuoteId AND q.status = 'DRAFT' AND o.store_id = @StoreId AND o.status = 'AWAITING_STORE_APPROVAL';
    IF @OrderId IS NULL THROW 51053, 'Draft quote is unavailable for submission in this store.', 1;
    IF NOT EXISTS (SELECT 1 FROM [ordering].price_quote_lines WHERE price_quote_id = @QuoteId AND delete_date IS NULL)
        THROW 51054, 'Quote must contain at least one line.', 1;
    IF EXISTS (SELECT 1 FROM [ordering].price_quotes q WHERE q.id = @QuoteId AND
        (q.subtotal <> (SELECT COALESCE(SUM(l.amount), 0) FROM [ordering].price_quote_lines l WHERE l.price_quote_id = q.id AND l.delete_date IS NULL)
         OR q.total_amount <> q.subtotal + q.pickup_fee + q.delivery_fee))
        THROW 51059, 'Quote totals do not match quote lines.', 1;
    UPDATE [ordering].price_quotes SET status = 'PENDING_STORE_APPROVAL', update_date = SYSUTCDATETIME()
    WHERE id = @QuoteId AND status = 'DRAFT';
    IF @@ROWCOUNT <> 1 THROW 51055, 'Quote changed concurrently.', 1;
    INSERT [ordering].quote_approval_history (id, price_quote_id, action, actor_user_id)
    VALUES (NEWID(), @QuoteId, 'SUBMITTED', @ActorUserId);
END;
GO
