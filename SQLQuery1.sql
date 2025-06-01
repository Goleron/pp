CREATE DATABASE ZADANIE

USE ZADANIE

go
CREATE TABLE Orders (
    OrderNumber INT PRIMARY KEY,
    OrderDate DATETIME NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    PaidAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00
);

CREATE TABLE MoneyReceipts (
    ReceiptNumber INT PRIMARY KEY,
    ReceiptDate DATETIME NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Balance DECIMAL(18,2) NOT NULL DEFAULT 0.00
);

CREATE TABLE Payments (
    PaymentID INT PRIMARY KEY IDENTITY(1,1),
    OrderNumber INT NOT NULL,
    ReceiptNumber INT NOT NULL,
    PaymentAmount DECIMAL(18,2) NOT NULL,
    FOREIGN KEY (OrderNumber) REFERENCES Orders(OrderNumber),
    FOREIGN KEY (ReceiptNumber) REFERENCES MoneyReceipts(ReceiptNumber)
);


USE ZADANIE
GO

CREATE TRIGGER TR_Payments_Insert
ON Payments
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PaymentAmount DECIMAL(18,2);
    DECLARE @OrderNumber INT;
    DECLARE @ReceiptNumber INT;
    DECLARE @TotalAmount DECIMAL(18,2);
    DECLARE @PaidAmount DECIMAL(18,2);
    DECLARE @Balance DECIMAL(18,2);

    SELECT 
        @PaymentAmount = PaymentAmount,
        @OrderNumber = OrderNumber,
        @ReceiptNumber = ReceiptNumber
    FROM inserted;

    IF @PaymentAmount <= 0
    BEGIN
        RAISERROR ('Сумма платежа должна быть больше 0.', 16, 1);
        ROLLBACK;
        RETURN;
    END;

    BEGIN TRANSACTION;

    SELECT 
        @TotalAmount = TotalAmount,
        @PaidAmount = PaidAmount
    FROM Orders WITH (UPDLOCK)
    WHERE OrderNumber = @OrderNumber;

    IF @TotalAmount IS NULL
    BEGIN
        RAISERROR ('Указанный заказ не существует.', 16, 1);
        ROLLBACK;
        RETURN;
    END;

    IF (@PaidAmount + @PaymentAmount) > @TotalAmount
    BEGIN
        RAISERROR ('Сумма платежа превышает сумму заказа.', 16, 1);
        ROLLBACK;
        RETURN;
    END;

    SELECT 
        @Balance = Balance
    FROM MoneyReceipts WITH (UPDLOCK)
    WHERE ReceiptNumber = @ReceiptNumber;

    IF @Balance IS NULL
    BEGIN
        RAISERROR ('Указанное поступление не существует.', 16, 1);
        ROLLBACK;
        RETURN;
    END;

    IF (@Balance - @PaymentAmount) < 0
    BEGIN
        RAISERROR ('Недостаточно средств на остатке поступления.', 16, 1);
        ROLLBACK;
        RETURN;
    END;

    UPDATE Orders
    SET PaidAmount = PaidAmount + @PaymentAmount
    WHERE OrderNumber = @OrderNumber;

    UPDATE MoneyReceipts
    SET Balance = Balance - @PaymentAmount
    WHERE ReceiptNumber = @ReceiptNumber;

    COMMIT TRANSACTION;
END;
GO


CREATE TRIGGER TR_MoneyReceipts_Insert
ON MoneyReceipts
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE MoneyReceipts
    SET Balance = inserted.Amount
    FROM inserted
    WHERE MoneyReceipts.ReceiptNumber = inserted.ReceiptNumber;
END;
GO


INSERT INTO Orders (OrderNumber, OrderDate, TotalAmount, PaidAmount)
VALUES 
    (1, '2025-05-01', 1000.00, 0.00),
    (2, '2025-05-02', 2000.00, 0.00);

INSERT INTO MoneyReceipts (ReceiptNumber, ReceiptDate, Amount)
VALUES 
    (1, '2025-05-01', 1500.00),
    (2, '2025-05-02', 2500.00);

INSERT INTO Payments (OrderNumber, ReceiptNumber, PaymentAmount)
VALUES (1, 1, 500.00);


SELECT*FROM Orders
