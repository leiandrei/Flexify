using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrudApp.Migrations;

/// <inheritdoc />
public partial class _20261118165200_AddPaymentsDateToPayments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "paymentDate",
            table: "Payments",
            type: "datetime2",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "paymentDate",
            table: "Payments");
    }
}
