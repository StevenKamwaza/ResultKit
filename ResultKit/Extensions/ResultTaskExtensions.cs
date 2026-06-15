using ResultKit.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResultKit.Extensions
{
    /// <summary>
    /// Extension methods that allow fluent chaining directly on <see cref="Task{Result}"/>
    /// and <see cref="Task{Result{T}}"/> without needing to await at every step.
    /// </summary>
    public static class ResultTaskExtensions
    {

        /// <inheritdoc cref="Result{T}.Then{TNext}(Func{T, Result{TNext}})"/>
        public static async Task<Result<TNext>> Then<T, TNext>(
            this Task<Result<T>> task,
            Func<T, Result<TNext>> next)
        {
            var result = await task;
            return result.Then(next);
        }

        /// <inheritdoc cref="Result{T}.ThenAsync{TNext}"/>
        public static async Task<Result<TNext>> ThenAsync<T, TNext>(
            this Task<Result<T>> task,
            Func<T, Task<Result<TNext>>> next)
        {
            var result = await task;
            return await result.ThenAsync(next);
        }

        /// <inheritdoc cref="Result{T}.Map{TNext}(Func{T, TNext})"/>
        public static async Task<Result<TNext>> Map<T, TNext>(
            this Task<Result<T>> task,
            Func<T, TNext> mapper)
        {
            var result = await task;
            return result.Map(mapper);
        }

        /// <inheritdoc cref="Result{T}.MapAsync{TNext}"/>
        public static async Task<Result<TNext>> MapAsync<T, TNext>(
            this Task<Result<T>> task,
            Func<T, Task<TNext>> mapper)
        {
            var result = await task;
            return await result.MapAsync(mapper);
        }

        /// <inheritdoc cref="Result{T}.Tap(Action{T})"/>
        public static async Task<Result<T>> Tap<T>(
            this Task<Result<T>> task,
            Action<T> action)
        {
            var result = await task;
            return result.Tap(action);
        }

        /// <inheritdoc cref="Result{T}.TapAsync"/>
        public static async Task<Result<T>> TapAsync<T>(
            this Task<Result<T>> task,
            Func<T, Task> action)
        {
            var result = await task;
            return await result.TapAsync(action);
        }

        /// <inheritdoc cref="Result{T}.TapError(Action{Result{T}})"/>
        public static async Task<Result<T>> TapError<T>(
            this Task<Result<T>> task,
            Action<Result<T>> action)
        {
            var result = await task;
            return result.TapError(action);
        }

        /// <inheritdoc cref="Result{T}.TapErrorAsync"/>
        public static async Task<Result<T>> TapErrorAsync<T>(
            this Task<Result<T>> task,
            Func<Result<T>, Task> action)
        {
            var result = await task;
            return await result.TapErrorAsync(action);
        }

        /// <inheritdoc cref="Result{T}.Ensure"/>
        public static async Task<Result<T>> Ensure<T>(
            this Task<Result<T>> task,
            Func<T, bool> predicate,
            string errorMessage,
            ResultStatus status = ResultStatus.Invalid)
        {
            var result = await task;
            return result.Ensure(predicate, errorMessage, status);
        }

        /// <inheritdoc cref="Result{T}.Match{TOut}(Func{T, TOut}, Func{Result{T}, TOut})"/>
        public static async Task<TOut> Match<T, TOut>(
            this Task<Result<T>> task,
            Func<T, TOut> onSuccess,
            Func<Result<T>, TOut> onFailure)
        {
            var result = await task;
            return result.Match(onSuccess, onFailure);
        }

        //  Task<Result> chaining (non-generic) 

        /// <inheritdoc cref="Result.Then(Func{Result})"/>
        public static async Task<Result> Then(
            this Task<Result> task,
            Func<Result> next)
        {
            var result = await task;
            return result.Then(next);
        }

        /// <inheritdoc cref="Result.ThenAsync(Func{Task{Result}})"/>
        public static async Task<Result> ThenAsync(
            this Task<Result> task,
            Func<Task<Result>> next)
        {
            var result = await task;
            return await result.ThenAsync(next);
        }

        /// <inheritdoc cref="Result.Tap(Action)"/>
        public static async Task<Result> Tap(
            this Task<Result> task,
            Action action)
        {
            var result = await task;
            return result.Tap(action);
        }

        /// <inheritdoc cref="Result.TapAsync"/>
        public static async Task<Result> TapAsync(
            this Task<Result> task,
            Func<Task> action)
        {
            var result = await task;
            return await result.TapAsync(action);
        }

        /// <inheritdoc cref="Result.TapError(Action{Result})"/>
        public static async Task<Result> TapError(
            this Task<Result> task,
            Action<Result> action)
        {
            var result = await task;
            return result.TapError(action);
        }

        /// <inheritdoc cref="Result.Match{TOut}(Func{TOut}, Func{Result, TOut})"/>
        public static async Task<TOut> Match<TOut>(
            this Task<Result> task,
            Func<TOut> onSuccess,
            Func<Result, TOut> onFailure)
        {
            var result = await task;
            return result.Match(onSuccess, onFailure);
        }
    }

}
