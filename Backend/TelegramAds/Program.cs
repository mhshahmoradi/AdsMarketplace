using System.Text;
using Carter;
using Coravel;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Telegram.Bot;
using Telegram.Bot.AspNetCore;
using Telegram.Bot.Types.Enums;
using TelegramAds.Features.Bot.Actions;
using TelegramAds.Features.Bot.Chat;
using TelegramAds.Features.Bot.Notifications;
using TelegramAds.Shared.Auth;
using TelegramAds.Shared.Db;
using TelegramAds.Shared.Errors;
using TelegramAds.Shared.Telegram;
using TelegramAds.Shared.Time;
using TelegramAds.Shared.Ton;
using TelegramAds.Workers;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = false;
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

var telegramOptions = builder.Configuration.GetSection(TelegramOptions.SectionName).Get<TelegramOptions>()!;
builder.Services.AddSingleton(telegramOptions);
builder.Services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(telegramOptions.BotToken));
builder.Services.AddSingleton(_ => new WTelegramService(telegramOptions, builder.Configuration));

var tonOptions = builder.Configuration.GetSection(TonOptions.SectionName).Get<TonOptions>()!;
builder.Services.AddSingleton(tonOptions);
builder.Services.AddSingleton<TonPaymentService>();
builder.Services.AddSingleton<TonWalletService>();
builder.Services.AddHttpClient<TonApiClient>();


var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton(_ => new TelegramInitDataValidator(
    TimeSpan.FromHours(1),
    builder.Configuration
));

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<CurrentUser>(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var currentUser = new CurrentUser();
    currentUser.SetFromClaimsPrincipal(httpContextAccessor.HttpContext?.User);
    return currentUser;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CallbackActionRouter>();
builder.Services.AddSingleton<ChatSessionStore>();
builder.Services.AddSingleton<CreativeSessionStore>();
builder.Services.AddScoped<ChatHandler>();
builder.Services.AddScoped<CreativeFlowHandler>();
builder.Services.AddScoped<BotNotificationService>();
builder.Services.AddScoped<BotMessageDispatcher>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var botConfigSection = builder.Configuration.GetSection("BotConfiguration");
var telegramBotClientOptions = new TelegramBotClientOptions(token: botConfigSection.Get<BotConfiguration>()!.BotToken);
builder.Services.Configure<BotConfiguration>(botConfigSection);
builder.Services.AddHttpClient("tgwebhook")
.RemoveAllLoggers()
.ConfigureHttpClient(_ => { })
.AddTypedClient<ITelegramBotClient>(
    httpClient => new TelegramBotClient(telegramBotClientOptions));

builder.Services.AddCarter();
builder.Services.AddScheduler();

builder.Services.AddTransient<OutboxDispatcherWorker>();
builder.Services.AddTransient<DealExpiryWorker>();
builder.Services.AddTransient<PostSchedulerWorker>();
builder.Services.AddTransient<PostVerifierWorker>();
builder.Services.AddTransient<PaymentVerificationWorker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
});

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{

    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if ((await db.Database.GetPendingMigrationsAsync()).Any())
    {
        await db.Database.MigrateAsync();
    }
}

app.Lifetime.ApplicationStarted.Register(async () =>
{
    var scope = app.Services.CreateScope();
    var bot = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
    var config = scope.ServiceProvider.GetRequiredService<IOptions<BotConfiguration>>();

    await bot.DeleteWebhook();
    var webhookUrl = config.Value.BotWebhookUrl.AbsoluteUri;

    await bot.SetWebhook(webhookUrl, allowedUpdates: [UpdateType.Message, UpdateType.CallbackQuery, UpdateType.PreCheckoutQuery], secretToken: config.Value.SecretToken);
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapCarter();

app.Services.UseScheduler(scheduler =>
{
    scheduler.Schedule<OutboxDispatcherWorker>().EveryFiveSeconds();
    scheduler.Schedule<DealExpiryWorker>().EveryMinute();
    scheduler.Schedule<PostSchedulerWorker>().EveryThirtySeconds();
    scheduler.Schedule<PostVerifierWorker>().EveryMinute();
    scheduler.Schedule<PaymentVerificationWorker>().EverySecond();
});

await app.RunAsync();

