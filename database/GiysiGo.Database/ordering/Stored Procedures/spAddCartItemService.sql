CREATE PROCEDURE [ordering].[spAddCartItemService]
    @Id uniqueidentifier, @CartItemId uniqueidentifier, @StoreOfferingId uniqueidentifier, @CustomerId uniqueidentifier, @ExpectedCartVersion int
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CartId uniqueidentifier, @StoreId uniqueidentifier, @ProductTypeId uniqueidentifier;
    SELECT @CartId = c.id, @StoreId = c.store_id, @ProductTypeId = ci.product_type_id
    FROM [ordering].cart_items ci JOIN [ordering].carts c WITH (UPDLOCK, HOLDLOCK) ON c.id = ci.cart_id
    WHERE ci.id = @CartItemId AND ci.delete_date IS NULL AND c.customer_id = @CustomerId AND c.status = 'OPEN' AND c.version = @ExpectedCartVersion;
    IF @CartId IS NULL THROW 51040, 'Open cart item missing or version conflict.', 1;
    IF NOT EXISTS (
        SELECT 1 FROM [catalog].store_offerings so JOIN [catalog].product_type_services pts ON pts.id = so.product_type_service_id
        JOIN [catalog].services s ON s.id = pts.service_id
        WHERE so.id = @StoreOfferingId AND so.store_id = @StoreId AND so.is_active = 1 AND so.delete_date IS NULL
          AND pts.product_type_id = @ProductTypeId AND pts.is_active = 1 AND pts.delete_date IS NULL AND s.is_active = 1 AND s.delete_date IS NULL)
        THROW 51041, 'Offering is inactive or does not match item and store.', 1;
    IF @Id IS NULL OR @Id = '00000000-0000-0000-0000-000000000000' SET @Id = NEWID();
    INSERT [ordering].cart_item_services (id, cart_item_id, store_offering_id) VALUES (@Id, @CartItemId, @StoreOfferingId);
    UPDATE [ordering].carts SET version = version + 1, update_date = SYSUTCDATETIME() WHERE id = @CartId;
END;
GO
