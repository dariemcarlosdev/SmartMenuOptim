using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Server.Features.Reviews.Models
{
    /// <summary>
    /// Represents the data model for a customer review submitted through the review form, including customer name, dish
    /// name, rating, and comment.
    /// </summary>
    /// <remarks>This model is typically used to capture and validate user input for restaurant or menu item
    /// reviews. All properties are required and subject to validation constraints to ensure completeness and data
    /// quality.</remarks>
    internal class ReviewFormModel
    {
        [Required(ErrorMessage = "Please enter your name.")]
        [StringLength(100, ErrorMessage = "Name is too long.")]
        public string CustomerName { get; set; } = "";

        [Required(ErrorMessage = "Please enter the dish name.")]
        public string DishName { get; set; } = "";

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; } = 5;

        [Required(ErrorMessage = "Please leave a comment.")]
        public string Comment { get; set; } = "";
    }
}
