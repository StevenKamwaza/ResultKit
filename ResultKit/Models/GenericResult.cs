using ResultKit.Extentions;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResultKit.Models
{
    /// <summary>
    /// Represents the outcome of an operation that produces a value of type <typeparamref name="T"/>.
    /// Carries a success/failure state, an optional value, a human-readable message,
    /// a semantic status, and zero or more structured error details.
    /// </summary>
    /// <typeparam name="T">The value type produced on success.</typeparam>
    public sealed class Result<T>
    {
        //  State 

        /// <summary>The value produced by the operation. Only meaningful when <see cref="IsSuccess"/> is true.</summary>
        public T? Value { get; }

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

        //  Constructor (private — use factories) 

        private Result(
            bool isSuccess,
            T? value,
            string message,
            ResultStatus status,
            IReadOnlyList<ErrorDetail> errors)
        {
            IsSuccess = isSuccess;
            Value = value;
            Message = message;
            Status = status;
            Errors = errors;
        }

        //  Success factories 

        /// <summary>Creates a successful result carrying <paramref name="value"/>.</summary>
        public static Result<T> Ok(T value)
            => new(true, value, string.Empty, ResultStatus.Ok, []);

        /// <summary>Creates a successful result carrying <paramref name="value"/> with a message.</summary>
        public static Result<T> Ok(T value, string message)
            => new(true, value, message, ResultStatus.Ok, []);

        /// <summary>Creates a successful result with a specific status (e.g. <see cref="ResultStatus.Created"/>).</summary>
        public static Result<T> Ok(T value, string message, ResultStatus status)
            => new(true, value, message, status, []);

        /// <summary>Creates a 201 Created result.</summary>
        public static Result<T> Created(T value, string message = "")
            => new(true, value, message, ResultStatus.Created, []);

        //  Failure factories 

        /// <summary>Creates a failed result with a message and optional status.</summary>
        public static Result<T> Fail(string message, ResultStatus status = ResultStatus.Error)
            => new(false, default, message, status, []);

        /// <summary>Creates a failed result with a single structured error.</summary>
        public static Result<T> Fail(ErrorDetail error, ResultStatus status = ResultStatus.Error)
            => new(false, default, error.Message, status, [error]);

        /// <summary>Creates a failed result with multiple structured errors.</summary>
        public static Result<T> Fail(IEnumerable<ErrorDetail> errors, ResultStatus status = ResultStatus.Error)
        {
            var list = errors.ToList();
            var msg = list.Count > 0 ? list[0].Message : string.Empty;
            return new(false, default, msg, status, list);
        }

        /// <summary>Creates a 404 Not Found failure.</summary>
        public static Result<T> NotFound(string message = "The requested resource was not found.")
            => Fail(message, ResultStatus.NotFound);

        /// <summary>Creates a 401 Unauthorized failure.</summary>
        public static Result<T> Unauthorized(string message = "Authentication is required.")
            => Fail(message, ResultStatus.Unauthorized);

        /// <summary>Creates a 403 Forbidden failure.</summary>
        public static Result<T> Forbidden(string message = "You do not have permission to perform this action.")
            => Fail(message, ResultStatus.Forbidden);

        /// <summary>Creates a 409 Conflict failure.</summary>
        public static Result<T> Conflict(string message)
            => Fail(message, ResultStatus.Conflict);

        /// <summary>Creates a 400/422 Invalid/Validation failure from a list of field errors.</summary>
        public static Result<T> Invalid(IEnumerable<ErrorDetail> errors)
            => Fail(errors, ResultStatus.Invalid);

        /// <summary>Creates a 400 Invalid failure with a single message.</summary>
        public static Result<T> Invalid(string message)
            => Fail(message, ResultStatus.Invalid);

        //  Chaining 

        /// <summary>
        /// Executes <paramref name="next"/> only if this result is a success.
        /// Returns the failure unchanged if this result has already failed.
        /// </summary>
        public Result<TNext> Then<TNext>(Func<T, Result<TNext>> next)
            => IsSuccess ? next(Value!) : Result<TNext>.Fail(Message, Status);

        /// <summary>Async overload of <see cref="Then{TNext}"/>.</summary>
        public async Task<Result<TNext>> ThenAsync<TNext>(Func<T, Task<Result<TNext>>> next)
            => IsSuccess ? await next(Value!) : Result<TNext>.Fail(Message, Status);

        /// <summary>
        /// Projects the value using <paramref name="mapper"/> if this result is a success.
        /// </summary>
        public Result<TNext> Map<TNext>(Func<T, TNext> mapper)
            => IsSuccess
                ? Result<TNext>.Ok(mapper(Value!), Message, Status)
                : Result<TNext>.Fail(Message, Status);

        /// <summary>Async overload of <see cref="Map{TNext}"/>.</summary>
        public async Task<Result<TNext>> MapAsync<TNext>(Func<T, Task<TNext>> mapper)
            => IsSuccess
                ? Result<TNext>.Ok(await mapper(Value!), Message, Status)
                : Result<TNext>.Fail(Message, Status);

        /// <summary>
        /// Executes <paramref name="action"/> if this result is a success, then returns this result unchanged.
        /// Use for side-effects such as logging, caching, or event publishing.
        /// </summary>
        public Result<T> Tap(Action<T> action)
        {
            if (IsSuccess) action(Value!);
            return this;
        }

        /// <summary>Async overload of <see cref="Tap"/>.</summary>
        public async Task<Result<T>> TapAsync(Func<T, Task> action)
        {
            if (IsSuccess) await action(Value!);
            return this;
        }

        /// <summary>
        /// Executes <paramref name="action"/> if this result is a failure, then returns this result unchanged.
        /// </summary>
        public Result<T> TapError(Action<Result<T>> action)
        {
            if (IsFailure) action(this);
            return this;
        }

        /// <summary>Async overload of <see cref="TapError"/>.</summary>
        public async Task<Result<T>> TapErrorAsync(Func<Result<T>, Task> action)
        {
            if (IsFailure) await action(this);
            return this;
        }

        /// <summary>
        /// Fails this result if the <paramref name="predicate"/> returns false.
        /// Use to add guard conditions after a successful operation.
        /// </summary>
        public Result<T> Ensure(Func<T, bool> predicate, string errorMessage, ResultStatus status = ResultStatus.Invalid)
            => IsSuccess && !predicate(Value!)
                ? Fail(errorMessage, status)
                : this;

        /// <summary>
        /// Projects the result into a single value depending on success or failure.
        /// The idiomatic "end of the railway."
        /// </summary>
        public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Result<T>, TOut> onFailure)
            => IsSuccess ? onSuccess(Value!) : onFailure(this);

        /// <summary>Async overload of <see cref="Match{TOut}"/>.</summary>
        public async Task<TOut> MatchAsync<TOut>(
            Func<T, Task<TOut>> onSuccess,
            Func<Result<T>, Task<TOut>> onFailure)
            => IsSuccess ? await onSuccess(Value!) : await onFailure(this);

        //  Try wrappers 

        /// <summary>
        /// Wraps a synchronous delegate, catching all exceptions and converting them to a failed result.
        /// </summary>
        /// <param name="operation">The operation to run.</param>
        /// <param name="errorMessage">Message to use if an exception is thrown. Defaults to the exception message.</param>
        public static Result<T> Try(
            Func<T> operation,
            string? errorMessage = null)
        {
            try
            {
                return Ok(operation());
            }
            catch (Exception ex)
            {
                return Fail(errorMessage ?? ex.Message);
            }
        }

        /// <summary>
        /// Wraps an asynchronous delegate, catching all exceptions and converting them to a failed result.
        /// </summary>
        /// <param name="operation">The async operation to run.</param>
        /// <param name="errorMessage">Message to use if an exception is thrown. Defaults to the exception message.</param>
        /// <param name="successMessage">Optional message to attach on success.</param>
        public static async Task<Result<T>> TryAsync(
            Func<Task<T>> operation,
            string? errorMessage = null,
            string? successMessage = null)
        {
            try
            {
                var value = await operation();
                return successMessage is not null
                    ? Ok(value, successMessage)
                    : Ok(value);
            }
            catch (Exception ex)
            {
                return Fail(errorMessage ?? ex.Message);
            }
        }

        //  Implicit conversions 

        /// <summary>Implicitly wraps a value in a successful result.</summary>
        public static implicit operator Result<T>(T value) => Ok(value);


        /// <summary>Deconstructs the result into its core components for pattern matching.</summary>
        public void Deconstruct(out bool isSuccess, out T? value, out string message)
        {
            isSuccess = IsSuccess;
            value = Value;
            message = Message;
        }

        /// <inheritdoc />
        public override string ToString()
            => IsSuccess
                ? $"Ok({typeof(T).Name}) {Message}".Trim()
                : $"Fail({Status}) {Message}".Trim();
    }

}
