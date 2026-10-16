CREATE PROCEDURE [ordering].[spGetOpenCartStoreId]
    @CartId uniqueidentifier, @CustomerId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SELECT store_id FROM [ordering].carts
    WHERE id = @CartId AND customer_id = @CustomerId AND status = 'OPEN' AND delete_date IS NULL;
END;
GO
