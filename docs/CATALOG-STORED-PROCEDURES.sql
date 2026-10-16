CREATE OR ALTER PROCEDURE catalog.usp_GetCategories
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        id AS Id,
        name AS Name,
        description AS Description,
        display_order AS DisplayOrder,
        is_active AS IsActive,
        create_date AS CreateDate,
        update_date AS UpdateDate,
        delete_date AS DeleteDate
    FROM catalog.product_categories
    WHERE (@IncludeInactive = 1 OR (is_active = 1 AND delete_date IS NULL))
    ORDER BY display_order ASC, name ASC;
END;
GO

CREATE OR ALTER PROCEDURE catalog.usp_GetCategoryById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        id AS Id,
        name AS Name,
        description AS Description,
        display_order AS DisplayOrder,
        is_active AS IsActive,
        create_date AS CreateDate,
        update_date AS UpdateDate,
        delete_date AS DeleteDate
    FROM catalog.product_categories
    WHERE id = @Id AND delete_date IS NULL;
END;
GO

CREATE OR ALTER PROCEDURE catalog.usp_GetProductTypes
    @CategoryId UNIQUEIDENTIFIER = NULL,
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        id AS Id,
        category_id AS CategoryId,
        name AS Name,
        description AS Description,
        icon_url AS IconUrl,
        display_order AS DisplayOrder,
        is_active AS IsActive,
        create_date AS CreateDate,
        update_date AS UpdateDate,
        delete_date AS DeleteDate
    FROM catalog.product_types
    WHERE (@CategoryId IS NULL OR category_id = @CategoryId)
      AND (@IncludeInactive = 1 OR (is_active = 1 AND delete_date IS NULL))
    ORDER BY display_order ASC, name ASC;
END;
GO

CREATE OR ALTER PROCEDURE catalog.usp_GetProductTypeById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        id AS Id,
        category_id AS CategoryId,
        name AS Name,
        description AS Description,
        icon_url AS IconUrl,
        display_order AS DisplayOrder,
        is_active AS IsActive,
        create_date AS CreateDate,
        update_date AS UpdateDate,
        delete_date AS DeleteDate
    FROM catalog.product_types
    WHERE id = @Id AND delete_date IS NULL;
END;
GO

CREATE OR ALTER PROCEDURE catalog.usp_GetServices
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        id AS Id,
        name AS Name,
        code AS Code,
        description AS Description,
        is_active AS IsActive,
        create_date AS CreateDate,
        update_date AS UpdateDate,
        delete_date AS DeleteDate
    FROM catalog.services
    WHERE (@IncludeInactive = 1 OR (is_active = 1 AND delete_date IS NULL))
    ORDER BY name ASC;
END;
GO

CREATE OR ALTER PROCEDURE catalog.usp_GetServiceById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        id AS Id,
        name AS Name,
        code AS Code,
        description AS Description,
        is_active AS IsActive,
        create_date AS CreateDate,
        update_date AS UpdateDate,
        delete_date AS DeleteDate
    FROM catalog.services
    WHERE id = @Id AND delete_date IS NULL;
END;
GO

CREATE OR ALTER PROCEDURE catalog.usp_GetStores
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        id AS Id,
        name AS Name,
        code AS Code,
        address AS Address,
        phone_number AS PhoneNumber,
        is_active AS IsActive,
        create_date AS CreateDate,
        update_date AS UpdateDate,
        delete_date AS DeleteDate
    FROM catalog.stores
    WHERE (@IncludeInactive = 1 OR (is_active = 1 AND delete_date IS NULL))
    ORDER BY name ASC;
END;
GO

CREATE OR ALTER PROCEDURE catalog.usp_GetStoreById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        id AS Id,
        name AS Name,
        code AS Code,
        address AS Address,
        phone_number AS PhoneNumber,
        is_active AS IsActive,
        create_date AS CreateDate,
        update_date AS UpdateDate,
        delete_date AS DeleteDate
    FROM catalog.stores
    WHERE id = @Id AND delete_date IS NULL;
END;
GO

CREATE OR ALTER PROCEDURE catalog.usp_GetStoreOfferings
    @StoreId UNIQUEIDENTIFIER = NULL,
    @ProductTypeServiceId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        id AS Id,
        store_id AS StoreId,
        product_type_service_id AS ProductTypeServiceId,
        price AS Price,
        currency AS Currency,
        lead_time_hours AS LeadTimeHours,
        is_active AS IsActive,
        create_date AS CreateDate,
        update_date AS UpdateDate,
        delete_date AS DeleteDate
    FROM catalog.store_offerings
    WHERE (@StoreId IS NULL OR store_id = @StoreId)
      AND (@ProductTypeServiceId IS NULL OR product_type_service_id = @ProductTypeServiceId)
      AND (is_active = 1 AND delete_date IS NULL)
    ORDER BY price ASC;
END;
GO

CREATE OR ALTER PROCEDURE catalog.usp_GetStoreOfferingById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        id AS Id,
        store_id AS StoreId,
        product_type_service_id AS ProductTypeServiceId,
        price AS Price,
        currency AS Currency,
        lead_time_hours AS LeadTimeHours,
        is_active AS IsActive,
        create_date AS CreateDate,
        update_date AS UpdateDate,
        delete_date AS DeleteDate
    FROM catalog.store_offerings
    WHERE id = @Id AND delete_date IS NULL;
END;
GO
-- 11. Sayfalamalı Ürün Tipleri Listesi
CREATE OR ALTER PROCEDURE catalog.usp_GetProductTypesPaged
    @CategoryId UNIQUEIDENTIFIER = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        id AS Id,
        category_id AS CategoryId,
        name AS Name,
        description AS Description,
        icon_url AS IconUrl,
        display_order AS DisplayOrder,
        is_active AS IsActive,
        create_date AS CreateDate,
        update_date AS UpdateDate,
        delete_date AS DeleteDate
    FROM catalog.product_types
    WHERE (@CategoryId IS NULL OR category_id = @CategoryId)
      AND is_active = 1 
      AND delete_date IS NULL
    ORDER BY display_order ASC, name ASC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

-- 12. Kategori Ekleme (Insert GUID Result-Set Kuralına Uygun)
CREATE OR ALTER PROCEDURE catalog.usp_InsertCategory
    @Name NVARCHAR(100),
    @Description NVARCHAR(500) = NULL,
    @DisplayOrder INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NewId UNIQUEIDENTIFIER = NEWID();
    DECLARE @NowUtc DATETIME2 = SYSUTCDATETIME();

    INSERT INTO catalog.product_categories (
        id, name, description, display_order, is_active, create_date
    )
    VALUES (
        @NewId, @Name, @Description, @DisplayOrder, 1, @NowUtc
    );

    -- insert GUID result-set kuralı:
    SELECT @NewId AS Id;
END;
GO