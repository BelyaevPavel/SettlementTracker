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
        builder.Logging.SetMinimumLevel(LogLevel.Debug);

        builder.Services.AddBlazorBootstrap();

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        var jsonDefinitionRepository = new JsonDefinitionRepository("Data");

        builder.Services.AddSingleton<IResourceDefinitionRepository>(jsonDefinitionRepository);
        builder.Services.AddSingleton<IBuildingDefinitionRepository>(jsonDefinitionRepository);

        builder.Services.AddSingleton<ICitizenService, CitizenService>();
        builder.Services.AddSingleton<ISettlementResourcesService, SettlementResourcesService>();
        builder.Services.AddSingleton<IBuildingService, BuildingService>();


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