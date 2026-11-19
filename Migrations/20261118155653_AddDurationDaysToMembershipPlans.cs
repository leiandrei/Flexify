using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrudApp.Migrations;

/// <inheritdoc />
public partial class _20261118155653_AddDurationDaysToMembershipPlans : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "durationDays",
            table: "MembershipPlans",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "durationDays",
            table: "MembershipPlans");
    }
}
