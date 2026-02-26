using System;
using System.Collections.Generic;

namespace CatBulk.Domain
{
    /// <summary>
    /// Provides methods for generating collections of Cat objects with randomized attributes for testing or
    /// demonstration purposes.
    /// </summary>
    /// <remarks>This static class is intended to facilitate the creation of sample Cat data, such as for
    /// populating test datasets or demonstrating application features. All methods are thread-safe for independent
    /// invocations, but the generated data is not guaranteed to be unique across multiple calls.</remarks>
    public static class CatGenerator
    {
        public static IEnumerable<Cat> Generate(int count)
        {
            Random rnd = new Random();
            string[] furs = { "Short", "Long", "Fluffy","Tabby","Calico" };
            string[] eyes = { "Green", "Blue", "Amber" };
            string[] genders = { "M", "F" };

            for (int i = 1; i <= count; i++)
            {
                yield return new Cat
                {
                    CatId = i,
                    Name = "Cat_" + i,
                    OwnerLastName = "Last_" + i,
                    OwnerFirstName = "First_" + i,
                    Age = rnd.Next(1, 20),
                    Gender = genders[rnd.Next(2)],
                    Fur = furs[rnd.Next(3)],
                    EyeColor = eyes[rnd.Next(3)]
                };
            }
        }
    }
}