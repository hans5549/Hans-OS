using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArticleBookmarkGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleBookmarkGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleBookmarkGroups_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArticleBookmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CustomTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ExcerptSnapshot = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CoverImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Domain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "ARRAY[]::text[]"),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastOpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleBookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleBookmarks_ArticleBookmarkGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "ArticleBookmarkGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ArticleBookmarks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarkGroups_UserId_Name",
                table: "ArticleBookmarkGroups",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarkGroups_UserId_SortOrder",
                table: "ArticleBookmarkGroups",
                columns: new[] { "UserId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarks_GroupId",
                table: "ArticleBookmarks",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarks_UserId_CreatedAt",
                table: "ArticleBookmarks",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarks_UserId_GroupId",
                table: "ArticleBookmarks",
                columns: new[] { "UserId", "GroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarks_UserId_SourceId",
                table: "ArticleBookmarks",
                columns: new[] { "UserId", "SourceId" },
                unique: true,
                filter: "\"SourceType\" = 'InternalArticle' AND \"SourceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarks_UserId_SourceType",
                table: "ArticleBookmarks",
                columns: new[] { "UserId", "SourceType" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarks_UserId_Url",
                table: "ArticleBookmarks",
                columns: new[] { "UserId", "Url" },
                unique: true,
                filter: "\"SourceType\" = 'ExternalUrl' AND \"Url\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleBookmarks");

            migrationBuilder.DropTable(
                name: "ArticleBookmarkGroups");
        }
    }
}
