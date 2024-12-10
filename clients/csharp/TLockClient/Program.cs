using System.ComponentModel.DataAnnotations;
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
                Force = true
            };
            if (request.Round > 0)
            {
                grpcRequest.RoundNumber = request.Round;
            }
            else if (!string.IsNullOrWhiteSpace(request.Duration))
            {
                var start = DateTime.UtcNow;
                var totalDuration = DurationParser.ParseDurationsAsSeconds(start, request.Duration);
                var decryptionTime = start.Add(totalDuration);
                grpcRequest.DisclosureTime = Timestamp.FromDateTime(decryptionTime);
            }
            else
            {
                throw new ValidationException("you must provide either duration or a round to encrypt");
            }

            var grpcResponse = await client.EncryptAsync(grpcRequest);
            var encrypted = grpcResponse.EncryptedData.ToBase64();
            // ReSharper disable once ComplexConditionExpression
            var chopIntoChunksOf64 = (string value) => string.Join("\n",
                Enumerable.Range(0, value.Length / 64 + (value.Length % 64 == 0 ? 0 : 1))
                    .Select(i => value.Substring(i * 64, Math.Min(64, value.Length - i * 64))));
            var chunkedEncrypted = chopIntoChunksOf64(encrypted);
            var response = new EncryptResponseDto
            {
                Encrypted = encrypted,
                Pem = $"-----BEGIN AGE ENCRYPTED FILE-----\n{chunkedEncrypted}\n-----END AGE ENCRYPTED FILE-----"
            };
            return Results.Ok(response);
        }
        catch (DurationParseError ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: -2); 
        }
        catch (ValidationException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: -1);
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