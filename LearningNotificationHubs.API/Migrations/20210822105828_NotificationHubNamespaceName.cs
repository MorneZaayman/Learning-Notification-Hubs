using Microsoft.EntityFrameworkCore.Migrations;

namespace LearningNotificationHubs.API.Migrations
{
    public partial class NotificationHubNamespaceName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotificationHubNamespaceName",
                table: "Devices",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationHubNamespaceName",
                table: "Devices");
        }
    }
}
