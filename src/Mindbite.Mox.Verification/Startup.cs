using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Mindbite.Mox.Verification
{
    public static class Startup
    {
        public static async Task VerifyAsync(IServiceProvider _serviceProvider)
        {
            async Task<IEnumerable<T>> AwaitAllSequentially<T>(IEnumerable<Task<T>> tasks)
            {
                var result = new List<T>();

                foreach(var task in tasks)
                {
                    result.Add(await task);
                }

                return result;
            }

            var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
            using (var scope = scopeFactory.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Services.IVerificator>>();
                var options = scope.ServiceProvider.GetRequiredService<IOptions<Services.VerificationOptions>>().Value;
                var results = await AwaitAllSequentially(options.Verificators.Select(x => x.VerifyAsync(scope.ServiceProvider)));

                if (results.All(x => x.Success))
                    return;

                using (logger.BeginScope("Validation failed!"))
                {
                    foreach (var result in results.Where(x => !x.Success))
                    {
                        using (logger.BeginScope(result.Verificator.Name))
                        {
                            foreach (var error in result.Errors)
                            {
                                logger.LogError("Code: {0}, Description: {1}", error.Code, error.Description);
                            }
                        }
                    }
                }

                throw new Exception(string.Join("\n", results.SelectMany(x => x.Errors.Select(y => $"Code: {y.Code}, Description: {y.Description}"))));
            }
        }
    }
}
