using Fundo.Application.Interfaces;
using Fundo.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Fundo.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddLocalization();
        services.AddScoped<ILoanService, LoanService>();

        return services;
    }
}
