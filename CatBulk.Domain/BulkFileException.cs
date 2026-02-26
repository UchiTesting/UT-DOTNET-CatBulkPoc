using System;

namespace CatBulk.Domain
{
    public class BulkFileException : Exception { 
        public BulkFileException() : base("An error occurred while handling a bulk file.") { }
        public BulkFileException(string message) : base(message) { }
    }
}