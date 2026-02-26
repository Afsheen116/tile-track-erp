using CeramicERP.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace CeramicERP.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260226141000_AddPaymentEntries")]
    public partial class AddPaymentEntries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Counterparty = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    EntryType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PaymentType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    BillingReference = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    EntryDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    InvoiceAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    SettledAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    DueAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentEntries", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentEntries");
        }
    }
}
