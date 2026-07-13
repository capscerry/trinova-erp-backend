-- Migration: create sales_return_header / sales_return_detail
-- Records goods a customer sends back, referencing the Delivery Order they
-- were originally shipped on. Creating a return immediately puts the
-- returned qty back into inventory_stock (qty_on_hand / qty_available) at
-- the same warehouse it was shipped from.

IF OBJECT_ID('sales_return_header', 'U') IS NULL
BEGIN
    CREATE TABLE sales_return_header
    (
        id                 INT IDENTITY(1,1) PRIMARY KEY,
        return_number      VARCHAR(50)   NOT NULL,
        return_date        DATETIME      NOT NULL,
        customer_id        INT           NOT NULL,
        delivery_order_id  INT           NOT NULL,
        sales_order_id     INT           NULL,
        notes              VARCHAR(500)  NULL,
        status             VARCHAR(30)   NOT NULL DEFAULT 'Completed',
        created_at         DATETIME      NOT NULL DEFAULT GETDATE()
    );

    PRINT 'Table sales_return_header created.';
END
ELSE
BEGIN
    PRINT 'Table sales_return_header already exists, skipping.';
END

IF OBJECT_ID('sales_return_detail', 'U') IS NULL
BEGIN
    CREATE TABLE sales_return_detail
    (
        id            INT IDENTITY(1,1) PRIMARY KEY,
        return_id     INT             NOT NULL,
        product_id    INT             NOT NULL,
        warehouse_id  INT             NOT NULL,
        qty           DECIMAL(18,2)   NOT NULL,
        uom_id        INT             NULL,
        reason        VARCHAR(255)    NULL,
        CONSTRAINT FK_sales_return_detail_header
            FOREIGN KEY (return_id) REFERENCES sales_return_header(id)
    );

    PRINT 'Table sales_return_detail created.';
END
ELSE
BEGIN
    PRINT 'Table sales_return_detail already exists, skipping.';
END
