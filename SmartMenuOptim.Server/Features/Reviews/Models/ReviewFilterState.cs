
/// <summary>
/// Represents the filter criteria used to query and sort reviews. It handles sorting options, minimum rating thresholds, and search terms.
/// Class that cen be used to encapsulate the state of review filters.
/// </summary>
public class ReviewFilterState
{    
    public string SortBy { get; set; } = "date";
    public int MinRating { get; set; }
    public string SearchTerm { get; set; } = "";
}