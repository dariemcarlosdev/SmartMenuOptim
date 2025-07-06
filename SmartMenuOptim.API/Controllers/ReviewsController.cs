using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.API.Services.Interfaces;
using SmartMenuOptim.Shared.Data;
using SmartMenuOptim.Shared.Data.Entities;
using SmartMenuOptim.Shared.Data.Interfaces;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SmartMenuOptim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly ILogger<ReviewsController> _logger;
        private readonly IUnityOfWork _unityOfWork;
        private readonly ISentimentService _sentimentService;

        public ReviewsController(ILogger<ReviewsController> logger, IUnityOfWork unityOfWork, ISentimentService sentimentService)
        {
            _sentimentService = sentimentService ?? throw new ArgumentNullException(nameof(sentimentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unityOfWork = unityOfWork ?? throw new ArgumentNullException(nameof(unityOfWork));
            if(_unityOfWork == null)
            {
                _logger.LogError("UnityOfWork is not initialized properly.");
                throw new InvalidOperationException("UnityOfWork is not initialized properly.");
            }
        }

        // GET: api/<ReviewsController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Review>>> GetAllReviews()
        {
            try
            {
                var reviews = await _unityOfWork.Reviews.GetAllAsync();
                return Ok(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching reviews.");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET api/<ReviewsController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Review>> GetReviewById(int id)
        {
            try
            {
                var review = await _unityOfWork.Reviews.GetByIdAsync(id);
                if (review == null)
                {
                    return NotFound();
                }
                return Ok(review);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the review.");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST api/<ReviewsController>
        /// <summary>
        /// Creates a new review.
        /// </summary>
        /// <param name="review"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<Review>> CreateReview([FromBody] Review review)
        {
            //all this checks are to ensure that the review object is valid before proceeding with the creation.
            if (review == null)
            {
                return BadRequest("Review cannot be null");
            }
            else if (string.IsNullOrWhiteSpace(review.CustomerName) || string.IsNullOrWhiteSpace(review.Comment))
            {
                return BadRequest("Customer name and comment cannot be empty");
            }

            //This check is commented out because the sentiment score is now being calculated by the sentiment analysis service.
            //else if (review.SentimentScore < 0 || review.SentimentScore > 1)
            //{
            //    return BadRequest("Sentiment score must be between 0 and 1");
            //}
            else
            {
                // Validate the review object
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
            }
            // If the review object is valid, we can proceed with the creation.

            try
            {
                if (_unityOfWork == null)
                {
                    return StatusCode(500, "Database context is not initialized.");
                }
                var sentimentScore = await _sentimentService.AnalyzeSentimentAsync(review.Comment); // call the sentiment analysis service and get the sentiment score for sentiment analysis of the review comment.
                review.SentimentScore = sentimentScore ?? 0.5; // Default to neutral sentiment if analysis fails

                await _unityOfWork.Reviews.AddAsync(review);
                await _unityOfWork.SaveChangesAsync();
                return CreatedAtAction(nameof(GetReviewById), new { id = review.Id }, review); // This will return the created review with its ID in the response.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the review.");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT api/<ReviewsController>/5
        [HttpPut("{id}")]
        public async Task<ActionResult<Review>> UpdateReview(int id, [FromBody] Review review)
        {
            if (review == null || review.Id != id)
            {
                return BadRequest("Review cannot be null and ID must match");
            }
            try
            {
               if(_unityOfWork == null)
                {
                    return StatusCode(500, "Database context is not initialized.");
                }

                var existingReview = await _unityOfWork.Reviews.GetByIdAsync(id);
                if (existingReview == null)
                {
                    return NotFound();
                }
                existingReview.CustomerName = review.CustomerName;
                existingReview.Comment = review.Comment;
                existingReview.SentimentScore = review.SentimentScore;
                
                _unityOfWork.Reviews.Update(existingReview);
                await _unityOfWork.SaveChangesAsync();
                return Ok(existingReview);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReviewExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.LogError(ex, "An error occurred while updating the review.");
                };
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE api/<ReviewsController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            if(_unityOfWork == null)
            {
                return StatusCode(500, "Database context is not initialized.");
            }   

            try
            {
                var review = await _unityOfWork.Reviews.GetByIdAsync(id);
                if (review == null)
                {
                    return NotFound();
                }
                _unityOfWork.Reviews.Delete(review);
                await _unityOfWork.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the review.");
                return StatusCode(500, "Internal server error");
            }
        }


        private bool ReviewExists(int id)
        {
           return _unityOfWork.Reviews.ExistsAsync(id).GetAwaiter().GetResult();
        }
    }
}
