using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultCategories : Migration
    {
        // ── 預設分類 ID（系統級，UserId = NULL）──────────────────────

        // 支出主分類
        private const string CatExpFood     = "c0000001-0001-0000-0000-000000000001";
        private const string CatExpTransit  = "c0000001-0002-0000-0000-000000000001";
        private const string CatExpFun      = "c0000001-0003-0000-0000-000000000001";
        private const string CatExpShop     = "c0000001-0004-0000-0000-000000000001";
        private const string CatExpHome     = "c0000001-0005-0000-0000-000000000001";
        private const string CatExpMed      = "c0000001-0006-0000-0000-000000000001";
        private const string CatExpEdu      = "c0000001-0007-0000-0000-000000000001";
        private const string CatExpOther    = "c0000001-0008-0000-0000-000000000001";

        // 收入主分類
        private const string CatIncSalary   = "c0000002-0001-0000-0000-000000000001";
        private const string CatIncBonus    = "c0000002-0002-0000-0000-000000000001";
        private const string CatIncInvest   = "c0000002-0003-0000-0000-000000000001";
        private const string CatIncSide     = "c0000002-0004-0000-0000-000000000001";
        private const string CatIncOther    = "c0000002-0005-0000-0000-000000000001";

        // 支出子分類 — 飲食
        private const string SubBreakfast   = "c0000001-0001-0001-0000-000000000001";
        private const string SubLunch       = "c0000001-0001-0002-0000-000000000001";
        private const string SubDinner      = "c0000001-0001-0003-0000-000000000001";
        private const string SubDrink       = "c0000001-0001-0004-0000-000000000001";
        private const string SubSnack       = "c0000001-0001-0005-0000-000000000001";

        // 支出子分類 — 交通
        private const string SubBus         = "c0000001-0002-0001-0000-000000000001";
        private const string SubMrt         = "c0000001-0002-0002-0000-000000000001";
        private const string SubTaxi        = "c0000001-0002-0003-0000-000000000001";
        private const string SubParking     = "c0000001-0002-0004-0000-000000000001";
        private const string SubGas         = "c0000001-0002-0005-0000-000000000001";

        // 支出子分類 — 娛樂
        private const string SubMovie       = "c0000001-0003-0001-0000-000000000001";
        private const string SubGame        = "c0000001-0003-0002-0000-000000000001";
        private const string SubParty       = "c0000001-0003-0003-0000-000000000001";

        // 支出子分類 — 購物
        private const string SubClothes     = "c0000001-0004-0001-0000-000000000001";
        private const string SubTech        = "c0000001-0004-0002-0000-000000000001";
        private const string SubDaily       = "c0000001-0004-0003-0000-000000000001";

        // 支出子分類 — 居住
        private const string SubRent        = "c0000001-0005-0001-0000-000000000001";
        private const string SubUtility     = "c0000001-0005-0002-0000-000000000001";
        private const string SubInternet    = "c0000001-0005-0003-0000-000000000001";

        // 支出子分類 — 醫療
        private const string SubDoctor      = "c0000001-0006-0001-0000-000000000001";
        private const string SubMedicine    = "c0000001-0006-0002-0000-000000000001";

        // 支出子分類 — 教育
        private const string SubBooks       = "c0000001-0007-0001-0000-000000000001";
        private const string SubCourse      = "c0000001-0007-0002-0000-000000000001";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 支出主分類 ──────────────────────
            InsertCategory(migrationBuilder, CatExpFood,    null, "飲食",  "Expense", 1);
            InsertCategory(migrationBuilder, CatExpTransit, null, "交通",  "Expense", 2);
            InsertCategory(migrationBuilder, CatExpFun,     null, "娛樂",  "Expense", 3);
            InsertCategory(migrationBuilder, CatExpShop,    null, "購物",  "Expense", 4);
            InsertCategory(migrationBuilder, CatExpHome,    null, "居住",  "Expense", 5);
            InsertCategory(migrationBuilder, CatExpMed,     null, "醫療",  "Expense", 6);
            InsertCategory(migrationBuilder, CatExpEdu,     null, "教育",  "Expense", 7);
            InsertCategory(migrationBuilder, CatExpOther,   null, "其他",  "Expense", 99);

            // ── 收入主分類 ──────────────────────
            InsertCategory(migrationBuilder, CatIncSalary, null, "薪資",     "Income", 1);
            InsertCategory(migrationBuilder, CatIncBonus,  null, "獎金",     "Income", 2);
            InsertCategory(migrationBuilder, CatIncInvest, null, "投資收益", "Income", 3);
            InsertCategory(migrationBuilder, CatIncSide,   null, "副業收入", "Income", 4);
            InsertCategory(migrationBuilder, CatIncOther,  null, "其他",     "Income", 99);

            // ── 支出子分類：飲食 ──────────────────
            InsertCategory(migrationBuilder, SubBreakfast, CatExpFood, "早餐", "Expense", 1);
            InsertCategory(migrationBuilder, SubLunch,     CatExpFood, "午餐", "Expense", 2);
            InsertCategory(migrationBuilder, SubDinner,    CatExpFood, "晚餐", "Expense", 3);
            InsertCategory(migrationBuilder, SubDrink,     CatExpFood, "飲料", "Expense", 4);
            InsertCategory(migrationBuilder, SubSnack,     CatExpFood, "零食", "Expense", 5);

            // ── 支出子分類：交通 ──────────────────
            InsertCategory(migrationBuilder, SubBus,     CatExpTransit, "公車",   "Expense", 1);
            InsertCategory(migrationBuilder, SubMrt,     CatExpTransit, "捷運",   "Expense", 2);
            InsertCategory(migrationBuilder, SubTaxi,    CatExpTransit, "計程車", "Expense", 3);
            InsertCategory(migrationBuilder, SubParking,  CatExpTransit, "停車費", "Expense", 4);
            InsertCategory(migrationBuilder, SubGas,     CatExpTransit, "加油",   "Expense", 5);

            // ── 支出子分類：娛樂 ──────────────────
            InsertCategory(migrationBuilder, SubMovie, CatExpFun, "電影", "Expense", 1);
            InsertCategory(migrationBuilder, SubGame,  CatExpFun, "遊戲", "Expense", 2);
            InsertCategory(migrationBuilder, SubParty, CatExpFun, "聚會", "Expense", 3);

            // ── 支出子分類：購物 ──────────────────
            InsertCategory(migrationBuilder, SubClothes, CatExpShop, "衣服",   "Expense", 1);
            InsertCategory(migrationBuilder, SubTech,    CatExpShop, "3C",     "Expense", 2);
            InsertCategory(migrationBuilder, SubDaily,   CatExpShop, "日用品", "Expense", 3);

            // ── 支出子分類：居住 ──────────────────
            InsertCategory(migrationBuilder, SubRent,     CatExpHome, "房租",   "Expense", 1);
            InsertCategory(migrationBuilder, SubUtility,  CatExpHome, "水電瓦斯", "Expense", 2);
            InsertCategory(migrationBuilder, SubInternet, CatExpHome, "網路",   "Expense", 3);

            // ── 支出子分類：醫療 ──────────────────
            InsertCategory(migrationBuilder, SubDoctor,   CatExpMed, "看診", "Expense", 1);
            InsertCategory(migrationBuilder, SubMedicine, CatExpMed, "藥品", "Expense", 2);

            // ── 支出子分類：教育 ──────────────────
            InsertCategory(migrationBuilder, SubBooks,  CatExpEdu, "書籍", "Expense", 1);
            InsertCategory(migrationBuilder, SubCourse, CatExpEdu, "課程", "Expense", 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "TransactionCategories" WHERE "UserId" IS NULL;
            """);
        }

        private static void InsertCategory(
            MigrationBuilder migrationBuilder,
            string id,
            string? parentId,
            string name,
            string categoryType,
            int sortOrder)
        {
            var parentSql = parentId is null ? "NULL" : $"'{parentId}'";
            migrationBuilder.Sql($"""
                INSERT INTO "TransactionCategories" ("Id", "UserId", "ParentId", "Name", "Icon", "CategoryType", "SortOrder", "IsActive", "CreatedAt")
                VALUES ('{id}', NULL, {parentSql}, '{name}', NULL, '{categoryType}', {sortOrder}, true, NOW());
            """);
        }
    }
}
