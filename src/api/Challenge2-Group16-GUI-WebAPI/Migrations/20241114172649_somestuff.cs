using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Challenge2_Group16_GUI_WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class somestuff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErrorAggregateData");

            migrationBuilder.DropTable(
                name: "ErrorData");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "DeviceStatusAggregateData");

            migrationBuilder.AlterColumn<long>(
                name: "Status",
                table: "DeviceStatusData",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<byte[]>(
                name: "StatusAggregate",
                table: "DeviceStatusAggregateData",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "LogAggregateData",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataTimeStamps = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Logs = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogAggregateData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogAggregateData_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogData",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogData_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogAggregateData_ClientId",
                table: "LogAggregateData",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_LogData_ClientId",
                table: "LogData",
                column: "ClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogAggregateData");

            migrationBuilder.DropTable(
                name: "LogData");

            migrationBuilder.DropColumn(
                name: "StatusAggregate",
                table: "DeviceStatusAggregateData");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DeviceStatusData",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DeviceStatusAggregateData",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ErrorAggregateData",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DataTimeStamps = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Errors = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorAggregateData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErrorAggregateData_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ErrorData",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Error = table.Column<int>(type: "int", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErrorData_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorAggregateData_ClientId",
                table: "ErrorAggregateData",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorData_ClientId",
                table: "ErrorData",
                column: "ClientId");
        }
    }
}
