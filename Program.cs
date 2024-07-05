using BlockChainServices.Interfaces;
using BlockChainServices.Services;
using Utility;
using BotRepository;
using ForwardTraderBot.Service;
using Services.Interfaces.V2;
using Services.V2;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<ISolanaQuickNodeMainetApiService, SolanaQuickNodeMainetApiService>("SolanaMainNet", httpClient =>
{
    httpClient.BaseAddress = new Uri(builder.Configuration.GetValue<string>("AppSettings:SolanaRPCEndPoint"));
}).AddPolicyHandler(httpRequestMessage => BuilderUtil.GetTimeOutPolicy())
.AddPolicyHandler(BuilderUtil.GetRetryPolicy());

builder.Services.AddHttpClient<IJupiterService, JupiterService>("JupiterHttpClient", httpClient =>
{
    httpClient.BaseAddress = new Uri(builder.Configuration.GetValue<string>("AppSettings:JupiterEndPoint"));
}).AddPolicyHandler(httpRequestMessage => BuilderUtil.GetTimeOutPolicy())
.AddPolicyHandler(BuilderUtil.GetRetryPolicy());

builder.Services.AddHttpClient<IJupiterService, JupiterService>("TransactionClient", httpClient =>
{
    httpClient.BaseAddress = new Uri(builder.Configuration.GetValue<string>("AppSettings:TransactionEndPoint"));
}).AddPolicyHandler(httpRequestMessage => BuilderUtil.GetTimeOutPolicy())
.AddPolicyHandler(BuilderUtil.GetRetryPolicy());


builder.Services.AddControllers();


builder.Services.AddHostedService<ReverseTraderService>();
builder.Services.AddTransient<ICoinCheckServiceV2, CoinCheckServiceV2>();
builder.Services.AddTransient<ICoinRepo, CoinRepo>();
builder.Services.AddTransient<IBotInstanceRepo, BotInstanceRepo>();
builder.Services.AddTransient<IBirdEyeService, BirdEyeService>();
builder.Services.AddTransient<IRPCService, RPCService>();
builder.Services.AddTransient<ITransactionRepo, TransactionRepo>();
builder.Services.AddTransient<IJupiterService, JupiterService>();
builder.Services.AddTransient<IBlockChainApiService, BlockChainApiService>();
builder.Services.AddTransient<ITransactionService, TransactionService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
