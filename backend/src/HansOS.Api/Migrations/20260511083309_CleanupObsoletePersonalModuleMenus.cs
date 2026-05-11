using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class CleanupObsoletePersonalModuleMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "AspNetUsers"
                SET "HomePath" = '/index'
                WHERE "HomePath" IN ('/hope-media', '/todo', '/article-collection')
                   OR "HomePath" LIKE '/hope-media/%'
                   OR "HomePath" LIKE '/todo/%'
                   OR "HomePath" LIKE '/article-collection/%';
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM "RoleMenus"
                WHERE "MenuId" IN (
                    SELECT "Id"
                    FROM "Menus"
                    WHERE "Path" IN ('/hope-media', '/todo', '/article-collection')
                       OR "Path" LIKE '/hope-media/%'
                       OR "Path" LIKE '/todo/%'
                       OR "Path" LIKE '/article-collection/%'
                );
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM "Menus"
                WHERE "Path" LIKE '/hope-media/%'
                   OR "Path" LIKE '/todo/%'
                   OR "Path" LIKE '/article-collection/%';
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM "Menus"
                WHERE "Path" IN ('/hope-media', '/todo', '/article-collection');
                """);

            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "ArticleBookmarks";
                DROP TABLE IF EXISTS "TodoItemTag";
                DROP TABLE IF EXISTS "ArticleBookmarkGroups";
                DROP TABLE IF EXISTS "TodoItems";
                DROP TABLE IF EXISTS "TodoTags";
                DROP TABLE IF EXISTS "TodoCategories";
                DROP TABLE IF EXISTS "TodoProjects";
                DROP TABLE IF EXISTS "__TodoChecklistMigrationMap";
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
