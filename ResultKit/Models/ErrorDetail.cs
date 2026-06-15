using System;
using System.Collections.Generic;
using System.Text;

namespace ResultKit.Models
{
    /// <summary>
    /// A structured error detail attached to a failed result.
    /// Carries a code, message, and optional field name for validation errors.
    /// </summary>
    public sealed class ErrorDetail
    {
        /// <summary>Error code, e.g. "USER_NOT_FOUND", "INVALID_EMAIL".</summary>
        public string Code { get; }

        /// <summary>Description of the error.</summary>
        public string Message { get; }

        /// <summary>
        /// Optional field or property name this error is associated with.
        /// Useful for surfacing validation errors on specific form fields.
        /// </summary>
        public string? Field { get; }

        /// <param name="code"> Error code.</param>
        /// <param name="message">Description.</param>
        /// <param name="field">Optional field name (for validation errors).</param>
        public ErrorDetail(string code, string message, string? field = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Error code must not be empty.", nameof(code));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Error message must not be empty.", nameof(message));

            Code = code;
            Message = message;
            Field = field;
        }


        /// <summary>Creates a general error with a code and message.</summary>
        public static ErrorDetail Create(string code, string message)
            => new(code, message);

        /// <summary>Creates a field-level validation error.</summary>
        public static ErrorDetail Validation(string field, string message)
            => new("VALIDATION_ERROR", message, field);

        /// <summary>Creates a not-found error.</summary>
        public static ErrorDetail NotFound(string resource, string? identifier = null)
        {
            var msg = identifier is not null
                ? $"{resource} with identifier '{identifier}' was not found."
                : $"{resource} was not found.";
            return new("NOT_FOUND", msg);
        }

        /// <summary>Creates an unauthorized error.</summary>
        public static ErrorDetail Unauthorized(string? message = null)
            => new("UNAUTHORIZED", message ?? "Authentication is required.");

        /// <summary>Creates a forbidden error.</summary>
        public static ErrorDetail Forbidden(string? message = null)
            => new("FORBIDDEN", message ?? "You do not have permission to perform this action.");

        /// <inheritdoc />
        public override string ToString() =>
            Field is not null
                ? $"[{Code}] {Field}: {Message}"
                : $"[{Code}] {Message}";
    }

}
