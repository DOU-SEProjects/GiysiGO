CREATE PROCEDURE [ordering].[spGetCheckoutOfferingSelections]
    @CartId uniqueidentifier, @CustomerId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT ci.product_type_id AS ProductTypeId, pts.service_id AS ServiceId
    FROM [ordering].carts c
    JOIN [ordering].cart_items ci ON ci.cart_id = c.id AND ci.delete_date IS NULL
    JOIN [ordering].cart_item_services cis ON cis.cart_item_id = ci.id AND cis.delete_date IS NULL
    JOIN [catalog].store_offerings so ON so.id = cis.store_offering_id
    JOIN [catalog].product_type_services pts ON pts.id = so.product_type_service_id
    WHERE c.id = @CartId AND c.customer_id = @CustomerId AND c.status = 'OPEN' AND c.delete_date IS NULL;
END;
GO
