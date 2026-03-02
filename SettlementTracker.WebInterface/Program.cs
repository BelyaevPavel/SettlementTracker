using SettlementTracker.Core.Repositories;
using SettlementTracker.Core.Services;
using SettlementTracker.WebInterface.Components;

namespace SettlementTracker.WebInterface;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder? builder = WebApplication.CreateBuilder(args);

        // Настройка логирования
        builder.Logging.ClearProviders(); // очищаем стандартные (необязательно)
        builder.Logging.AddConsole(); // вывод в консоль
        builder.Logging.AddDebug(); // вывод в окно отладки

// Установка минимального уровня логирования для всего приложения
// TODO: Change to Error before pull-request
        builder.Logging.SetMinimumLevel(LogLevel.Trace);

        builder.Services.AddBlazorBootstrap();

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddSingleton<ICitizenService, CitizenService>();
        builder.Services.AddSingleton<IBuildingService, BuildingService>();
        builder.Services.AddSingleton<ISettlementResourcesService>(
            new SettlementResourcesService(new JsonDefinitionRepository("Data")));


        WebApplication? app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}