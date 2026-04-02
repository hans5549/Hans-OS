using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdditionalCategories : Migration
    {
        // ── 新增支出主分類 ──────────────────────
        private const string CatExpSubscription = "c0000001-0009-0000-0000-000000000001";
        private const string CatExpPersonal    = "c0000001-0010-0000-0000-000000000001";
        private const string CatExpDonation    = "c0000001-0011-0000-0000-000000000001";

        // ── 新增支出子分類：飲食（補齊）──────────────
        private const string SubDessert      = "c0000001-0001-0006-0000-000000000001";
        private const string SubCoffee       = "c0000001-0001-0007-0000-000000000001";
        private const string SubGrocery      = "c0000001-0001-0008-0000-000000000001";

        // ── 新增支出子分類：交通（補齊）──────────────
        private const string SubHsr          = "c0000001-0002-0006-0000-000000000001";
        private const string SubTrain        = "c0000001-0002-0007-0000-000000000001";
        private const string SubMotorcycle   = "c0000001-0002-0008-0000-000000000001";

        // ── 新增支出子分類：購物（補齊）──────────────
        private const string SubEbook        = "c0000001-0004-0004-0000-000000000001";

        // ── 新增支出子分類：家居（原居住，補齊）─────────
        private const string SubMortgage     = "c0000001-0005-0004-0000-000000000001";
        private const string SubPhone        = "c0000001-0005-0005-0000-000000000001";
        private const string SubMgmtFee      = "c0000001-0005-0006-0000-000000000001";
        private const string SubFurniture    = "c0000001-0005-0007-0000-000000000001";
        private const string SubElectricity  = "c0000001-0005-0008-0000-000000000001";
        private const string SubWater        = "c0000001-0005-0009-0000-000000000001";
        private const string SubGasUtil      = "c0000001-0005-0010-0000-000000000001";

        // ── 新增支出子分類：娛樂（補齊）──────────────
        private const string SubMobileGame   = "c0000001-0003-0004-0000-000000000001";

        // ── 新增支出子分類：學習（原教育，補齊）─────────
        private const string SubOnlineCourse = "c0000001-0007-0003-0000-000000000001";

        // ── 新增支出子分類：訂閱 ────────────────────
        private const string SubAiTool       = "c0000001-0009-0001-0000-000000000001";
        private const string SubMusicStream  = "c0000001-0009-0002-0000-000000000001";
        private const string SubCloudStorage = "c0000001-0009-0003-0000-000000000001";
        private const string SubBookkeeping  = "c0000001-0009-0004-0000-000000000001";

        // ── 新增支出子分類：個人 ────────────────────
        private const string SubInsurance    = "c0000001-0010-0001-0000-000000000001";
        private const string SubMobilePhone  = "c0000001-0010-0002-0000-000000000001";
        private const string SubHaircut      = "c0000001-0010-0003-0000-000000000001";

        // ── 新增支出子分類：奉獻 ────────────────────
        private const string SubTithe        = "c0000001-0011-0001-0000-000000000001";
        private const string SubDesignated   = "c0000001-0011-0002-0000-000000000001";

        // ── 新增收入子分類（補齊）────────────────────
        private const string SubStockSale    = "c0000002-0003-0001-0000-000000000001";
        private const string SubBankInterest = "c0000002-0003-0002-0000-000000000001";
        private const string SubCashback     = "c0000002-0003-0003-0000-000000000001";
        private const string SubTransferInc  = "c0000002-0005-0001-0000-000000000001";
        private const string SubRefund       = "c0000002-0005-0002-0000-000000000001";

        // ── 既有分類 ID 參照（用於重命名和新增子分類）─────
        private const string CatExpFood     = "c0000001-0001-0000-0000-000000000001";
        private const string CatExpTransit  = "c0000001-0002-0000-0000-000000000001";
        private const string CatExpFun      = "c0000001-0003-0000-0000-000000000001";
        private const string CatExpShop     = "c0000001-0004-0000-0000-000000000001";
        private const string CatExpHome     = "c0000001-0005-0000-0000-000000000001";
        private const string CatExpEdu      = "c0000001-0007-0000-0000-000000000001";
        private const string CatIncInvest   = "c0000002-0003-0000-0000-000000000001";
        private const string CatIncOther    = "c0000002-0005-0000-0000-000000000001";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 重命名既有分類（與 MOZE 一致）──────────
            migrationBuilder.Sql($"""
                UPDATE "TransactionCategories" SET "Name" = '家居' WHERE "Id" = '{CatExpHome}';
            """);
            migrationBuilder.Sql($"""
                UPDATE "TransactionCategories" SET "Name" = '學習' WHERE "Id" = '{CatExpEdu}';
            """);

            // ── 拆分「水電瓦斯」為獨立子分類 ──────────────
            // 先刪除舊的「水電瓦斯」合併子分類
            var subUtility = "c0000001-0005-0002-0000-000000000001";
            migrationBuilder.Sql($"""
                UPDATE "TransactionCategories" SET "Name" = '電費' WHERE "Id" = '{subUtility}';
            """);

            // ── 新增支出主分類 ──────────────────────
            InsertCategory(migrationBuilder, CatExpSubscription, null, "訂閱", "mdi:credit-card-clock", "Expense", 8);
            InsertCategory(migrationBuilder, CatExpPersonal,     null, "個人", "mdi:account",           "Expense", 9);
            InsertCategory(migrationBuilder, CatExpDonation,     null, "奉獻", "mdi:hand-heart",        "Expense", 10);

            // ── 飲食子分類（補齊）──────────────────────
            InsertCategory(migrationBuilder, SubDessert,  CatExpFood, "點心", null, "Expense", 6);
            InsertCategory(migrationBuilder, SubCoffee,   CatExpFood, "咖啡", null, "Expense", 7);
            InsertCategory(migrationBuilder, SubGrocery,  CatExpFood, "超市", null, "Expense", 8);

            // ── 交通子分類（補齊）──────────────────────
            InsertCategory(migrationBuilder, SubHsr,        CatExpTransit, "高鐵",   null, "Expense", 6);
            InsertCategory(migrationBuilder, SubTrain,      CatExpTransit, "火車",   null, "Expense", 7);
            InsertCategory(migrationBuilder, SubMotorcycle, CatExpTransit, "機車",   null, "Expense", 8);

            // ── 購物子分類（補齊）──────────────────────
            InsertCategory(migrationBuilder, SubEbook, CatExpShop, "電子書", null, "Expense", 4);

            // ── 家居子分類（補齊）──────────────────────
            InsertCategory(migrationBuilder, SubMortgage,    CatExpHome, "房貸",     null, "Expense", 4);
            InsertCategory(migrationBuilder, SubPhone,       CatExpHome, "電話費",   null, "Expense", 5);
            InsertCategory(migrationBuilder, SubMgmtFee,     CatExpHome, "管理費",   null, "Expense", 6);
            InsertCategory(migrationBuilder, SubFurniture,   CatExpHome, "家具",     null, "Expense", 7);
            InsertCategory(migrationBuilder, SubWater,       CatExpHome, "水費",     null, "Expense", 8);
            InsertCategory(migrationBuilder, SubGasUtil,     CatExpHome, "瓦斯",     null, "Expense", 9);

            // ── 娛樂子分類（補齊）──────────────────────
            InsertCategory(migrationBuilder, SubMobileGame, CatExpFun, "手遊", null, "Expense", 4);

            // ── 學習子分類（補齊）──────────────────────
            InsertCategory(migrationBuilder, SubOnlineCourse, CatExpEdu, "線上課程", null, "Expense", 3);

            // ── 訂閱子分類 ────────────────────────────
            InsertCategory(migrationBuilder, SubAiTool,      CatExpSubscription, "AI 工具",     null, "Expense", 1);
            InsertCategory(migrationBuilder, SubMusicStream, CatExpSubscription, "音樂串流",     null, "Expense", 2);
            InsertCategory(migrationBuilder, SubCloudStorage, CatExpSubscription, "雲端儲存",    null, "Expense", 3);
            InsertCategory(migrationBuilder, SubBookkeeping, CatExpSubscription, "記帳軟體",     null, "Expense", 4);

            // ── 個人子分類 ────────────────────────────
            InsertCategory(migrationBuilder, SubInsurance,   CatExpPersonal, "保險",     null, "Expense", 1);
            InsertCategory(migrationBuilder, SubMobilePhone, CatExpPersonal, "手機費",   null, "Expense", 2);
            InsertCategory(migrationBuilder, SubHaircut,     CatExpPersonal, "理髮",     null, "Expense", 3);

            // ── 奉獻子分類 ────────────────────────────
            InsertCategory(migrationBuilder, SubTithe,      CatExpDonation, "十一奉獻", null, "Expense", 1);
            InsertCategory(migrationBuilder, SubDesignated,  CatExpDonation, "指定奉獻", null, "Expense", 2);

            // ── 收入子分類（補齊）────────────────────────
            InsertCategory(migrationBuilder, SubStockSale,    CatIncInvest, "股票賣出", null, "Income", 1);
            InsertCategory(migrationBuilder, SubBankInterest, CatIncInvest, "銀行利息", null, "Income", 2);
            InsertCategory(migrationBuilder, SubCashback,     CatIncInvest, "回饋金",   null, "Income", 3);
            InsertCategory(migrationBuilder, SubTransferInc,  CatIncOther,  "轉帳收入", null, "Income", 1);
            InsertCategory(migrationBuilder, SubRefund,       CatIncOther,  "退款",     null, "Income", 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 還原重命名
            migrationBuilder.Sql($"""
                UPDATE "TransactionCategories" SET "Name" = '居住' WHERE "Id" = '{CatExpHome}';
            """);
            migrationBuilder.Sql($"""
                UPDATE "TransactionCategories" SET "Name" = '教育' WHERE "Id" = '{CatExpEdu}';
            """);
            var subUtility = "c0000001-0005-0002-0000-000000000001";
            migrationBuilder.Sql($"""
                UPDATE "TransactionCategories" SET "Name" = '水電瓦斯' WHERE "Id" = '{subUtility}';
            """);

            // 刪除新增的分類
            migrationBuilder.Sql($"""
                DELETE FROM "TransactionCategories" WHERE "Id" IN (
                    '{CatExpSubscription}', '{CatExpPersonal}', '{CatExpDonation}',
                    '{SubDessert}', '{SubCoffee}', '{SubGrocery}',
                    '{SubHsr}', '{SubTrain}', '{SubMotorcycle}',
                    '{SubEbook}',
                    '{SubMortgage}', '{SubPhone}', '{SubMgmtFee}', '{SubFurniture}', '{SubWater}', '{SubGasUtil}',
                    '{SubMobileGame}', '{SubOnlineCourse}',
                    '{SubAiTool}', '{SubMusicStream}', '{SubCloudStorage}', '{SubBookkeeping}',
                    '{SubInsurance}', '{SubMobilePhone}', '{SubHaircut}',
                    '{SubTithe}', '{SubDesignated}',
                    '{SubStockSale}', '{SubBankInterest}', '{SubCashback}',
                    '{SubTransferInc}', '{SubRefund}'
                );
            """);
        }

        private static void InsertCategory(
            MigrationBuilder migrationBuilder,
            string id,
            string? parentId,
            string name,
            string? icon,
            string categoryType,
            int sortOrder)
        {
            var parentSql = parentId is null ? "NULL" : $"'{parentId}'";
            var iconSql = icon is null ? "NULL" : $"'{icon}'";
            migrationBuilder.Sql($"""
                INSERT INTO "TransactionCategories" ("Id", "UserId", "ParentId", "Name", "Icon", "CategoryType", "SortOrder", "IsActive", "CreatedAt")
                VALUES ('{id}', NULL, {parentSql}, '{name}', {iconSql}, '{categoryType}', {sortOrder}, true, NOW());
            """);
        }
    }
}
