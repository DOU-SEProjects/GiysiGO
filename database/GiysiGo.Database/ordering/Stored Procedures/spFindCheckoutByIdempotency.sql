CREATE PROCEDURE [ordering].[spFindCheckoutByIdempotency]
    @CartId uniqueidentifier, @CustomerId uniqueidentifier, @IdempotencyKey nvarchar(200)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ExistingCartId uniqueidentifier;
    SELECT @ExistingCartId = source_cart_id FROM [ordering].orders WITH (UPDLOCK, HOLDLOCK)
    WHERE customer_id = @CustomerId AND idempotency_key = @IdempotencyKey;
    IF @ExistingCartId IS NULL RETURN;
    IF @ExistingCartId <> @CartId THROW 51009, 'Idempotency key was already used for another cart.', 1;
    SELECT id AS OrderId, source_cart_id AS SourceCartId, store_id AS StoreId,
           pickup_window_start AS PickupWindowStartUtc, pickup_window_end AS PickupWindowEndUtc,
           CAST(0 AS bit) AS Created
    FROM [ordering].orders WHERE customer_id = @CustomerId AND idempotency_key = @IdempotencyKey;
END;
GO
