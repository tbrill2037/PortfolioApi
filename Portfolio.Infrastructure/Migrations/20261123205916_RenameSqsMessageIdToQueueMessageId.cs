using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infra.Migrations
{
    /// <inheritdoc />
    public partial class RenameSqsMessageIdToQueueMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SqsMessageId",
                table: "EmailDeadLetters",
                newName: "QueueMessageId");

            migrationBuilder.RenameIndex(
                name: "IX_EmailDeadLetters_SqsMessageId",
                table: "EmailDeadLetters",
                newName: "IX_EmailDeadLetters_QueueMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "QueueMessageId",
                table: "EmailDeadLetters",
                newName: "SqsMessageId");

            migrationBuilder.RenameIndex(
                name: "IX_EmailDeadLetters_QueueMessageId",
                table: "EmailDeadLetters",
                newName: "IX_EmailDeadLetters_SqsMessageId");
        }
    }
}
