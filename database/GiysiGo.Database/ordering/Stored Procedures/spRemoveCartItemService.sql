CREATE PROCEDURE [ordering].[spRemoveCartItemService]
    @CartId uniqueidentifier, @CartItemId uniqueidentifier, @ServiceId uniqueidentifier, @CustomerId uniqueidentifier, @ExpectedCartVersion int
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE cis SET delete_date = SYSUTCDATETIME(), update_date = SYSUTCDATETIME()
    FROM [ordering].cart_item_services cis JOIN [ordering].cart_items ci ON ci.id = cis.cart_item_id
    JOIN [ordering].carts c WITH (UPDLOCK, HOLDLOCK) ON c.id = ci.cart_id
    WHERE cis.id = @ServiceId AND ci.id = @CartItemId AND c.id = @CartId AND c.customer_id = @CustomerId AND c.status = 'OPEN'
      AND c.version = @ExpectedCartVersion AND cis.delete_date IS NULL;
    IF @@ROWCOUNT <> 1 THROW 51042, 'Cart service missing or version conflict.', 1;
    UPDATE [ordering].carts SET version = version + 1, update_date = SYSUTCDATETIME() WHERE id = @CartId;
END;
GO
