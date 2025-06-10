using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.API.Data;
using SmartMenuOptim.Shared.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SmartMenuOptim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SaleRecordsController : ControllerBase
    {
        private readonly ILogger<SaleRecordsController> _logger;
        private readonly AppDbContext _context;

        public SaleRecordsController(ILogger<SaleRecordsController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: api/<SaleRecordsController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SaleRecord>>> Get()
        {
            try
            {
                var saleRecords = await _context.SaleRecords.ToListAsync();
                return Ok(saleRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching sale records.");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET api/<SaleRecordsController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SaleRecord>> Get(int id)
        {
            try
            {
                var saleRecord = await _context.SaleRecords.FindAsync(id);
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

        // POST api/<SaleRecordsController>
        [HttpPost]
        public async Task<ActionResult<SaleRecord>> Post([FromBody] SaleRecord saleRecord)
        {
            if (saleRecord == null)
            {
                return BadRequest("Sale record cannot be null.");
            }
            try
            {
                _context.SaleRecords.Add(saleRecord);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(Get), new { id = saleRecord.Id }, saleRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the sale record.");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT api/<SaleRecordsController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] SaleRecord saleRecord)
        {
            if (id != saleRecord.Id)
            {
                return BadRequest("Sale record ID mismatch.");
            }
            try
            {
                _context.Entry(saleRecord).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
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


        // DELETE api/<SaleRecordsController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var saleRecord = await _context.SaleRecords.FindAsync(id);
                if (saleRecord == null)
                {
                    return NotFound();
                }
                _context.SaleRecords.Remove(saleRecord);
                await _context.SaveChangesAsync();
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
            return _context.SaleRecords.Any(e => e.Id == id);
        }
    }
}
