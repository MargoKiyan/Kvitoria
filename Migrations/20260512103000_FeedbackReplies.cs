using System;
using Kvitoria.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kvitoria.Migrations;

[DbContext(typeof(KvitoriaDbContext))]
[Migration("20260512103000_FeedbackReplies")]
public partial class FeedbackReplies : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "admin_reply",
            table: "feedback_messages",
            type: "character varying(2500)",
            maxLength: 2500,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "replied_at_utc",
            table: "feedback_messages",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "replied_by_admin_id",
            table: "feedback_messages",
            type: "text",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_feedback_messages_replied_by_admin_id",
            table: "feedback_messages",
            column: "replied_by_admin_id");

        migrationBuilder.AddForeignKey(
            name: "FK_feedback_messages_AspNetUsers_replied_by_admin_id",
            table: "feedback_messages",
            column: "replied_by_admin_id",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_feedback_messages_AspNetUsers_replied_by_admin_id",
            table: "feedback_messages");

        migrationBuilder.DropIndex(
            name: "IX_feedback_messages_replied_by_admin_id",
            table: "feedback_messages");

        migrationBuilder.DropColumn(
            name: "admin_reply",
            table: "feedback_messages");

        migrationBuilder.DropColumn(
            name: "replied_at_utc",
            table: "feedback_messages");

        migrationBuilder.DropColumn(
            name: "replied_by_admin_id",
            table: "feedback_messages");
    }
}
