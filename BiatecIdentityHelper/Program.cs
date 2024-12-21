using BiatecIdentityHelper.BusinessController;
using BiatecIdentityHelper.Model;
using BiatecIdentityHelper.Repository.Files;

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

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
