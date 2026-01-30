using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using SmartMenuOptim.Domain.Repositories;
using SmartMenuOptim.Domain.Specifications.SaleRecordSpecifications;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SmartMenuOptim.API.Controllers.v1
{
    //For versioning, add [ApiVersion("1.0")] above [Route("api/[controller]")]
    //[ApiVersion(1)]
    //[ApiVersion(2)]
    //[ApiController]
    //[Route("api/v{v:apiVersion}/[controller]")]

    //[ApiVersion("1.0")]
    //[Route("api/v{version:apiVersion}/[controller]")]
    [Route("api/[controller]")]
    [ApiController]
    public class SaleRecordsController : ControllerBase
    {
        private readonly ILogger<SaleRecordsController> _logger;
        private readonly IUnityOfWork _unityOfWork;

        public SaleRecordsController(ILogger<SaleRecordsController> logger, IUnityOfWork unityOfWork)
        {
            _logger = logger;
            _unityOfWork = unityOfWork;
            if (_unityOfWork == null)
            {
                _logger.LogError("UnityOfWork is not initialized properly.");
                throw new InvalidOperationException("UnityOfWork is not initialized properly.");
            }
        }

        // Fallowing REST API conventions, the GET method is used to retrieve a collection of resources.
        // The convention is to return a 200 OK status code with the collection in the response body.

        // GET: api/<SaleRecordsController>
        
        [HttpGet]
        //[MapToApiVersion("1.0")] // Map this action to API version 1.0
        public async Task<ActionResult<IEnumerable<SaleRecordDTO>>> GetAllSaleRecords()
        {
            _logger.LogInformation("GetAllSaleRecords method called at: {time}", DateTime.UtcNow);
            try
            {
                // ✅ NEW: Use specification pattern for complex includes
                var spec = new SaleRecordWithDetailsSpecification();
                var saleRecords = await _unityOfWork.SaleRecords.FindAsync(spec);
                
                var saleRecordsDtos = saleRecords.Select(s => new SaleRecordDTO
                {
                    Id = s.Id,
                    SaleDate = s.SaleDate,
                    QuantitySold = s.QuantitySold,
                    DishId = s.DishId,
                    DishName = s.Dish?.Name,
                    Category = s.Dish?.Category?.Name ?? "Unknown",
                    Rating = s.Dish?.Reviews.Any() == true ? (int)s.Dish.Reviews.Average(r => r.Rating) : 0,
                    RestaurantName = s.Dish?.Restaurant?.Name ?? "Unknown",
                    DishPrice = s.Dish?.DishPrice ?? 0
                }).ToList();
                
                return Ok(saleRecordsDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching sale records.");
                return StatusCode(500, "Internal server error");
            }
        }

        // Fallowing REST API conventions, the GET method is used to retrieve a specific resource by its ID.
        // The convention is to return a 200 OK status code with the resource if it exists, or a 404 Not Found if it does not exist.

        // GET api/<SaleRecordsController>/5
        //[MapToApiVersion("1.0")] // Map this action to API version 1.0
        [HttpGet("{id}")]
        public async Task<ActionResult<SaleRecord>> GetSaleRecordById (int id)
        {
            try
            {
                var saleRecord = await _unityOfWork.SaleRecords.GetByIdAsync(id);
                if (saleRecord == null)
                {
                    return NotFound();
                }
                return Ok(saleRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the sale record.");
                return StatusCode(500, "Internal server error");
            }
        }

        // Fallowing REST API conventions, the POST method is used to create a new resource.
        // The convention is to return a 201 Created status code with the location of the newly created resource in the response headers.

        // POST api/<SaleRecordsController>
        //[MapToApiVersion("1.0")] // Map this action to API version 1.0
        [HttpPost]
        public async Task<ActionResult<SaleRecord>> CreateSaleRecord([FromBody] SaleRecord saleRecord)
        {
            if (saleRecord == null)
            {
                return BadRequest("Sale record cannot be null.");
            }
            try
            {
                await _unityOfWork.SaleRecords.AddAsync(saleRecord);
                await _unityOfWork.SaveChangesAsync();
                return CreatedAtAction(nameof(CreateSaleRecord), new { id = saleRecord.Id }, saleRecord); // This will return the created sale record with its ID in the response.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the sale record.");
                return StatusCode(500, "Internal server error");
            }
        }
        // Fallowing REST API conventions, the PUT method is used to update an existing resource by its ID.
        // The convention is to return a 200 OK status code with the updated resource if the update is successful, or a 404 Not Found if the resource does not exist.

        // PUT api/<SaleRecordsController>/5
        //[MapToApiVersion("1.0")] // Map this action to API version 1.0
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSaleRecord(int id, [FromBody] SaleRecord SaleRecord)
        {


            if (SaleRecord == null || SaleRecord.Id != id)
            {
                return BadRequest("Review cannot be null and ID must match");
            }

             try
            {
                if (_unityOfWork == null)
                {
                    return StatusCode(500, "Database context is not initialized.");
                }

                var existingSaleRecord = await _unityOfWork.SaleRecords.GetByIdAsync(id);
                if (existingSaleRecord == null)
                {
                    return NotFound();
                }

                // Update the properties of the sale record using domain methods
                existingSaleRecord.UpdateSaleDate(SaleRecord.SaleDate);
                existingSaleRecord.UpdateQuantity(SaleRecord.QuantitySold);
                existingSaleRecord.UpdateSaleAmount(SaleRecord.SaleAmount);

                _unityOfWork.SaleRecords.Update(existingSaleRecord);
                await _unityOfWork.SaveChangesAsync();
                return Ok(existingSaleRecord);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SaleRecordExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the sale record.");
                return StatusCode(500, "Internal server error");
            }
        }

        // Fallowing rest API conventions, the DELETE method is used to remove a resource by its ID.
        // the convention is to return a 204 No Content status code if the deletion is successful, or a 404 Not Found if the resource does not exist.

        // DELETE api/<SaleRecordsController>/5
        //[MapToApiVersion("1.0")] // Map this action to API version 1.0
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSaleRecord(int id)
        {
            try
            {
                var saleRecord = await _unityOfWork.SaleRecords.GetByIdAsync(id);
                if (saleRecord == null)
                {
                    return NotFound();
                }
                _unityOfWork.SaleRecords.Delete(saleRecord);
                await _unityOfWork.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the sale record.");
                return StatusCode(500, "Internal server error");
            }
        }


        private bool SaleRecordExists(int id)
        {
            return _unityOfWork.SaleRecords.ExistsAsync(id).GetAwaiter().GetResult();
        }
    }
}
