using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMenuOptim.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Background job for optimizing menus. Job implementation details to be added.
    /// </summary>
    internal class MenuOptimizationJob : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
    }
}
