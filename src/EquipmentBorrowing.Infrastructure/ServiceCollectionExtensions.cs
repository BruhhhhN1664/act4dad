using EquipmentBorrowing.Application.Abstractions;
using EquipmentBorrowing.Infrastructure.Persistence;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentBorrowing.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<EquipmentBorrowingDbContext>(o => o.UseNpgsql(connectionString));
        services.AddSingleton<IUnitOfWorkFactory, EfUnitOfWorkFactory>();
        services.AddSingleton<IDatabaseErrorClassifier, NpgsqlErrorClassifier>();
        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        return services;
    }
}
