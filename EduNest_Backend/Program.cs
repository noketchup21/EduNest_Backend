using System;
using System.Text;
using BusinessLayer.IServices;
using BusinessLayer.Mappings;
using BusinessLayer.Services;
using BusinessLayer.Settings;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using DataAccessLayer.Repositories;
using EduNest_Backend.BackgroundServices;
using EduNest_Backend.Middleware.RateLimit;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<EduNestDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
});

builder.Services.AddMapster();
MapsterConfig.Configure();
builder.Services.AddRateLimiting(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
                    ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Register email settings
builder.Services.Configure<EmailSetting>(
    builder.Configuration.GetSection(EmailSetting.SectionName));

builder.Services.Configure<PayOSSetting>(
    builder.Configuration.GetSection(PayOSSetting.SectionName));

builder.Services.Configure<PayOSChiSetting>(
    builder.Configuration.GetSection(PayOSChiSetting.SectionName));

builder.Services.Configure<CloudinarySetting>(
    builder.Configuration.GetSection("Cloudinary"));

#region ADDSCOPE
//Repo
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IParentRepository, ParentRepository>();
builder.Services.AddScoped<ITutorRepository, TutorRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<ITutorSubjectRepository, TutorSubjectRepository>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ILessonRepository, LessonRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IAvailabilityRepository, AvailabilityRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();
builder.Services.AddScoped<IPayoutRepository, PayoutRepository>();

builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IConversationUserRepository, ConversationUserRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ITutorReportRepository, TutorReportRepository>();
builder.Services.AddScoped<IAdminTutorRepository, AdminTutorRepository>();
//Service
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IParentService, ParentService>();
builder.Services.AddScoped<ITutorService, TutorService>();
builder.Services.AddScoped<ITutorSubjectService, TutorSubjectService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ILessonService, LessonService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IPayoutService, PayoutService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IMeetingLinkService, GoogleMeetLinkService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<ITutorEngagementService, TutorEngagementService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ITutorReportRepository, TutorReportRepository>();
builder.Services.AddScoped<IAdminTutorRepository, AdminTutorRepository>();
builder.Services.AddScoped<ISupportReportService, SupportReportService>();
builder.Services.AddScoped<IPayOSChiPayoutService, PayOSChiPayoutService>();
#endregion

builder.Services.AddHostedService<BookingExpiryBackgroundService>();

builder.Services.AddHttpClient();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EduNest API",
        Version = "v1",
        Description = "EduNest Backend API"
    });

    // ── Step 1: Define the security scheme ───────────────────────────────
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your token here}\n\nExample: Bearer eyJhbGci..."
    });

    // ── Step 2: Apply it globally to all endpoints ────────────────────────
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Render sets PORT environment variable
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://+:{port}");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiting();

app.MapControllers();

app.Run();
