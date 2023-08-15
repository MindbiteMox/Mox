using System;

namespace Mindbite.Mox.DirectoryListing
{
    public class DirectoryListingException : Exception
    {
        public DirectoryListingException() : base() { }
        public DirectoryListingException(string? message) : base(message) { }
        public DirectoryListingException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
