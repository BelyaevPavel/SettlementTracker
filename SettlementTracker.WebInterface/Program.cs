using System.ComponentModel.Design;
using SettlementTracker.Core.Repositories;
using SettlementTracker.WebInterface.Components;
using SettlementTracker.WebInterface.Data.Services;

namespace SettlementTracker.WebInterface;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddBlazorBootstrap();

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddSingleton<ICitizenService, CitizenService>();
        builder.Services.AddSingleton<IBuildingService, BuildingService>();
        builder.Services.AddSingleton<ISettlementResourcesService, SettlementResourcesService>();
        builder.Services.AddSingleton<JsonDefinitionRepository>(new JsonDefinitionRepository("State"));


        var app = builder.Build();

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