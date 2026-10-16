CREATE PROCEDURE [ordering].[spAddCartItemPhoto]
    @Id uniqueidentifier, @CartItemId uniqueidentifier, @StorageKey nvarchar(1024), @PhotoType nvarchar(40), @CustomerId uniqueidentifier, @ExpectedCartVersion int
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CartId uniqueidentifier;
    SELECT @CartId = c.id FROM [ordering].cart_items ci JOIN [ordering].carts c WITH (UPDLOCK, HOLDLOCK) ON c.id = ci.cart_id
    WHERE ci.id = @CartItemId AND ci.delete_date IS NULL AND c.customer_id = @CustomerId AND c.status = 'OPEN' AND c.version = @ExpectedCartVersion;
    IF @CartId IS NULL THROW 51043, 'Open cart item missing or version conflict.', 1;
    IF @Id IS NULL OR @Id = '00000000-0000-0000-0000-000000000000' SET @Id = NEWID();
    INSERT [ordering].cart_item_photos (id, cart_item_id, storage_key, photo_type) VALUES (@Id, @CartItemId, @StorageKey, @PhotoType);
    UPDATE [ordering].carts SET version = version + 1, update_date = SYSUTCDATETIME() WHERE id = @CartId;
END;
GO
