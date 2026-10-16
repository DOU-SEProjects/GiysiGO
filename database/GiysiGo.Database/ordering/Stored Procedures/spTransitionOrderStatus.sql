CREATE PROCEDURE [ordering].[spTransitionOrderStatus]
    @OrderId uniqueidentifier, @StoreId uniqueidentifier, @ExpectedStatus nvarchar(40), @NewStatus nvarchar(40), @ExpectedVersion int,
    @ActorUserId uniqueidentifier = NULL, @Source nvarchar(40), @Note nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ActualStoreId uniqueidentifier, @ActualStatus nvarchar(40), @ActualVersion int;
    SELECT @ActualStoreId = store_id, @ActualStatus = status, @ActualVersion = version
    FROM [ordering].orders WITH (UPDLOCK, HOLDLOCK) WHERE id = @OrderId AND delete_date IS NULL;
    IF @ActualStoreId IS NULL OR @ActualStoreId <> @StoreId THROW 51021, 'Order not found in store scope.', 1;
    IF @ActualStatus = @NewStatus
    BEGIN SELECT @OrderId AS OrderId, @StoreId AS StoreId, @ActualStatus AS Status, CAST(0 AS bit) AS Changed; RETURN; END;
    IF @ActualStatus <> @ExpectedStatus OR @ActualVersion <> @ExpectedVersion
        THROW 51022, 'Order status or version conflict.', 1;
    IF NOT EXISTS (
        SELECT 1 FROM (VALUES
            ('AWAITING_PICKUP_ASSIGNMENT','PICKUP_ASSIGNED'), ('PICKUP_ASSIGNED','PICKED_UP'),
            ('PICKED_UP','AT_STORE'), ('AT_STORE','INSPECTING'), ('INSPECTING','AWAITING_STORE_APPROVAL'),
            ('PROCESSING','READY_FOR_DELIVERY'), ('READY_FOR_DELIVERY','DELIVERY_ASSIGNED'),
            ('DELIVERY_ASSIGNED','OUT_FOR_DELIVERY'), ('OUT_FOR_DELIVERY','DELIVERED'),
            ('RETURN_REQUESTED','RETURN_ASSIGNED'), ('RETURN_ASSIGNED','RETURN_IN_TRANSIT'), ('RETURN_IN_TRANSIT','RETURNED')
        ) allowed(from_status, to_status) WHERE from_status = @ExpectedStatus AND to_status = @NewStatus
    ) THROW 51020, 'Order status transition is not allowed.', 1;
    UPDATE [ordering].orders SET status = @NewStatus, version = version + 1, update_date = SYSUTCDATETIME()
    WHERE id = @OrderId AND store_id = @StoreId AND status = @ExpectedStatus AND version = @ExpectedVersion AND delete_date IS NULL;
    IF @@ROWCOUNT <> 1 THROW 51023, 'Order missing or version conflict.', 1;
    INSERT [ordering].order_status_history (id, order_id, from_status, to_status, actor_user_id, source, note)
    VALUES (NEWID(), @OrderId, @ExpectedStatus, @NewStatus, @ActorUserId, @Source, @Note);
    SELECT @OrderId AS OrderId, @StoreId AS StoreId, @NewStatus AS Status, CAST(1 AS bit) AS Changed;
END;
GO
