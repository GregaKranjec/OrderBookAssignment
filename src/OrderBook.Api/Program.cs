using Microsoft.EntityFrameworkCore;
using OrderBook.Api.Exchanges.Bitstamp;
using OrderBook.Api.Hubs;
using OrderBook.Api.OrderBooks;
using OrderBook.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

string auditConnectionString = AuditDatabase.GetConnectionString(builder.Configuration, builder.Environment);
builder.Services.AddDbContext<AuditDbContext>(options => options.UseSqlite(auditConnectionString));
builder.Services.AddScoped<OrderBookAuditWriter>();

builder.Services.AddOptions<OrderBookPollingOptions>()
    .BindConfiguration("OrderBook")
    .ValidateDataAnnotations()
    .Validate(options => options.MaxRetryDelayMilliseconds >= options.PollingIntervalMilliseconds,
        "The maximum retry delay must be at least the polling interval.")
    .ValidateOnStart();

builder.Services.AddSingleton<OrderBookSnapshotStore>();
builder.Services.AddHostedService<OrderBookWorker>();

// bitstamp api
builder.Services.AddHttpClient<BitstampOrderBookClient>(client =>
{
    client.BaseAddress = new Uri("https://www.bitstamp.net/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

var app = builder.Build();

// Apply migrations before starting the worker or accepting requests
await AuditDatabase.InitializeAsync(app);

app.UseHttpsRedirection();
app.MapControllers();
app.MapHub<OrderBookHub>("/hubs/order-book");
app.Run();
