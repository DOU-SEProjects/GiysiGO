CREATE PROCEDURE [ordering].[spRemoveCartItem]
    @CartId uniqueidentifier, @ItemId uniqueidentifier, @CustomerId uniqueidentifier, @ExpectedVersion int
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ci SET delete_date = SYSUTCDATETIME(), update_date = SYSUTCDATETIME()
    FROM [ordering].cart_items ci JOIN [ordering].carts c WITH (UPDLOCK, HOLDLOCK) ON c.id = ci.cart_id
    WHERE ci.id = @ItemId AND ci.cart_id = @CartId AND c.customer_id = @CustomerId AND ci.delete_date IS NULL AND c.status = 'OPEN' AND c.version = @ExpectedVersion;
    IF @@ROWCOUNT <> 1 THROW 51013, 'Cart item missing, cart closed, or version conflict.', 1;
    UPDATE [ordering].carts SET version = version + 1, update_date = SYSUTCDATETIME() WHERE id = @CartId;
END;
GO
