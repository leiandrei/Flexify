using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCrudApp.Migrations;

/// <inheritdoc />
public partial class _20261118074858_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MembershipPlans",
            columns: table => new
            {
                planId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                planName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                planPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                planDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                isActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MembershipPlans", x => x.planId);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                userId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                userEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                passwordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                isActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.userId);
            });

        migrationBuilder.CreateTable(
            name: "Members",
            columns: table => new
            {
                memberId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                userId = table.Column<int>(type: "int", nullable: false),
                userId1 = table.Column<int>(type: "int", nullable: true),
                fullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                contactNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                birthdate = table.Column<DateTime>(type: "datetime2", nullable: false),
                userEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                joinDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                isActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Members", x => x.memberId);
                table.ForeignKey(
                    name: "FK_Members_Users_userId",
                    column: x => x.userId,
                    principalTable: "Users",
                    principalColumn: "userId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Members_Users_userId1",
                    column: x => x.userId1,
                    principalTable: "Users",
                    principalColumn: "userId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Staff",
            columns: table => new
            {
                StaffID = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                userId = table.Column<int>(type: "int", nullable: false),
                userId1 = table.Column<int>(type: "int", nullable: true),
                fullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                position = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                dateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Staff", x => x.StaffID);
                table.ForeignKey(
                    name: "FK_Staff_Users_userId",
                    column: x => x.userId,
                    principalTable: "Users",
                    principalColumn: "userId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Staff_Users_userId1",
                    column: x => x.userId1,
                    principalTable: "Users",
                    principalColumn: "userId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Attendance",
            columns: table => new
            {
                attendanceId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                memberId = table.Column<int>(type: "int", nullable: false),
                checkIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                checkOut = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Attendance", x => x.attendanceId);
                table.ForeignKey(
                    name: "FK_Attendance_Members_memberId",
                    column: x => x.memberId,
                    principalTable: "Members",
                    principalColumn: "memberId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Memberships",
            columns: table => new
            {
                membershipId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                memberId = table.Column<int>(type: "int", nullable: false),
                planId = table.Column<int>(type: "int", nullable: false),
                startDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                endDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                isActive = table.Column<bool>(type: "bit", nullable: false),
                lastReminderSent = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Memberships", x => x.membershipId);
                table.ForeignKey(
                    name: "FK_Memberships_Members_memberId",
                    column: x => x.memberId,
                    principalTable: "Members",
                    principalColumn: "memberId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Memberships_MembershipPlans_planId",
                    column: x => x.planId,
                    principalTable: "MembershipPlans",
                    principalColumn: "planId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Payments",
            columns: table => new
            {
                paymentId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                memberId = table.Column<int>(type: "int", nullable: false),
                membershipId = table.Column<int>(type: "int", nullable: false),
                amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                paymentType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                referenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                receiptImagePath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                ocrRawText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                isVoided = table.Column<bool>(type: "bit", nullable: false),
                recordedByStaffId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Payments", x => x.paymentId);
                table.ForeignKey(
                    name: "FK_Payments_Members_memberId",
                    column: x => x.memberId,
                    principalTable: "Members",
                    principalColumn: "memberId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Payments_Memberships_membershipId",
                    column: x => x.membershipId,
                    principalTable: "Memberships",
                    principalColumn: "membershipId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Payments_Staff_recordedByStaffId",
                    column: x => x.recordedByStaffId,
                    principalTable: "Staff",
                    principalColumn: "StaffID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Attendance_memberId",
            table: "Attendance",
            column: "memberId");

        migrationBuilder.CreateIndex(
            name: "IX_Members_userId",
            table: "Members",
            column: "userId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Members_userId1",
            table: "Members",
            column: "userId1");

        migrationBuilder.CreateIndex(
            name: "IX_Memberships_memberId",
            table: "Memberships",
            column: "memberId");

        migrationBuilder.CreateIndex(
            name: "IX_Memberships_planId",
            table: "Memberships",
            column: "planId");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_memberId",
            table: "Payments",
            column: "memberId");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_membershipId",
            table: "Payments",
            column: "membershipId");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_recordedByStaffId",
            table: "Payments",
            column: "recordedByStaffId");

        migrationBuilder.CreateIndex(
            name: "IX_Staff_userId",
            table: "Staff",
            column: "userId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Staff_userId1",
            table: "Staff",
            column: "userId1");

        migrationBuilder.CreateIndex(
            name: "IX_Users_userEmail",
            table: "Users",
            column: "userEmail",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Attendance");

        migrationBuilder.DropTable(
            name: "Payments");

        migrationBuilder.DropTable(
            name: "Memberships");

        migrationBuilder.DropTable(
            name: "Staff");

        migrationBuilder.DropTable(
            name: "Members");

        migrationBuilder.DropTable(
            name: "MembershipPlans");

        migrationBuilder.DropTable(
            name: "Users");
    }
}
