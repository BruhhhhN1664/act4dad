using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EquipmentBorrowing.Infrastructure.Migrations
{
    public partial class InitialPostgres : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipmentIdentifier = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EquipmentIdentifierNormalized = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                    table.CheckConstraint("CK_Equipment_IdentifierNotBlank", "btrim(\"EquipmentIdentifier\") <> ''");
                    table.CheckConstraint("CK_Equipment_NameNotBlank", "btrim(\"Name\") <> ''");
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StudentNumberNormalized = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Contact = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsAllowedToBorrow = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.CheckConstraint("CK_Students_NameNotBlank", "btrim(\"FullName\") <> ''");
                    table.CheckConstraint("CK_Students_NumberNotBlank", "btrim(\"StudentNumber\") <> ''");
                });

            migrationBuilder.CreateTable(
                name: "Borrowings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    EquipmentId = table.Column<int>(type: "integer", nullable: false),
                    BorrowedAt = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedReturnAt = table.Column<DateOnly>(type: "date", nullable: false),
                    ReturnedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Borrowings", x => x.Id);
                    table.CheckConstraint("CK_Borrowings_DueAfterBorrowed", "\"ExpectedReturnAt\" > \"BorrowedAt\"");
                    table.CheckConstraint("CK_Borrowings_StatusMatchesReturn", "(\"Status\" = 'Active' AND \"ReturnedAt\" IS NULL) OR (\"Status\" = 'Returned' AND \"ReturnedAt\" IS NOT NULL AND \"ReturnedAt\" >= \"BorrowedAt\")");
                    table.ForeignKey(
                        name: "FK_Borrowings_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Borrowings_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Borrowings_StudentId_Lookup",
                table: "Borrowings",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "UX_Borrowings_ActiveByEquipment",
                table: "Borrowings",
                column: "EquipmentId",
                unique: true,
                filter: "\"ReturnedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Equipment_IdentifierNormalized",
                table: "Equipment",
                column: "EquipmentIdentifierNormalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Students_StudentNumberNormalized",
                table: "Students",
                column: "StudentNumberNormalized",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Borrowings");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "Students");
        }
    }
}
