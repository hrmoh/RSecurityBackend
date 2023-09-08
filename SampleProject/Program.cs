using Audit.WebApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RSecurityBackend.Authorization;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Mail;
using RSecurityBackend.Services;
using RSecurityBackend.Services.Implementation;
using RSecurityBackend.Utilities;
using SampleProject.DbContext;
using SampleProject.Models.Auth.Memory;
using SampleProject.Services.Implementation;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();


// Add service and create Policy with options
builder.Services.AddCors(options =>
{
    options.AddPolicy("RServiceCorsPolicy",
        builder => builder.SetIsOriginAllowed(_ => true)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .WithExposedHeaders("paging-headers")
        .AllowCredentials()
        );
});

builder.Services.AddDbContextPool<RDbContext>(
                        options => options.UseSqlServer(
                            builder.Configuration.GetConnectionString("DefaultConnection"),
                            providerOptions =>
                            {
                                providerOptions.EnableRetryOnFailure();
                                providerOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                            }
                            )
                        );

Audit.Core.Configuration.DataProvider = new RAuditDataProvider(builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddIdentityCore<RAppUser>(
                options =>
                {
                    // Password settings.
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequiredLength = 6;
                    options.Password.RequiredUniqueChars = 1;

                    // Lockout settings.
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;

                    // User settings.
                    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                    options.User.RequireUniqueEmail = false;
                }
                ).AddErrorDescriber<PersianIdentityErrorDescriber>();

new IdentityBuilder(typeof(RAppUser), typeof(RAppRole), builder.Services)
                .AddRoleManager<RoleManager<RAppRole>>()
                .AddSignInManager<SignInManager<RAppUser>>()
                .AddEntityFrameworkStores<RDbContext>()
                .AddErrorDescriber<PersianIdentityErrorDescriber>();
if (bool.Parse(builder.Configuration["AuditNetEnabled"] ?? false.ToString()))
{
    builder.Services.AddMvc(mvc =>
                   mvc.AddAuditFilter(config => config
                   .LogRequestIf(r => r.Method != "GET")
                   .WithEventType("{controller}/{action} ({verb})")
                   .IncludeHeaders(ctx => !ctx.ModelState.IsValid)
                   .IncludeRequestBody()
                   .IncludeModelState()
               ));
}

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "bearer";
}).AddJwtBearer("bearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = false,
        ValidAudience = "Everyone",
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration.GetSection("RSecurityBackend")["ApplicationName"],

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes($"{builder.Configuration.GetSection("Security")["Secret"]}")),

        ValidateLifetime = true, //validate the expiration and not before values in the token

        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Add("Token-Expired", "true");
            }
            return Task.CompletedTask;
        }
    };

});

builder.Services.AddAuthorization(options =>
{
    //this is the default policy to make sure the use session has not yet been deleted by him/her from another client
    //or by an admin (Authorize with no policy should fail on deleted sessions)
    var defPolicy = new AuthorizationPolicyBuilder();
    defPolicy.Requirements.Add(new UserGroupPermissionRequirement("null", "null"));
    options.DefaultPolicy = defPolicy.Build();


    foreach (SecurableItem Item in RSecurableItem.Items)
    {
        foreach (SecurableItemOperation Operation in Item.Operations)
        {
            options.AddPolicy($"{Item.ShortName}:{Operation.ShortName}", policy => policy.Requirements.Add(new UserGroupPermissionRequirement(Item.ShortName, Operation.ShortName)));
        }
    }
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = $"{builder.Configuration.GetSection("RSecurityBackend")["ApplicationName"]} API",
        Version = "v1",
        Description = $"{builder.Configuration.GetSection("RSecurityBackend")["ApplicationName"]} API",
        TermsOfService = new Uri("https://github.com/hrmoh/RSecurityBackend"),
        Contact = new OpenApiContact
        {
            Name = builder.Configuration.GetSection("RSecurityBackend")["ApplicationName"],
            Email = "email@domain.com",
            Url = new Uri("https://github.com/hrmoh/RSecurityBackend")
        }
    }
    );

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "RSecurityBackend.xml"));

    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme()
    {
        Description = "format: \"bearer {token}\"",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey

    });


    c.OperationFilter<SecurityRequirementsOperationFilter>();
    c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>(); // Adds "(Auth)" to the summary so that you can see which endpoints have Authorization
                                                                  // or use the generic method, e.g. c.OperationFilter<AppendAuthorizeToSummaryOperationFilter<MyCustomAttribute>>();

});

//IHttpContextAccessor
builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();


//authorization handler
builder.Services.AddScoped<IAuthorizationHandler, UserGroupPermissionHandler>();


//security context maps to main db context
builder.Services.AddTransient<RSecurityDbContext<RAppUser, RAppRole, Guid>, RDbContext>();

//captcha service
builder.Services.AddTransient<ICaptchaService, CaptchaServiceEF>();


//generic image file service
builder.Services.AddTransient<IImageFileService, ImageFileServiceEF>();

//app user services
builder.Services.AddTransient<IAppUserService, AppUserService>();

//user groups services
builder.Services.AddTransient<IUserRoleService, RoleService>();

//audit service
builder.Services.AddTransient<IAuditLogService, AuditLogServiceEF>();

//user permission checker
builder.Services.AddTransient<IUserPermissionChecker, UserPermissionChecker>();

//workspace service
builder.Services.AddTransient<IWorkspaceService, WorkspaceService>();

//workspace role service
builder.Services.AddTransient<IWorkspaceRolesService, WorkspaceRolesService>();

//secret generator
builder.Services.AddTransient<ISecretGenerator, SecretGenerator>();

// email service
builder.Services.AddTransient<IEmailSender, MailKitEmailSender>();
builder.Services.Configure<SmptConfig>(builder.Configuration);

//messaging service
builder.Services.AddTransient<IRNotificationService, RNotificationService>();

//long running job service
builder.Services.AddTransient<ILongRunningJobProgressService, LongRunningJobProgressServiceEF>();

//generic options service
builder.Services.AddTransient<IRGenericOptionsService, RGenericOptionsServiceEF>();

//upload limit for IIS
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.Parse(builder.Configuration.GetSection("IIS")["UploadLimit"] ?? "52428800");
});

builder.Services.AddHostedService<QueuedHostedService>();
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "R Service API V1");
    c.RoutePrefix = string.Empty;
});

app.UseAuthentication();

// global policy - assign here or on each controller
app.UseCors("RServiceCorsPolicy");

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();
