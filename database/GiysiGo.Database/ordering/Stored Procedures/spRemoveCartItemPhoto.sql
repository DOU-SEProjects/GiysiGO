CREATE PROCEDURE [ordering].[spRemoveCartItemPhoto]
    @CartId uniqueidentifier, @CartItemId uniqueidentifier, @PhotoId uniqueidentifier, @CustomerId uniqueidentifier, @ExpectedCartVersion int
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE cp SET delete_date = SYSUTCDATETIME(), update_date = SYSUTCDATETIME()
    FROM [ordering].cart_item_photos cp JOIN [ordering].cart_items ci ON ci.id = cp.cart_item_id
    JOIN [ordering].carts c WITH (UPDLOCK, HOLDLOCK) ON c.id = ci.cart_id
    WHERE cp.id = @PhotoId AND cp.cart_item_id = @CartItemId AND c.id = @CartId AND c.customer_id = @CustomerId AND c.status = 'OPEN'
      AND c.version = @ExpectedCartVersion AND cp.delete_date IS NULL;
    IF @@ROWCOUNT <> 1 THROW 51044, 'Cart photo missing or version conflict.', 1;
    UPDATE [ordering].carts SET version = version + 1, update_date = SYSUTCDATETIME() WHERE id = @CartId;
END;
GO
