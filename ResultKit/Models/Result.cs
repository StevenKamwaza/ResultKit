using ResultKit.Extentions;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResultKit.Models
{
    /// <summary>
    /// Represents the outcome of an operation that does not produce a value.
    /// Ideal for commands, saves, deletes, and any void-returning service methods.
    /// </summary>
    public sealed class Result
    {
        //  State 

        /// <summary>Human-readable message describing the outcome.</summary>
        public string Message { get; }

        /// <summary>Semantic status of the result.</summary>
        public ResultStatus Status { get; }

        /// <summary>Structured error details. Empty on success.</summary>
        public IReadOnlyList<ErrorDetail> Errors { get; }

        /// <summary>True when the operation succeeded.</summary>
        public bool IsSuccess { get; }

        /// <summary>True when the operation failed.</summary>
        public bool IsFailure => !IsSuccess;

        //  Constructor 

        private Result(
            bool isSuccess,
            string message,
            ResultStatus status,
            IReadOnlyList<ErrorDetail> errors)
        {
            IsSuccess = isSuccess;
            Message = message;
            Status = status;
            Errors = errors;
        }

        //  Success factories 

        /// <summary>Creates a successful result with no message.</summary>
        public static Result Ok()
            => new(true, string.Empty, ResultStatus.Ok, []);

        /// <summary>Creates a successful result with a message.</summary>
        public static Result Ok(string message)
            => new(true, message, ResultStatus.Ok, []);

        /// <summary>Creates a 201 Created result.</summary>
        public static Result Created(string message = "")
            => new(true, message, ResultStatus.Created, []);

        /// <summary>Creates a 204 No Content result.</summary>
        public static Result NoContent()
            => new(true, string.Empty, ResultStatus.NoContent, []);

        //  Failure factories 

        /// <summary>Creates a failed result with a message and optional status.</summary>
        public static Result Fail(string message, ResultStatus status = ResultStatus.Error)
            => new(false, message, status, []);

        /// <summary>Creates a failed result with a single structured error.</summary>
        public static Result Fail(ErrorDetail error, ResultStatus status = ResultStatus.Error)
            => new(false, error.Message, status, [error]);

        /// <summary>Creates a failed result with multiple structured errors.</summary>
        public static Result Fail(IEnumerable<ErrorDetail> errors, ResultStatus status = ResultStatus.Error)
        {
            var list = errors.ToList();
            var msg = list.Count > 0 ? list[0].Message : string.Empty;
            return new(false, msg, status, list);
        }

        /// <summary>Creates a 404 Not Found failure.</summary>
        public static Result NotFound(string message = "The requested resource was not found.")
            => Fail(message, ResultStatus.NotFound);

        /// <summary>Creates a 401 Unauthorized failure.</summary>
        public static Result Unauthorized(string message = "Authentication is required.")
            => Fail(message, ResultStatus.Unauthorized);

        /// <summary>Creates a 403 Forbidden failure.</summary>
        public static Result Forbidden(string message = "You do not have permission to perform this action.")
            => Fail(message, ResultStatus.Forbidden);

        /// <summary>Creates a 409 Conflict failure.</summary>
        public static Result Conflict(string message)
            => Fail(message, ResultStatus.Conflict);

        /// <summary>Creates a 400/422 Invalid/Validation failure.</summary>
        public static Result Invalid(IEnumerable<ErrorDetail> errors)
            => Fail(errors, ResultStatus.Invalid);

        /// <summary>Creates a 400 Invalid failure with a single message.</summary>
        public static Result Invalid(string message)
            => Fail(message, ResultStatus.Invalid);

        //  Chaining 

        /// <summary>
        /// Executes <paramref name="next"/> only if this result is a success.
        /// Returns this failure unchanged if this result has already failed.
        /// </summary>
        public Result Then(Func<Result> next)
            => IsSuccess ? next() : this;

        /// <summary>Async overload of <see cref="Then"/>.</summary>
        public async Task<Result> ThenAsync(Func<Task<Result>> next)
            => IsSuccess ? await next() : this;

        /// <summary>Executes <paramref name="next"/> only if this result is a success, producing a typed result.</summary>
        public Result<T> Then<T>(Func<Result<T>> next)
            => IsSuccess ? next() : Result<T>.Fail(Message, Status);

        /// <summary>Async overload of <see cref="Then{T}"/>.</summary>
        public async Task<Result<T>> ThenAsync<T>(Func<Task<Result<T>>> next)
            => IsSuccess ? await next() : Result<T>.Fail(Message, Status);

        /// <summary>Runs <paramref name="action"/> if this result is a success, then returns this unchanged.</summary>
        public Result Tap(Action action)
        {
            if (IsSuccess) action();
            return this;
        }

        /// <summary>Async overload of <see cref="Tap"/>.</summary>
        public async Task<Result> TapAsync(Func<Task> action)
        {
            if (IsSuccess) await action();
            return this;
        }

        /// <summary>Runs <paramref name="action"/> if this result is a failure, then returns this unchanged.</summary>
        public Result TapError(Action<Result> action)
        {
            if (IsFailure) action(this);
            return this;
        }

        /// <summary>Async overload of <see cref="TapError"/>.</summary>
        public async Task<Result> TapErrorAsync(Func<Result, Task> action)
        {
            if (IsFailure) await action(this);
            return this;
        }

        /// <summary>
        /// Projects the result into a single value depending on success or failure.
        /// </summary>
        public TOut Match<TOut>(Func<TOut> onSuccess, Func<Result, TOut> onFailure)
            => IsSuccess ? onSuccess() : onFailure(this);

        /// <summary>Async overload of <see cref="Match{TOut}"/>.</summary>
        public async Task<TOut> MatchAsync<TOut>(
            Func<Task<TOut>> onSuccess,
            Func<Result, Task<TOut>> onFailure)
            => IsSuccess ? await onSuccess() : await onFailure(this);

        //  Try wrappers 

        /// <summary>Wraps a synchronous void delegate, catching exceptions as failures.</summary>
        public static Result Try(Action operation, string? errorMessage = null)
        {
            try
            {
                operation();
                return Ok();
            }
            catch (Exception ex)
            {
                return Fail(errorMessage ?? ex.Message);
            }
        }

        /// <summary>Wraps an async void delegate, catching exceptions as failures.</summary>
        public static async Task<Result> TryAsync(
            Func<Task> operation,
            string? errorMessage = null,
            string? successMessage = null)
        {
            try
            {
                await operation();
                return successMessage is not null ? Ok(successMessage) : Ok();
            }
            catch (Exception ex)
            {
                return Fail(errorMessage ?? ex.Message);
            }
        }

        /// <summary>Wraps an async typed delegate, catching exceptions as failures.</summary>
        public static async Task<Result<T>> TryAsync<T>(
            Func<Task<T>> operation,
            string? errorMessage = null,
            string? successMessage = null)
            => await Result<T>.TryAsync(operation, errorMessage, successMessage);

        //  Conversions 

        /// <summary>Converts to a typed <see cref="Result{T}"/> with no value.</summary>
        public Result<T> ToResult<T>()
            => IsSuccess
                ? Result<T>.Ok(default!, Message)
                : Result<T>.Fail(Message, Status);

        /// <inheritdoc />
        public override string ToString()
            => IsSuccess
                ? $"Ok {Message}".Trim()
                : $"Fail({Status}) {Message}".Trim();
    }

}
