using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subjects",
                columns: table => new
                {
                    subjectid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subjects", x => x.subjectid);
                });

            migrationBuilder.CreateTable(
                name: "tiers",
                columns: table => new
                {
                    tierid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rate = table.Column<int>(type: "integer", nullable: false),
                    currentstreak = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tiers", x => x.tierid);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    userid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isdeleted = table.Column<bool>(type: "boolean", nullable: false),
                    isactive = table.Column<bool>(type: "boolean", nullable: false),
                    emailverificationtoken = table.Column<string>(type: "text", nullable: true),
                    emailverificationtokenexpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    refreshtoken = table.Column<string>(type: "text", nullable: true),
                    refreshtokenexpirytime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.userid);
                });

            migrationBuilder.CreateTable(
                name: "conversations",
                columns: table => new
                {
                    conversationid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    lastmessageat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isactive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversations", x => x.conversationid);
                    table.ForeignKey(
                        name: "fk_conversations_users_userid",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parents",
                columns: table => new
                {
                    parentid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parents", x => x.parentid);
                    table.ForeignKey(
                        name: "fk_parents_users_userid",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tutors",
                columns: table => new
                {
                    tutorid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    bio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    rating = table.Column<double>(type: "double precision", nullable: false),
                    isverified = table.Column<bool>(type: "boolean", nullable: false),
                    tierid = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tutors", x => x.tutorid);
                    table.ForeignKey(
                        name: "fk_tutors_tiers_tierid",
                        column: x => x.tierid,
                        principalTable: "tiers",
                        principalColumn: "tierid",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tutors_users_userid",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "conversationusers",
                columns: table => new
                {
                    conversationid = table.Column<int>(type: "integer", nullable: false),
                    userid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversationusers", x => new { x.conversationid, x.userid });
                    table.ForeignKey(
                        name: "fk_conversationusers_conversations_conversationid",
                        column: x => x.conversationid,
                        principalTable: "conversations",
                        principalColumn: "conversationid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_conversationusers_users_userid",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    messageid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    conversationid = table.Column<int>(type: "integer", nullable: false),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    isread = table.Column<bool>(type: "boolean", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isdeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_messages", x => x.messageid);
                    table.ForeignKey(
                        name: "fk_messages_conversations_conversationid",
                        column: x => x.conversationid,
                        principalTable: "conversations",
                        principalColumn: "conversationid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_messages_users_userid",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    studentid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parentid = table.Column<int>(type: "integer", nullable: true),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    grade = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    school = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_students", x => x.studentid);
                    table.ForeignKey(
                        name: "fk_students_parents_parentid",
                        column: x => x.parentid,
                        principalTable: "parents",
                        principalColumn: "parentid",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_students_users_userid",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "availabilities",
                columns: table => new
                {
                    availabilityid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tutorid = table.Column<int>(type: "integer", nullable: false),
                    subjectid = table.Column<int>(type: "integer", nullable: true),
                    dayofweek = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    starttime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    endtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    startcoursetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    endcoursetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    slot = table.Column<int>(type: "integer", nullable: false),
                    priceperslot = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_availabilities", x => x.availabilityid);
                    table.ForeignKey(
                        name: "fk_availabilities_subjects_subjectid",
                        column: x => x.subjectid,
                        principalTable: "subjects",
                        principalColumn: "subjectid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_availabilities_tutors_tutorid",
                        column: x => x.tutorid,
                        principalTable: "tutors",
                        principalColumn: "tutorid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "favoritetutors",
                columns: table => new
                {
                    favoriteid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tutorid = table.Column<int>(type: "integer", nullable: false),
                    parentid = table.Column<int>(type: "integer", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_favoritetutors", x => x.favoriteid);
                    table.ForeignKey(
                        name: "fk_favoritetutors_parents_parentid",
                        column: x => x.parentid,
                        principalTable: "parents",
                        principalColumn: "parentid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_favoritetutors_tutors_tutorid",
                        column: x => x.tutorid,
                        principalTable: "tutors",
                        principalColumn: "tutorid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tutorbankaccounts",
                columns: table => new
                {
                    tutorbankaccountid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tutorid = table.Column<int>(type: "integer", nullable: false),
                    bankname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    accountnumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    accountholdername = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tutorbankaccounts", x => x.tutorbankaccountid);
                    table.ForeignKey(
                        name: "fk_tutorbankaccounts_tutors_tutorid",
                        column: x => x.tutorid,
                        principalTable: "tutors",
                        principalColumn: "tutorid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tutorsubjects",
                columns: table => new
                {
                    subjectid = table.Column<int>(type: "integer", nullable: false),
                    tutorid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tutorsubjects", x => new { x.subjectid, x.tutorid });
                    table.ForeignKey(
                        name: "fk_tutorsubjects_subjects_subjectid",
                        column: x => x.subjectid,
                        principalTable: "subjects",
                        principalColumn: "subjectid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tutorsubjects_tutors_tutorid",
                        column: x => x.tutorid,
                        principalTable: "tutors",
                        principalColumn: "tutorid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wallets",
                columns: table => new
                {
                    walletid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tutorid = table.Column<int>(type: "integer", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    pendingbalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallets", x => x.walletid);
                    table.ForeignKey(
                        name: "fk_wallets_tutors_tutorid",
                        column: x => x.tutorid,
                        principalTable: "tutors",
                        principalColumn: "tutorid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    bookingid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    availabilityid = table.Column<int>(type: "integer", nullable: false),
                    parentid = table.Column<int>(type: "integer", nullable: false),
                    studentid = table.Column<int>(type: "integer", nullable: false),
                    priceatbooking = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    isdeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookings", x => x.bookingid);
                    table.ForeignKey(
                        name: "fk_bookings_availabilities_availabilityid",
                        column: x => x.availabilityid,
                        principalTable: "availabilities",
                        principalColumn: "availabilityid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookings_parents_parentid",
                        column: x => x.parentid,
                        principalTable: "parents",
                        principalColumn: "parentid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookings_students_studentid",
                        column: x => x.studentid,
                        principalTable: "students",
                        principalColumn: "studentid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "materials",
                columns: table => new
                {
                    materialid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    availabilityid = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    fileurl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_materials", x => x.materialid);
                    table.ForeignKey(
                        name: "fk_materials_availabilities_availabilityid",
                        column: x => x.availabilityid,
                        principalTable: "availabilities",
                        principalColumn: "availabilityid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wallettransactions",
                columns: table => new
                {
                    wallettransactionid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    walletid = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallettransactions", x => x.wallettransactionid);
                    table.ForeignKey(
                        name: "fk_wallettransactions_wallets_walletid",
                        column: x => x.walletid,
                        principalTable: "wallets",
                        principalColumn: "walletid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "homeworks",
                columns: table => new
                {
                    homeworkid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bookingid = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    duedate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    uploadedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_homeworks", x => x.homeworkid);
                    table.ForeignKey(
                        name: "fk_homeworks_bookings_bookingid",
                        column: x => x.bookingid,
                        principalTable: "bookings",
                        principalColumn: "bookingid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lessons",
                columns: table => new
                {
                    lessonid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bookingid = table.Column<int>(type: "integer", nullable: false),
                    scheduletime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duration = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    meetinglink = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lessons", x => x.lessonid);
                    table.ForeignKey(
                        name: "fk_lessons_bookings_bookingid",
                        column: x => x.bookingid,
                        principalTable: "bookings",
                        principalColumn: "bookingid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    paymentid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bookingid = table.Column<int>(type: "integer", nullable: false),
                    totalprice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.paymentid);
                    table.ForeignKey(
                        name: "fk_payments_bookings_bookingid",
                        column: x => x.bookingid,
                        principalTable: "bookings",
                        principalColumn: "bookingid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    reviewid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tutorid = table.Column<int>(type: "integer", nullable: false),
                    bookingid = table.Column<int>(type: "integer", nullable: false),
                    parentid = table.Column<int>(type: "integer", nullable: true),
                    rating = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    uploadedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviews", x => x.reviewid);
                    table.ForeignKey(
                        name: "fk_reviews_bookings_bookingid",
                        column: x => x.bookingid,
                        principalTable: "bookings",
                        principalColumn: "bookingid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reviews_parents_parentid",
                        column: x => x.parentid,
                        principalTable: "parents",
                        principalColumn: "parentid",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_reviews_tutors_tutorid",
                        column: x => x.tutorid,
                        principalTable: "tutors",
                        principalColumn: "tutorid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payouts",
                columns: table => new
                {
                    payoutid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tutorid = table.Column<int>(type: "integer", nullable: false),
                    wallettransactionid = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    createdat_decimal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    paidat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payouts", x => x.payoutid);
                    table.ForeignKey(
                        name: "fk_payouts_tutors_tutorid",
                        column: x => x.tutorid,
                        principalTable: "tutors",
                        principalColumn: "tutorid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payouts_wallettransactions_wallettransactionid",
                        column: x => x.wallettransactionid,
                        principalTable: "wallettransactions",
                        principalColumn: "wallettransactionid",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "essays",
                columns: table => new
                {
                    essayid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    homeworkid = table.Column<int>(type: "integer", nullable: false),
                    questiontext = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    points = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_essays", x => x.essayid);
                    table.ForeignKey(
                        name: "fk_essays_homeworks_homeworkid",
                        column: x => x.homeworkid,
                        principalTable: "homeworks",
                        principalColumn: "homeworkid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "multiplechoicequestions",
                columns: table => new
                {
                    multiplechoicequestionid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    homeworkid = table.Column<int>(type: "integer", nullable: false),
                    questiontext = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    point = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_multiplechoicequestions", x => x.multiplechoicequestionid);
                    table.ForeignKey(
                        name: "fk_multiplechoicequestions_homeworks_homeworkid",
                        column: x => x.homeworkid,
                        principalTable: "homeworks",
                        principalColumn: "homeworkid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "submissions",
                columns: table => new
                {
                    submissionid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    homeworkid = table.Column<int>(type: "integer", nullable: false),
                    studentid = table.Column<int>(type: "integer", nullable: false),
                    submittedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    totalscore = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_submissions", x => x.submissionid);
                    table.ForeignKey(
                        name: "fk_submissions_homeworks_homeworkid",
                        column: x => x.homeworkid,
                        principalTable: "homeworks",
                        principalColumn: "homeworkid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_submissions_students_studentid",
                        column: x => x.studentid,
                        principalTable: "students",
                        principalColumn: "studentid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attendances",
                columns: table => new
                {
                    attendanceid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lessonid = table.Column<int>(type: "integer", nullable: false),
                    studentid = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    attendedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendances", x => x.attendanceid);
                    table.ForeignKey(
                        name: "fk_attendances_lessons_lessonid",
                        column: x => x.lessonid,
                        principalTable: "lessons",
                        principalColumn: "lessonid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_attendances_students_studentid",
                        column: x => x.studentid,
                        principalTable: "students",
                        principalColumn: "studentid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "progressreports",
                columns: table => new
                {
                    reportid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lessonid = table.Column<int>(type: "integer", nullable: false),
                    tutorid = table.Column<int>(type: "integer", nullable: false),
                    studentid = table.Column<int>(type: "integer", nullable: false),
                    comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    createdat = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_progressreports", x => x.reportid);
                    table.ForeignKey(
                        name: "fk_progressreports_lessons_lessonid",
                        column: x => x.lessonid,
                        principalTable: "lessons",
                        principalColumn: "lessonid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_progressreports_students_studentid",
                        column: x => x.studentid,
                        principalTable: "students",
                        principalColumn: "studentid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_progressreports_tutors_tutorid",
                        column: x => x.tutorid,
                        principalTable: "tutors",
                        principalColumn: "tutorid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "questionoptionss",
                columns: table => new
                {
                    questionoptionid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    multiplechoicequestionid = table.Column<int>(type: "integer", nullable: false),
                    iscorrect = table.Column<bool>(type: "boolean", nullable: false),
                    content = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_questionoptionss", x => x.questionoptionid);
                    table.ForeignKey(
                        name: "fk_questionoptionss_multiplechoicequestions_multiplechoiceques~",
                        column: x => x.multiplechoicequestionid,
                        principalTable: "multiplechoicequestions",
                        principalColumn: "multiplechoicequestionid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "essayanswers",
                columns: table => new
                {
                    essayanswerid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    submissionid = table.Column<int>(type: "integer", nullable: false),
                    essayid = table.Column<int>(type: "integer", nullable: false),
                    answertext = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    score = table.Column<double>(type: "double precision", nullable: false),
                    feedback = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_essayanswers", x => x.essayanswerid);
                    table.ForeignKey(
                        name: "fk_essayanswers_essays_essayid",
                        column: x => x.essayid,
                        principalTable: "essays",
                        principalColumn: "essayid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_essayanswers_submissions_submissionid",
                        column: x => x.submissionid,
                        principalTable: "submissions",
                        principalColumn: "submissionid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "multiplechoicequestionanswers",
                columns: table => new
                {
                    multiplechoicequestionanswerid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    questionoptionid = table.Column<int>(type: "integer", nullable: false),
                    submissionid = table.Column<int>(type: "integer", nullable: false),
                    selectedoption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    multiplechoicequestionid = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_multiplechoicequestionanswers", x => x.multiplechoicequestionanswerid);
                    table.ForeignKey(
                        name: "fk_multiplechoicequestionanswers_multiplechoicequestions_multi~",
                        column: x => x.multiplechoicequestionid,
                        principalTable: "multiplechoicequestions",
                        principalColumn: "multiplechoicequestionid");
                    table.ForeignKey(
                        name: "fk_multiplechoicequestionanswers_questionoptionss_questionopti~",
                        column: x => x.questionoptionid,
                        principalTable: "questionoptionss",
                        principalColumn: "questionoptionid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_multiplechoicequestionanswers_submissions_submissionid",
                        column: x => x.submissionid,
                        principalTable: "submissions",
                        principalColumn: "submissionid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attendances_lessonid_studentid",
                table: "attendances",
                columns: new[] { "lessonid", "studentid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_attendances_studentid",
                table: "attendances",
                column: "studentid");

            migrationBuilder.CreateIndex(
                name: "ix_availabilities_subjectid",
                table: "availabilities",
                column: "subjectid");

            migrationBuilder.CreateIndex(
                name: "ix_availabilities_tutorid_dayofweek",
                table: "availabilities",
                columns: new[] { "tutorid", "dayofweek" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_availabilityid_studentid_status",
                table: "bookings",
                columns: new[] { "availabilityid", "studentid", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_parentid",
                table: "bookings",
                column: "parentid");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_studentid",
                table: "bookings",
                column: "studentid");

            migrationBuilder.CreateIndex(
                name: "ix_conversations_userid",
                table: "conversations",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "ix_conversationusers_userid",
                table: "conversationusers",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "ix_essayanswers_essayid",
                table: "essayanswers",
                column: "essayid");

            migrationBuilder.CreateIndex(
                name: "ix_essayanswers_submissionid",
                table: "essayanswers",
                column: "submissionid");

            migrationBuilder.CreateIndex(
                name: "ix_essays_homeworkid",
                table: "essays",
                column: "homeworkid");

            migrationBuilder.CreateIndex(
                name: "ix_favoritetutors_parentid_tutorid",
                table: "favoritetutors",
                columns: new[] { "parentid", "tutorid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_favoritetutors_tutorid",
                table: "favoritetutors",
                column: "tutorid");

            migrationBuilder.CreateIndex(
                name: "ix_homeworks_bookingid",
                table: "homeworks",
                column: "bookingid");

            migrationBuilder.CreateIndex(
                name: "ix_lessons_bookingid",
                table: "lessons",
                column: "bookingid");

            migrationBuilder.CreateIndex(
                name: "ix_materials_availabilityid",
                table: "materials",
                column: "availabilityid");

            migrationBuilder.CreateIndex(
                name: "ix_messages_conversationid",
                table: "messages",
                column: "conversationid");

            migrationBuilder.CreateIndex(
                name: "ix_messages_userid",
                table: "messages",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "ix_multiplechoicequestionanswers_multiplechoicequestionid",
                table: "multiplechoicequestionanswers",
                column: "multiplechoicequestionid");

            migrationBuilder.CreateIndex(
                name: "ix_multiplechoicequestionanswers_questionoptionid",
                table: "multiplechoicequestionanswers",
                column: "questionoptionid");

            migrationBuilder.CreateIndex(
                name: "ix_multiplechoicequestionanswers_submissionid",
                table: "multiplechoicequestionanswers",
                column: "submissionid");

            migrationBuilder.CreateIndex(
                name: "ix_multiplechoicequestions_homeworkid",
                table: "multiplechoicequestions",
                column: "homeworkid");

            migrationBuilder.CreateIndex(
                name: "ix_parents_userid",
                table: "parents",
                column: "userid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_bookingid",
                table: "payments",
                column: "bookingid");

            migrationBuilder.CreateIndex(
                name: "ix_payouts_tutorid",
                table: "payouts",
                column: "tutorid");

            migrationBuilder.CreateIndex(
                name: "ix_payouts_wallettransactionid",
                table: "payouts",
                column: "wallettransactionid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_progressreports_lessonid",
                table: "progressreports",
                column: "lessonid");

            migrationBuilder.CreateIndex(
                name: "ix_progressreports_studentid",
                table: "progressreports",
                column: "studentid");

            migrationBuilder.CreateIndex(
                name: "ix_progressreports_tutorid",
                table: "progressreports",
                column: "tutorid");

            migrationBuilder.CreateIndex(
                name: "ix_questionoptionss_multiplechoicequestionid",
                table: "questionoptionss",
                column: "multiplechoicequestionid");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_bookingid",
                table: "reviews",
                column: "bookingid");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_parentid",
                table: "reviews",
                column: "parentid");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_tutorid",
                table: "reviews",
                column: "tutorid");

            migrationBuilder.CreateIndex(
                name: "ix_students_parentid",
                table: "students",
                column: "parentid");

            migrationBuilder.CreateIndex(
                name: "ix_students_userid",
                table: "students",
                column: "userid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_submissions_homeworkid_studentid",
                table: "submissions",
                columns: new[] { "homeworkid", "studentid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_submissions_studentid",
                table: "submissions",
                column: "studentid");

            migrationBuilder.CreateIndex(
                name: "ix_tutorbankaccounts_tutorid",
                table: "tutorbankaccounts",
                column: "tutorid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tutors_tierid",
                table: "tutors",
                column: "tierid");

            migrationBuilder.CreateIndex(
                name: "ix_tutors_userid",
                table: "tutors",
                column: "userid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tutorsubjects_tutorid",
                table: "tutorsubjects",
                column: "tutorid");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wallets_tutorid",
                table: "wallets",
                column: "tutorid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wallettransactions_walletid",
                table: "wallettransactions",
                column: "walletid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendances");

            migrationBuilder.DropTable(
                name: "conversationusers");

            migrationBuilder.DropTable(
                name: "essayanswers");

            migrationBuilder.DropTable(
                name: "favoritetutors");

            migrationBuilder.DropTable(
                name: "materials");

            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "multiplechoicequestionanswers");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "payouts");

            migrationBuilder.DropTable(
                name: "progressreports");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "tutorbankaccounts");

            migrationBuilder.DropTable(
                name: "tutorsubjects");

            migrationBuilder.DropTable(
                name: "essays");

            migrationBuilder.DropTable(
                name: "conversations");

            migrationBuilder.DropTable(
                name: "questionoptionss");

            migrationBuilder.DropTable(
                name: "submissions");

            migrationBuilder.DropTable(
                name: "wallettransactions");

            migrationBuilder.DropTable(
                name: "lessons");

            migrationBuilder.DropTable(
                name: "multiplechoicequestions");

            migrationBuilder.DropTable(
                name: "wallets");

            migrationBuilder.DropTable(
                name: "homeworks");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "availabilities");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropTable(
                name: "subjects");

            migrationBuilder.DropTable(
                name: "tutors");

            migrationBuilder.DropTable(
                name: "parents");

            migrationBuilder.DropTable(
                name: "tiers");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
