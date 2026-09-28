using AKUNAeat.DAL;
using AKUNAeat.Interfaces;

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("Local");

builder.Services.AddControllersWithViews();

builder.Services.AddTransient<IAccountDAL>(ad => new AccountDAL(connectionString));
builder.Services.AddTransient<IRestaurantDAL>(rd => new RestaurantDAL(connectionString));
builder.Services.AddTransient<IMealDAL>(rd => new MealDAL(connectionString));
builder.Services.AddTransient<IOrderDAL>(od => new OrderDAL(connectionString));

builder.Services.AddHttpContextAccessor();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); 
    options.Cookie.HttpOnly = true; 
    options.Cookie.IsEssential = true; 
});


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


app.UseSession(); 

app.MapControllers(); 

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
