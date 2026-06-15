using System;
using System.Collections.Generic;
using System.Text;
using ResultKit.Models;

namespace ResultKit.Extentions
{
    /// <summary>
    /// A lightweight view-model that Blazor components can bind to directly.
    /// Holds all the state needed to drive loading indicators, error messages,
    /// success feedback, and the result value — no manual flag management.
    /// </summary>
    /// <typeparam name="T">The value type returned on success.</typeparam>
    public sealed class ResultState<T>
    {
        /// <summary>True while an async operation is in progress.</summary>
        public bool IsLoading { get; private set; }

        /// <summary>True when the last operation succeeded.</summary>
        public bool IsSuccess { get; private set; }

        /// <summary>True when the last operation failed.</summary>
        public bool IsFailure { get; private set; }

        /// <summary>True before any operation has been executed.</summary>
        public bool IsIdle => !IsLoading && !IsSuccess && !IsFailure;

        /// <summary>The value returned by the last successful operation.</summary>
        public T? Value { get; private set; }

        /// <summary>Human-readable message from the last operation.</summary>
        public string Message { get; private set; } = string.Empty;

        /// <summary>Structured errors from the last failed operation.</summary>
        public IReadOnlyList<ErrorDetail> Errors { get; private set; } = [];

        /// <summary>The semantic status of the last result.</summary>
        public ResultStatus Status { get; private set; } = ResultStatus.Ok;

        //  Mutation 

        internal void SetLoading()
        {
            IsLoading = true;
            IsSuccess = false;
            IsFailure = false;
            Value = default;
            Message = string.Empty;
            Errors = [];
        }

        internal void Apply(Result<T> result)
        {
            IsLoading = false;
            IsSuccess = result.IsSuccess;
            IsFailure = result.IsFailure;
            Value = result.Value;
            Message = result.Message;
            Errors = result.Errors;
            Status = result.Status;
        }

        internal void Apply(Result result)
        {
            IsLoading = false;
            IsSuccess = result.IsSuccess;
            IsFailure = result.IsFailure;
            Value = default;
            Message = result.Message;
            Errors = result.Errors;
            Status = result.Status;
        }

        /// <summary>Resets the state back to idle.</summary>
        public void Reset()
        {
            IsLoading = false;
            IsSuccess = false;
            IsFailure = false;
            Value = default;
            Message = string.Empty;
            Errors = [];
            Status = ResultStatus.Ok;
        }
    }

    /// <summary>
    /// Extension methods for binding <see cref="Result{T}"/> to Blazor component state.
    /// </summary>
    public static class ResultBlazorExtensions
    {
        //  ResultState<T> execution helpers 

        /// <summary>
        /// Executes <paramref name="operation"/> and updates <paramref name="state"/> automatically,
        /// setting loading before the call and applying the result after.
        /// Triggers <paramref name="stateChanged"/> (typically <c>StateHasChanged</c>) on each transition.
        /// </summary>
        /// <example>
        /// <code>
        /// private ResultState&lt;List&lt;UserDto&gt;&gt; _usersState = new();
        ///
        /// protected override async Task OnInitializedAsync()
        ///     => await _usersState.ExecuteAsync(() => _service.GetAllAsync(), StateHasChanged);
        /// </code>
        /// </example>
        public static async Task ExecuteAsync<T>(
            this ResultState<T> state,
            Func<Task<Result<T>>> operation,
            Action? stateChanged = null)
        {
            state.SetLoading();
            stateChanged?.Invoke();

            var result = await operation();
            state.Apply(result);
            stateChanged?.Invoke();
        }

        /// <summary>
        /// Executes a void <paramref name="operation"/> and updates <paramref name="state"/> automatically.
        /// </summary>
        public static async Task ExecuteAsync<T>(
            this ResultState<T> state,
            Func<Task<Result>> operation,
            Action? stateChanged = null)
        {
            state.SetLoading();
            stateChanged?.Invoke();

            var result = await operation();
            state.Apply(result);
            stateChanged?.Invoke();
        }

        //  Binding helpers on Result<T> 

        /// <summary>
        /// Returns <paramref name="value"/> if the result is a success, otherwise <paramref name="fallback"/>.
        /// Useful for providing safe defaults in Razor markup without null checks.
        /// </summary>
        public static TOut ValueOrDefault<T, TOut>(
            this Result<T> result,
            Func<T, TOut> selector,
            TOut fallback)
            => result.IsSuccess ? selector(result.Value!) : fallback;

        /// <summary>
        /// Returns the value if the result is a success, otherwise the default of <typeparamref name="T"/>.
        /// </summary>
        public static T? ValueOrDefault<T>(this Result<T> result)
            => result.IsSuccess ? result.Value : default;

        /// <summary>
        /// Returns the first error message, or an empty string if there are no errors.
        /// Handy for inline field validation messages in forms.
        /// </summary>
        public static string FirstErrorMessage<T>(this Result<T> result)
            => result.Errors.Count > 0 ? result.Errors[0].Message : string.Empty;

        /// <summary>
        /// Returns all error messages for a specific field, useful for driving
        /// per-field validation feedback in Blazor EditForms.
        /// </summary>
        public static IEnumerable<string> ErrorsForField<T>(this Result<T> result, string fieldName)
            => result.Errors
                .Where(e => string.Equals(e.Field, fieldName, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.Message);

        /// <summary>
        /// Returns all error messages for a specific field (non-generic overload).
        /// </summary>
        public static IEnumerable<string> ErrorsForField(this Result result, string fieldName)
            => result.Errors
                .Where(e => string.Equals(e.Field, fieldName, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.Message);

        /// <summary>
        /// Returns true if there is at least one error for the given field.
        /// </summary>
        public static bool HasFieldError<T>(this Result<T> result, string fieldName)
            => result.Errors.Any(e =>
                string.Equals(e.Field, fieldName, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Returns a CSS class string based on the result state.
        /// Useful for styling feedback banners or input borders in Razor components.
        /// </summary>
        /// <param name="result">The result to inspect.</param>
        /// <param name="successClass">CSS class when successful (default: "alert-success").</param>
        /// <param name="failureClass">CSS class when failed (default: "alert-danger").</param>
        public static string ToCssClass<T>(
            this Result<T> result,
            string successClass = "alert-success",
            string failureClass = "alert-danger")
            => result.IsSuccess ? successClass : failureClass;

        /// <inheritdoc cref="ToCssClass{T}"/>
        public static string ToCssClass(
            this Result result,
            string successClass = "alert-success",
            string failureClass = "alert-danger")
            => result.IsSuccess ? successClass : failureClass;
    }

}
