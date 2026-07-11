using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusManagement.Model.Migrations
{
    /// <inheritdoc />
    public partial class firstReleaseBus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TM_APPROVAL_HISTORY",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    INT_BUS_REQEST_ID = table.Column<int>(type: "int", nullable: true),
                    INT_TRIP_REQEST_ID = table.Column<int>(type: "int", nullable: true),
                    INT_GROUP_BATCH_APPLICATIONS = table.Column<int>(type: "int", nullable: true),
                    CHR_CONFIRMER_APPROVER = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INT_ACTION = table.Column<int>(type: "int", nullable: true),
                    BIT_ACTION = table.Column<bool>(type: "bit", nullable: true),
                    CHR_ACTION = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DTM_ACTION = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NVCHR_REASON_REJECT = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_APPROVAL_HISTORY", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_BUS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CHR_BUS_CODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_BUS_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_LICENSE_PLATE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_CATEGORY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_BRAND = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_MODEL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_MODEL_YEAR = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INT_CAPACITY = table.Column<int>(type: "int", nullable: true),
                    INT_VENDOR_ID = table.Column<int>(type: "int", nullable: true),
                    BIT_IS_ACTIVE = table.Column<bool>(type: "bit", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_BUS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_BUS_OT",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DTM_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    INT_ROUTE_ID = table.Column<int>(type: "int", nullable: true),
                    INT_BUS_ID = table.Column<int>(type: "int", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_BUS_OT", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_BUS_REQUEST",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CHR_EMPLOYEE_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INT_ROUTE_ID = table.Column<int>(type: "int", nullable: true),
                    INT_BUS_STOP_ID = table.Column<int>(type: "int", nullable: true),
                    DTM_START_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DTM_CANCEL_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    INT_REQUEST_TYPE = table.Column<int>(type: "int", nullable: true),
                    INT_STATUS = table.Column<int>(type: "int", nullable: true),
                    CHR_QLSC_APPROVE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_GA_CONFIRM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_BUS_REQUEST", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_BUS_ROUTE",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NVCHR_ROUTE_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_ROUTE_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INT_ID_ROUTE_TYPE = table.Column<int>(type: "int", nullable: true),
                    INT_CAPACITY = table.Column<int>(type: "int", nullable: true),
                    BIT_IS_ACTIVE = table.Column<bool>(type: "bit", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_BUS_ROUTE", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_BUS_ROUTE_STOP",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    INT_ROUTE_ID = table.Column<int>(type: "int", nullable: true),
                    INT_BUS_STOP_ID = table.Column<int>(type: "int", nullable: true),
                    INT_ORDER = table.Column<int>(type: "int", nullable: true),
                    DTM_PICKUP_TIME = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_BUS_ROUTE_STOP", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_BUS_STOP",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NVCHR_STOP_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_STOP_ADDRESS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_STOP_CITY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_STOP_WARD = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_COORDINATES = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FLT_DISTANCE_TO_BIVN = table.Column<double>(type: "float", nullable: true),
                    FLT_DISTANCE_TO_TTF = table.Column<double>(type: "float", nullable: true),
                    BIT_IS_ACTIVE = table.Column<bool>(type: "bit", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_BUS_STOP", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_CARD",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CHR_EMPLOYEE_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_CARD_CODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INT_CARD_TYPE = table.Column<int>(type: "int", nullable: true),
                    NVCHR_CARD_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INT_CARD_SCOPE = table.Column<int>(type: "int", nullable: true),
                    NVCHR_CARD_SCOPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DTM_ISSUE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DTM_EXPRIRATION_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BIT_IS_ACTIVE = table.Column<bool>(type: "bit", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_CARD", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_CARD_BORROW",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    INT_CARD_ID = table.Column<int>(type: "int", nullable: true),
                    CHR_CARD_CODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_EMPLOYEE_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DTM_BORROW_TIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DTM_RETURN_TIME = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_CARD_BORROW", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_CARD_USAGE",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    INT_CARD_ID = table.Column<int>(type: "int", nullable: true),
                    CHR_CARD_CODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_EMPLOYEE_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DTM_USE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NVCHR_FROM_ADDRESS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_TO_ADDRESS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INT_AMOUNT = table.Column<int>(type: "int", nullable: true),
                    NVCHR_NOTE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_CARD_USAGE", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_DRIVER",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NVCHR_DRIVER_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_DRIVER_PHONE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_LICENSE_CATEGORY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_DATE_OF_BIRTH = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_ACCESS_CARD_CODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INT_VENDOR_ID = table.Column<int>(type: "int", nullable: true),
                    CHR_AVARTAR_PATH = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BIT_IS_ACTIVE = table.Column<bool>(type: "bit", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_DRIVER", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_EMPLOYEE",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CHR_EMPLOYEE_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_EMPLOYEE_ADID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_EMPLOYEE_MAIL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_EMPLOYEE_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_CODE_DEPT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_CODE_SEC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_CODE_GROUP = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_POSITION = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_POSITION_GROUP = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_PHONE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DTM_BIRTHDAY = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CHR_ADDRESS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_CITY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_WARD = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_ADDRESS_DETAIL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BIT_GENDER = table.Column<bool>(type: "bit", nullable: true),
                    BIT_IS_ACTIVE = table.Column<bool>(type: "bit", nullable: true),
                    CHR_NOTE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_EMPLOYEE", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_MASTER_MAIL",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CHR_MAIL_CODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_FROM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_TO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_CC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_BCC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_SUBJECT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_CONTENT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_ATTACH = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BIT_HIGHT = table.Column<bool>(type: "bit", nullable: true),
                    CHR_NOTE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_MASTER_MAIL", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_MENU",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CHR_MENU_CODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_MENU_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INT_ROLE_ID = table.Column<int>(type: "int", nullable: true),
                    BIT_IS_ACTIVE = table.Column<bool>(type: "bit", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_MENU", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_REQUEST_TYPE",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NVCHR_REQUEST_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_REQUEST_TYPE", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_ROLE",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NVCHR_ROLE_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_DESCRIPTION = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BIT_IS_ACTIVE = table.Column<bool>(type: "bit", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_ROLE", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_TAXI",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CHR_LICENSE_PLATE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_CATEGORY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_BRAND = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INT_CAPACITY = table.Column<int>(type: "int", nullable: true),
                    INT_DRIVER = table.Column<int>(type: "int", nullable: true),
                    NVCHR_DRIVER = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BIT_IS_ACTIVE = table.Column<bool>(type: "bit", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_TAXI", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_TRIP_REQUEST",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CHR_EMPLOYEE_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_TRIP_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_TRIP_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DTM_PICKUP_GO = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DTM_PICKUP_RETURN = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NVCHR_FROM_LOCATION_GO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_TO_LOCATION_GO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_FROM_LOCATION_RETURN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_TO_LOCATION_RETURN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_PURPOSE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INT_PASSENGER_COUNT = table.Column<int>(type: "int", nullable: true),
                    CHR_QLSC_APPROVE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_QLTC_APPROVE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_GA_CONFIRM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INT_GROUP_BATCH_APPLICATIONS = table.Column<int>(type: "int", nullable: true),
                    CHR_QUOTE_PATH = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BIT_QUOTE_CONFIRM = table.Column<bool>(type: "bit", nullable: true),
                    NVCHR_DRIVER_INFO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INT_STATUS = table.Column<int>(type: "int", nullable: true),
                    CHR_STATUS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_TRIP_REQUEST", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_USER",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CHR_EMPLOYEE_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_ADID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_PASSWORD = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_SEC_CODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_SEC_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_POSITION = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INT_ROLE_ID = table.Column<int>(type: "int", nullable: true),
                    DTM_LAST_LOGIN = table.Column<DateTime>(type: "datetime2", nullable: true),
                    INT_LOCK = table.Column<int>(type: "int", nullable: true),
                    INT_LOCK_DAY = table.Column<int>(type: "int", nullable: true),
                    CHR_NOTE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_AVATAR_PATH = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BIT_IS_ACTIVE = table.Column<bool>(type: "bit", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_USER", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TM_VENDOR",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CHR_VENDOR_CODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_VENDOR_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_VENDOR_PHONE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_ADDRESS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_TAX_IDENTIFY_NUMBER = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_DIRECTOR_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_DIRECTOR_PHONE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NVCHR_PIC_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_PIC_PHONE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CHR_USER_CREATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DTM_CREATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CHR_USER_UPDATE = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DTM_UPDATE = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TM_VENDOR", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TM_APPROVAL_HISTORY");

            migrationBuilder.DropTable(
                name: "TM_BUS");

            migrationBuilder.DropTable(
                name: "TM_BUS_OT");

            migrationBuilder.DropTable(
                name: "TM_BUS_REQUEST");

            migrationBuilder.DropTable(
                name: "TM_BUS_ROUTE");

            migrationBuilder.DropTable(
                name: "TM_BUS_ROUTE_STOP");

            migrationBuilder.DropTable(
                name: "TM_BUS_STOP");

            migrationBuilder.DropTable(
                name: "TM_CARD");

            migrationBuilder.DropTable(
                name: "TM_CARD_BORROW");

            migrationBuilder.DropTable(
                name: "TM_CARD_USAGE");

            migrationBuilder.DropTable(
                name: "TM_DRIVER");

            migrationBuilder.DropTable(
                name: "TM_EMPLOYEE");

            migrationBuilder.DropTable(
                name: "TM_MASTER_MAIL");

            migrationBuilder.DropTable(
                name: "TM_MENU");

            migrationBuilder.DropTable(
                name: "TM_REQUEST_TYPE");

            migrationBuilder.DropTable(
                name: "TM_ROLE");

            migrationBuilder.DropTable(
                name: "TM_TAXI");

            migrationBuilder.DropTable(
                name: "TM_TRIP_REQUEST");

            migrationBuilder.DropTable(
                name: "TM_USER");

            migrationBuilder.DropTable(
                name: "TM_VENDOR");
        }
    }
}
