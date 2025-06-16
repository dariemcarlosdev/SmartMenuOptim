using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Shared.Data;
using SmartMenuOptim.Shared.Data.Entities;
using SmartMenuOptim.Shared.Data.Interfaces;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SmartMenuOptim.API.Controllers
{
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
        }

        // Fallowing REST API conventions, the GET method is used to retrieve a collection of resources.
        // The convention is to return a 200 OK status code with the collection in the response body.

        // GET: api/<SaleRecordsController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SaleRecord>>> GetAllSaleRecords ()
        {
            try
            {
                var saleRecords = await _unityOfWork.SaleRecords.GetAllAsync();
                return Ok(saleRecords);
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

                // Update the properties of the sale record
                existingSaleRecord.SaleDate = SaleRecord.SaleDate;
                existingSaleRecord.QuantitySold = SaleRecord.QuantitySold;
                existingSaleRecord.DishName = SaleRecord.DishName;

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
            return _unityOfWork.Reviews.ExistsAsync(id).GetAwaiter().GetResult();
        }
    }
}
