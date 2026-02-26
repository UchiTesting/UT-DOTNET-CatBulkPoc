namespace CatBulk.Domain
{
    /// <summary>
    /// Represents a cat with identifying information, physical characteristics, and owner details.
    /// </summary>
    /// <remarks>The Cat class encapsulates basic descriptive data about a cat, including its name, age,
    /// gender, fur type, and eye color, as well as the first and last name of its owner.</remarks>
    public class Cat
    {
        public int CatId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerLastName { get; set; } = string.Empty;
        public string OwnerFirstName { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Fur { get; set; } = string.Empty;
        public string EyeColor { get; set; } = string.Empty;
    }
}