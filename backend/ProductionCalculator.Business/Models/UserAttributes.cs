using System.Collections.Generic;

namespace ProductionCalculator.Business.Models
{
    // A simple alias for user-defined key/value attributes.
    // Values are stored as objects and serialized to jsonb in the database.
    public class UserAttributes : Dictionary<string, object?>
    {
        public UserAttributes() : base() { }
        public UserAttributes(IDictionary<string, object?> dictionary) : base(dictionary) { }
    }
}
