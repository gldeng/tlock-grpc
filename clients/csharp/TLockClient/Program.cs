using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Tlock;
using TLockClient;

var builder = WebApplication.CreateBuilder(args);

// ReSharper disable once TooManyChainedReferences
var grpcServerAddress = builder.Configuration.GetSection("TLock:Endpoint").Value ??
                        throw new ArgumentNullException("TLock:Endpoint");
// ReSharper disable once TooManyChainedReferences
var chainHash = builder.Configuration.GetSection("TLock:ChainHash").Value ??
                throw new ArgumentNullException("TLock:ChainHash");

var channel = GrpcChannel.ForAddress(grpcServerAddress);

var client = new TLockService.TLockServiceClient(channel);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapPost("/encrypt", async (EncryptDto request) =>
    {
        try
        {
            var grpcRequest = new EncryptRequest
            {
                ChainHash = chainHash,
                Data = ByteString.CopyFromUtf8(request.Text),
                DisclosureTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(1))
            };
            var grpcResponse = await client.EncryptAsync(grpcRequest);
            var response = new EncryptResponseDto { Encrypted = grpcResponse.EncryptedData.ToBase64() };
            return Results.Ok(response);
        }
        catch (RpcException ex)
        {
            // Log the exception or handle it as needed
            return Results.Problem(detail: ex.Status.Detail, statusCode: (int)ex.StatusCode);
        }
    })
    .WithName("Encrypt");

app.MapPost("/decrypt", async (DecryptDto request) =>
    {
        try
        {
            var grpcRequest = new DecryptRequest
            {
                ChainHash = chainHash,
                EncryptedData = ByteString.FromBase64(request.Encrypted)
            };
            var grpcResponse = await client.DecryptAsync(grpcRequest);
            var response = new DecryptResponseDto { Text = grpcResponse.Data.ToStringUtf8() };
            return Results.Ok(response);
        }
        catch (RpcException ex)
        {
            // Log the exception or handle it as needed
            return Results.Problem(detail: ex.Status.Detail, statusCode: (int)ex.StatusCode);
        }
    })
    .WithName("Decrypt");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();