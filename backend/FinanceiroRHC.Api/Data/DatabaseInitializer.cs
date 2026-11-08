using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroRHC.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        await context.Database.EnsureCreatedAsync(cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS `ExpenseRecurrenceRules` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
                `Description` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
                `CategoryId` int NOT NULL,
                `StartDate` date NOT NULL,
                `Amount` decimal(12,2) NOT NULL,
                `RecurrenceType` int NOT NULL,
                `StartInstallment` int NOT NULL,
                `TotalInstallments` int NULL,
                `EndInstallment` int NULL,
                `IsActive` tinyint(1) NOT NULL,
                CONSTRAINT `PK_ExpenseRecurrenceRules` PRIMARY KEY (`Id`),
                CONSTRAINT `FK_ExpenseRecurrenceRules_AspNetUsers_UserId`
                    FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
                CONSTRAINT `FK_ExpenseRecurrenceRules_Categories_CategoryId`
                    FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`Id`) ON DELETE RESTRICT,
                INDEX `IX_ExpenseRecurrenceRules_CategoryId` (`CategoryId`),
                INDEX `IX_ExpenseRecurrenceRules_UserId_IsActive` (`UserId`, `IsActive`)
            ) CHARACTER SET=utf8mb4 ENGINE=InnoDB;
            """,
            cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS `CardPurchaseRecurrenceRules` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
                `CreditCardId` int NOT NULL,
                `CategoryId` int NOT NULL,
                `Description` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
                `StartDate` date NOT NULL,
                `Amount` decimal(12,2) NOT NULL,
                `RecurrenceType` int NOT NULL,
                `StartInstallment` int NOT NULL,
                `TotalInstallments` int NULL,
                `EndInstallment` int NULL,
                `IsActive` tinyint(1) NOT NULL,
                CONSTRAINT `PK_CardPurchaseRecurrenceRules` PRIMARY KEY (`Id`),
                CONSTRAINT `FK_CardPurchaseRecurrenceRules_AspNetUsers_UserId`
                    FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
                CONSTRAINT `FK_CardPurchaseRecurrenceRules_CreditCards_CreditCardId`
                    FOREIGN KEY (`CreditCardId`) REFERENCES `CreditCards` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_CardPurchaseRecurrenceRules_Categories_CategoryId`
                    FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`Id`) ON DELETE RESTRICT,
                INDEX `IX_CardPurchaseRecurrenceRules_CreditCardId` (`CreditCardId`),
                INDEX `IX_CardPurchaseRecurrenceRules_CategoryId` (`CategoryId`),
                INDEX `IX_CardPurchaseRecurrenceRules_UserId_IsActive` (`UserId`, `IsActive`)
            ) CHARACTER SET=utf8mb4 ENGINE=InnoDB;
            """,
            cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS `Investments` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
                `Description` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
                `ApplicationDate` date NOT NULL,
                `MaturityDate` date NOT NULL,
                `Amount` decimal(12,2) NOT NULL,
                CONSTRAINT `PK_Investments` PRIMARY KEY (`Id`),
                CONSTRAINT `FK_Investments_AspNetUsers_UserId`
                    FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
                INDEX `IX_Investments_UserId` (`UserId`)
            ) CHARACTER SET=utf8mb4 ENGINE=InnoDB;
            """,
            cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS `InvestmentWithdrawals` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
                `InvestmentId` int NOT NULL,
                `WithdrawalDate` date NOT NULL,
                `Amount` decimal(12,2) NOT NULL,
                `ReturnAmount` decimal(12,2) NOT NULL DEFAULT 0,
                CONSTRAINT `PK_InvestmentWithdrawals` PRIMARY KEY (`Id`),
                CONSTRAINT `FK_InvestmentWithdrawals_Investment`
                    FOREIGN KEY (`InvestmentId`) REFERENCES `Investments` (`Id`) ON DELETE CASCADE,
                CONSTRAINT `FK_InvestmentWithdrawals_AspNetUsers_UserId`
                    FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
                INDEX `IX_InvestmentWithdrawals_UserId_InvestmentId` (`UserId`, `InvestmentId`)
            ) CHARACTER SET=utf8mb4 ENGINE=InnoDB;
            """,
            cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS `CreditCardInvoicePayments` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
                `Year` int NOT NULL,
                `Month` int NOT NULL,
                `Amount` decimal(12,2) NOT NULL,
                `PaidAt` date NOT NULL,
                `ExpenseId` int NOT NULL,
                CONSTRAINT `PK_CreditCardInvoicePayments` PRIMARY KEY (`Id`),
                CONSTRAINT `FK_InvoicePayments_Expense`
                    FOREIGN KEY (`ExpenseId`) REFERENCES `Expenses` (`Id`) ON DELETE CASCADE,
                CONSTRAINT `FK_InvoicePayments_AspNetUsers_UserId`
                    FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
                UNIQUE INDEX `IX_CreditCardInvoicePayments_UserId_Year_Month` (`UserId`, `Year`, `Month`),
                UNIQUE INDEX `IX_CreditCardInvoicePayments_ExpenseId` (`ExpenseId`)
            ) CHARACTER SET=utf8mb4 ENGINE=InnoDB;
            """,
            cancellationToken);

        await AddColumnIfMissingAsync(context, "Expenses", "RecurrenceRuleId", "int NULL", cancellationToken);
        await AddColumnIfMissingAsync(context, "Expenses", "InstallmentNumber", "int NULL", cancellationToken);
        await AddColumnIfMissingAsync(context, "Expenses", "TotalInstallments", "int NULL", cancellationToken);
        await AddColumnIfMissingAsync(context, "Expenses", "IsDeleted", "tinyint(1) NOT NULL DEFAULT 0", cancellationToken);
        await AddColumnIfMissingAsync(context, "ExpenseRecurrenceRules", "EndInstallment", "int NULL", cancellationToken);
        await AddColumnIfMissingAsync(context, "CreditCardPurchases", "CategoryId", "int NULL", cancellationToken);
        await AddColumnIfMissingAsync(context, "CreditCardPurchases", "RecurrenceRuleId", "int NULL", cancellationToken);
        await AddColumnIfMissingAsync(context, "CreditCardPurchases", "InstallmentNumber", "int NULL", cancellationToken);
        await AddColumnIfMissingAsync(context, "CreditCardPurchases", "TotalInstallments", "int NULL", cancellationToken);
        await AddColumnIfMissingAsync(context, "CreditCardPurchases", "IsDeleted", "tinyint(1) NOT NULL DEFAULT 0", cancellationToken);
        await MakeColumnNullableIfRequiredAsync(context, "CreditCardPurchases", "TotalAmount", cancellationToken);
        await AddColumnIfMissingAsync(context, "Incomes", "Type", "int NOT NULL DEFAULT 0", cancellationToken);
        await AddColumnIfMissingAsync(context, "Incomes", "InvestmentWithdrawalId", "int NULL", cancellationToken);
        await AddColumnIfMissingAsync(context, "Expenses", "InvestmentId", "int NULL", cancellationToken);
        await AddColumnIfMissingAsync(context, "InvestmentWithdrawals", "ReturnAmount", "decimal(12,2) NOT NULL DEFAULT 0", cancellationToken);
        await AddColumnIfMissingAsync(context, "Expenses", "InvestmentWithdrawalId", "int NULL", cancellationToken);
        await AddIndexIfMissingAsync(
            context,
            "Expenses",
            "IX_Expenses_RecurrenceRuleId_InstallmentNumber",
            "`RecurrenceRuleId`, `InstallmentNumber`",
            unique: true,
            cancellationToken);
        await AddForeignKeyIfMissingAsync(
            context,
            "Expenses",
            "FK_Expenses_ExpenseRecurrenceRules_RecurrenceRuleId",
            "ALTER TABLE `Expenses` ADD CONSTRAINT `FK_Expenses_ExpenseRecurrenceRules_RecurrenceRuleId` FOREIGN KEY (`RecurrenceRuleId`) REFERENCES `ExpenseRecurrenceRules` (`Id`) ON DELETE CASCADE;",
            cancellationToken);
        await AddIndexIfMissingAsync(
            context,
            "ExpenseRecurrenceRules",
            "IX_ExpenseRecurrenceRules_UserId_IsActive",
            "`UserId`, `IsActive`",
            unique: false,
            cancellationToken);
        await AddForeignKeyIfMissingAsync(
            context,
            "ExpenseRecurrenceRules",
            "FK_ExpenseRecurrenceRules_AspNetUsers_UserId",
            "ALTER TABLE `ExpenseRecurrenceRules` ADD CONSTRAINT `FK_ExpenseRecurrenceRules_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE;",
            cancellationToken);
        await AddForeignKeyIfMissingAsync(
            context,
            "ExpenseRecurrenceRules",
            "FK_ExpenseRecurrenceRules_Categories_CategoryId",
            "ALTER TABLE `ExpenseRecurrenceRules` ADD CONSTRAINT `FK_ExpenseRecurrenceRules_Categories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`Id`) ON DELETE RESTRICT;",
            cancellationToken);
        await AddIndexIfMissingAsync(
            context,
            "CreditCardPurchases",
            "IX_CreditCardPurchases_RecurrenceRuleId_InstallmentNumber",
            "`RecurrenceRuleId`, `InstallmentNumber`",
            unique: true,
            cancellationToken);
        await AddForeignKeyIfMissingAsync(
            context,
            "CreditCardPurchases",
            "FK_CardPurchases_RecurrenceRule",
            "ALTER TABLE `CreditCardPurchases` ADD CONSTRAINT `FK_CardPurchases_RecurrenceRule` FOREIGN KEY (`RecurrenceRuleId`) REFERENCES `CardPurchaseRecurrenceRules` (`Id`) ON DELETE CASCADE;",
            cancellationToken);
        await AddForeignKeyIfMissingAsync(
            context,
            "CreditCardPurchases",
            "FK_CreditCardPurchases_Categories_CategoryId",
            "ALTER TABLE `CreditCardPurchases` ADD CONSTRAINT `FK_CreditCardPurchases_Categories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`Id`) ON DELETE RESTRICT;",
            cancellationToken);
        await AddForeignKeyIfMissingAsync(
            context,
            "Incomes",
            "FK_Incomes_InvestmentWithdrawal",
            "ALTER TABLE `Incomes` ADD CONSTRAINT `FK_Incomes_InvestmentWithdrawal` FOREIGN KEY (`InvestmentWithdrawalId`) REFERENCES `InvestmentWithdrawals` (`Id`) ON DELETE CASCADE;",
            cancellationToken);
        await AddIndexIfMissingAsync(
            context,
            "Incomes",
            "IX_Incomes_InvestmentWithdrawalId",
            "`InvestmentWithdrawalId`",
            unique: true,
            cancellationToken);
        await AddForeignKeyIfMissingAsync(
            context,
            "Expenses",
            "FK_Expenses_Investment",
            "ALTER TABLE `Expenses` ADD CONSTRAINT `FK_Expenses_Investment` FOREIGN KEY (`InvestmentId`) REFERENCES `Investments` (`Id`) ON DELETE CASCADE;",
            cancellationToken);
        await AddIndexIfMissingAsync(
            context,
            "Expenses",
            "IX_Expenses_InvestmentWithdrawalId",
            "`InvestmentWithdrawalId`",
            unique: true,
            cancellationToken);
        await AddForeignKeyIfMissingAsync(
            context,
            "Expenses",
            "FK_Expenses_InvestmentWithdrawal",
            "ALTER TABLE `Expenses` ADD CONSTRAINT `FK_Expenses_InvestmentWithdrawal` FOREIGN KEY (`InvestmentWithdrawalId`) REFERENCES `InvestmentWithdrawals` (`Id`) ON DELETE CASCADE;",
            cancellationToken);
    }

    private static async Task AddColumnIfMissingAsync(
        ApplicationDbContext context,
        string table,
        string column,
        string definition,
        CancellationToken cancellationToken)
    {
        if (await CountAsync(
                context,
                "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table AND COLUMN_NAME = @column;",
                table,
                column,
                cancellationToken) == 0)
        {
            await ExecuteDdlAsync(context, $"ALTER TABLE `{table}` ADD COLUMN `{column}` {definition};", cancellationToken);
        }
    }

    private static async Task MakeColumnNullableIfRequiredAsync(
        ApplicationDbContext context,
        string table,
        string column,
        CancellationToken cancellationToken)
    {
        if (await CountAsync(
                context,
                "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table AND COLUMN_NAME = @column AND IS_NULLABLE = 'NO';",
                table,
                column,
                cancellationToken) > 0)
        {
            await ExecuteDdlAsync(context, $"ALTER TABLE `{table}` MODIFY COLUMN `{column}` decimal(12,2) NULL;", cancellationToken);
        }
    }

    private static async Task AddIndexIfMissingAsync(
        ApplicationDbContext context,
        string table,
        string index,
        string columns,
        bool unique,
        CancellationToken cancellationToken)
    {
        if (await CountAsync(
                context,
                "SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table AND INDEX_NAME = @column;",
                table,
                index,
                cancellationToken) == 0)
        {
            await ExecuteDdlAsync(
                context,
                $"CREATE {(unique ? "UNIQUE " : string.Empty)}INDEX `{index}` ON `{table}` ({columns});",
                cancellationToken);
        }
    }

    private static async Task AddForeignKeyIfMissingAsync(
        ApplicationDbContext context,
        string table,
        string constraint,
        string sql,
        CancellationToken cancellationToken)
    {
        if (await CountAsync(
                context,
                "SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = @table AND CONSTRAINT_NAME = @column AND CONSTRAINT_TYPE = 'FOREIGN KEY';",
                table,
                constraint,
                cancellationToken) == 0)
        {
            await ExecuteDdlAsync(context, sql, cancellationToken);
        }
    }

    private static async Task<int> CountAsync(
        ApplicationDbContext context,
        string sql,
        string table,
        string column,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@table", table);
        AddParameter(command, "@column", column);
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value);
    }

    private static void AddParameter(DbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static async Task ExecuteDdlAsync(
        ApplicationDbContext context,
        string sql,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
