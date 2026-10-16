CREATE PROCEDURE [ordering].[spAddCartItem]
    @Id uniqueidentifier, @CartId uniqueidentifier, @CustomerId uniqueidentifier, @ProductTypeId uniqueidentifier,
    @CustomerNote nvarchar(max) = NULL, @CustomerMeasurement decimal(12,3) = NULL, @ExpectedCartVersion int
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM [ordering].carts WITH (UPDLOCK, HOLDLOCK) WHERE id = @CartId AND customer_id = @CustomerId AND status = 'OPEN' AND version = @ExpectedCartVersion AND delete_date IS NULL)
        THROW 51011, 'Open cart missing or version conflict.', 1;
    IF NOT EXISTS (SELECT 1 FROM [catalog].product_types WHERE id = @ProductTypeId AND is_active = 1 AND delete_date IS NULL)
        THROW 51012, 'Product type is unavailable.', 1;
    IF @Id IS NULL OR @Id = '00000000-0000-0000-0000-000000000000' SET @Id = NEWID();
    INSERT [ordering].cart_items (id, cart_id, product_type_id, customer_note, customer_measurement)
    VALUES (@Id, @CartId, @ProductTypeId, @CustomerNote, @CustomerMeasurement);
    UPDATE [ordering].carts SET version = version + 1, update_date = SYSUTCDATETIME() WHERE id = @CartId AND version = @ExpectedCartVersion;
END;
GO
