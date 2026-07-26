using System;
/// <summary>
/// The exception that is thrown when a requested entity or resource cannot be found.
/// </summary>
/// <remarks>Use this exception to indicate that an operation failed because the specified item does not exist.
/// This exception is typically used in scenarios where a lookup or retrieval operation does not yield a
/// result.</remarks>
public class NotFoundException : Exception
{
    public NotFoundException() { }

    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string message, Exception innerException) : base(message, innerException) { }
}
