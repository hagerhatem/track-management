using TrackManagement.Api.Auth;
using TrackManagement.Api.Cors;
using TrackManagement.Api.Middleware;
using TrackManagement.Application;
using TrackManagement.Infrastructure;
using TrackManagement.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddFrontendCors(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create);

builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer<OpenApiBearerTransformer>());

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Track Management API v1"));

    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsExtensions.PolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
