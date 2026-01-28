using Microsoft.AspNetCore.Mvc;

namespace SmartMenuOptim.API.Controllers.v1
{
    /// <summary>
    /// This controller is used to check the configuration settings of the application.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class ConfigCheckController : ControllerBase
    {
        private IConfiguration _configuration;
        public ConfigCheckController(IConfiguration configuration) => _configuration = configuration;

        [HttpGet]
        public IActionResult CheckConfig()
        {
            // Here you would implement the logic to check the configuration
            // For now, we return a simple OK response

            return Ok(new
            {
                Connection = _configuration.GetConnectionString("DefaultConnection"),
                AzureKey = _configuration["Azure:TextAnalytics:Key"]
            });

        }
    }
    } 
