using System;

namespace CatBulk.Domain
{
    public class DataOutOfScopeException : Exception
    {
        public DataOutOfScopeException() : base("Data is out of scope.") { }

        public DataOutOfScopeException(string message) : base(message) { }
    }
}