using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CGMSportFinance.Api.Infrastructure.Persistence.Migrations
{
    public partial class AddUserProfiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Introduction = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    NotifyAccountPassword = table.Column<bool>(type: "boolean", nullable: false),
                    NotifySystemMessage = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyTodoTask = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProfiles");
        }
    }
}
