using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Challenge2_Group16_GUI_WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class removedEncryption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptionIV",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "EncryptionKey",
                table: "Clients");

            migrationBuilder.AddColumn<long>(
                name: "PhTarget",
                table: "DeviceStatusData",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "RPMTarget",
                table: "DeviceStatusData",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TempTarget",
                table: "DeviceStatusData",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhTarget",
                table: "DeviceStatusData");

            migrationBuilder.DropColumn(
                name: "RPMTarget",
                table: "DeviceStatusData");

            migrationBuilder.DropColumn(
                name: "TempTarget",
                table: "DeviceStatusData");

            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptionIV",
                table: "Clients",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptionKey",
                table: "Clients",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
