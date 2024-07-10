using BlockChainServices.Interfaces;
using BlockChainServices.Services;
using Utility;
using BotRepository;
using ForwardTraderBot.Service;
using Services.Interfaces.V2;
using Services.V2;
using InfrastructureUtil;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureBuilder();
builder.Services.ConfigureServices(builder);
builder.Services.AddHostedService<ReverseTraderService>();


var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
