using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infra.Migrations
{
    /// <inheritdoc />
    public partial class MakeEmailMessageIdsProviderNeutral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailMessages_SesMessageId",
                table: "EmailMessages");

            migrationBuilder.RenameColumn(
                name: "SesMessageId",
                table: "EmailMessages",
                newName: "ProviderMessageId");

            migrationBuilder.RenameColumn(
                name: "TopicArn",
                table: "EmailEvents",
                newName: "TransportTopic");

            migrationBuilder.RenameColumn(
                name: "SnsMessageId",
                table: "EmailEvents",
                newName: "TransportMessageId");

            migrationBuilder.RenameColumn(
                name: "SesMessageId",
                table: "EmailEvents",
                newName: "ProviderMessageId");

            migrationBuilder.RenameIndex(
                name: "IX_EmailEvents_SesMessageId_EventType",
                table: "EmailEvents",
                newName: "IX_EmailEvents_ProviderMessageId_EventType");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_ProviderMessageId",
                table: "EmailMessages",
                column: "ProviderMessageId",
                unique: true,
                filter: "\"ProviderMessageId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailMessages_ProviderMessageId",
                table: "EmailMessages");

            migrationBuilder.RenameColumn(
                name: "ProviderMessageId",
                table: "EmailMessages",
                newName: "SesMessageId");

            migrationBuilder.RenameColumn(
                name: "TransportTopic",
                table: "EmailEvents",
                newName: "TopicArn");

            migrationBuilder.RenameColumn(
                name: "TransportMessageId",
                table: "EmailEvents",
                newName: "SnsMessageId");

            migrationBuilder.RenameColumn(
                name: "ProviderMessageId",
                table: "EmailEvents",
                newName: "SesMessageId");

            migrationBuilder.RenameIndex(
                name: "IX_EmailEvents_ProviderMessageId_EventType",
                table: "EmailEvents",
                newName: "IX_EmailEvents_SesMessageId_EventType");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_SesMessageId",
                table: "EmailMessages",
                column: "SesMessageId",
                unique: true,
                filter: "\"SesMessageId\" IS NOT NULL");
        }
    }
}
