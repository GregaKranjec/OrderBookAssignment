using OrderBook.Api.Exchanges.Bitstamp;
using OrderBook.Api.Hubs;
using OrderBook.Api.OrderBooks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSignalR();

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapHub<OrderBookHub>("/hubs/order-book");
app.Run();
