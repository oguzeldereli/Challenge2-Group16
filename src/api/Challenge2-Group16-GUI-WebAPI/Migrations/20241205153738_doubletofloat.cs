using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Challenge2_Group16_GUI_WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class doubletofloat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceStatusAggregateData");

            migrationBuilder.DropTable(
                name: "LogAggregateData");

            migrationBuilder.DropTable(
                name: "pHAggregateData");

            migrationBuilder.DropTable(
                name: "StirringAggregateData");

            migrationBuilder.DropTable(
                name: "TempAggregateData");

            migrationBuilder.AlterColumn<float>(
                name: "Temperature",
                table: "TempData",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<float>(
                name: "RPM",
                table: "StirringData",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<float>(
                name: "TempTarget",
                table: "DeviceStatusData",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<float>(
                name: "RPMTarget",
                table: "DeviceStatusData",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<float>(
                name: "PhTarget",
                table: "DeviceStatusData",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "Temperature",
                table: "TempData",
                type: "float",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<double>(
                name: "RPM",
                table: "StirringData",
                type: "float",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<double>(
                name: "TempTarget",
                table: "DeviceStatusData",
                type: "float",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<double>(
                name: "RPMTarget",
                table: "DeviceStatusData",
                type: "float",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<double>(
                name: "PhTarget",
                table: "DeviceStatusData",
                type: "float",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.CreateTable(
                name: "DeviceStatusAggregateData",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DataTimeStamps = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    StatusAggregate = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceStatusAggregateData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceStatusAggregateData_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogAggregateData",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DataTimeStamps = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Logs = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                name: "pHAggregateData",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DataTimeStamps = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    pHAggregate = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pHAggregateData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pHAggregateData_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StirringAggregateData",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DataTimeStamps = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    RPMAggregate = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StirringAggregateData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StirringAggregateData_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TempAggregateData",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DataTimeStamps = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    TemperatureAggregate = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TempAggregateData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TempAggregateData_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceStatusAggregateData_ClientId",
                table: "DeviceStatusAggregateData",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_LogAggregateData_ClientId",
                table: "LogAggregateData",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_pHAggregateData_ClientId",
                table: "pHAggregateData",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_StirringAggregateData_ClientId",
                table: "StirringAggregateData",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_TempAggregateData_ClientId",
                table: "TempAggregateData",
                column: "ClientId");
        }
    }
}
