using BiatecIdentityHelper.BusinessController;
using BiatecIdentityHelper.Model;
using BiatecIdentityHelper.Repository.Files;
using Grpc.Net.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<BiatecIdentityHelper.Model.Config>(
    builder.Configuration.GetSection("BiatecIdentity"));
builder.Services.Configure<BiatecIdentityHelper.Model.ObjectStorage>(
    builder.Configuration.GetSection("ObjectStorage"));

builder.Services.AddSingleton<IdentityHelper>();

var objectStorage = new ObjectStorage();
builder.Configuration.GetSection("ObjectStorage").Bind(objectStorage);
if(objectStorage.Type == "AWS")
{
    builder.Services.AddSingleton<IFileStorage, S3ObjectStorage>();
}
else
{
    builder.Services.AddSingleton<IFileStorage, FilesystemStorage>();
}

var app = builder.Build();

using (var channel = GrpcChannel.ForAddress("http://localhost:50051"))
{
    // this tests the connection to the derec crypto service and fails to start the app if the connection does not exists. it also allows to create new app configuration if not defined
    var client = new DerecCrypto.DeRecCryptographyService.DeRecCryptographyServiceClient(channel);
    var encKeys = client.EncryptGenerateEncryptionKey(new DerecCrypto.EncryptGenerateEncryptionKeyRequest());

    app.Logger.LogInformation($"New enc keys: PK: {encKeys.PublicKey.ToBase64()} SK: {encKeys.PrivateKey.ToBase64()} ");
    var signKeys = client.SignGenerateSigningKey(new DerecCrypto.SignGenerateSigningKeyRequest());
    app.Logger.LogInformation($"New sign keys: PK: {signKeys.PublicKey.ToBase64()} SK: {signKeys.PrivateKey.ToBase64()} ");
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
