using EquipmentBorrowing.Application.Abstractions;
using EquipmentBorrowing.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentBorrowing.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<DemoFaultSwitch>();
        services.AddSingleton<ITransactionFaultHook>(sp => sp.GetRequiredService<DemoFaultSwitch>());
        services.AddSingleton<StudentService>();
        services.AddSingleton<EquipmentService>();
        services.AddSingleton<BorrowingService>();
        return services;
    }
}
