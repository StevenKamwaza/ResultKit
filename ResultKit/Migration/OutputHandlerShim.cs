using ResultKit.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResultKit.Migration
{
    /// <summary>
    /// A minimal shim to help migrate from <c>OutputHandler&lt;T&gt;</c> to <see cref="Result{T}"/>
    /// without a big-bang refactor. Add this file once and migrate service by service.
    /// </summary>
    public static class OutputHandlerShim
    {
        /// <summary>
        /// Converts an <c>OutputHandler&lt;T&gt;</c>-shaped object to a <see cref="Result{T}"/>.
        /// Call this at the boundary where new code consumes old services.
        /// </summary>
        /// <param name="isErrorOccured">The <c>IsErrorOccured</c> flag.</param>
        /// <param name="message">The <c>Message</c> string.</param>
        /// <param name="result">The <c>Result</c> / <c>Value</c> field.</param>
        public static Result<T> ToResult<T>(bool isErrorOccured, string message, T? result)
            => isErrorOccured
                ? Result<T>.Fail(message)
                : Result<T>.Ok(result!, message);
    }

    /// <summary>
    /// Extension methods on <see cref="Result{T}"/> for OutputHandler compatibility.
    /// </summary>
    public static class OutputHandlerResultExtensions
    {
        /// <summary>
        /// Projects a <see cref="Result{T}"/> into an <c>OutputHandler</c>-compatible shape.
        /// Use this when a caller still expects the old contract while you migrate the provider.
        /// </summary>
        public static (bool IsErrorOccured, string Message, T? Result) ToOutputHandlerTuple<T>(
            this Result<T> result)
            => (result.IsFailure, result.Message, result.Value);
    }

}
