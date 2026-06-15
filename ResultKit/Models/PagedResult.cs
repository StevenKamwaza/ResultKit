using System;
using System.Collections.Generic;
using System.Text;

namespace ResultKit.Models
{
    /// <summary>
    /// Wraps a page of items alongside the full pagination metadata.
    /// Designed to be used as the value type inside <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    public sealed class PagedResult<T>
    {
        /// <summary>The items on the current page.</summary>
        public IReadOnlyList<T> Items { get; init; } = [];

        /// <summary>Total number of items across all pages.</summary>
        public int TotalCount { get; init; }

        /// <summary>The current page number (1-based).</summary>
        public int PageNumber { get; init; }

        /// <summary>Maximum number of items per page.</summary>
        public int PageSize { get; init; }

        /// <summary>Total number of pages, derived from <see cref="TotalCount"/> and <see cref="PageSize"/>.</summary>
        public int TotalPages => PageSize > 0
            ? (int)Math.Ceiling((double)TotalCount / PageSize)
            : 0;

        /// <summary>Whether there is a page before the current one.</summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>Whether there is a page after the current one.</summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>Whether the current page is empty.</summary>
        public bool IsEmpty => Items.Count == 0;


        /// <summary>
        /// Creates a <see cref="PagedResult{T}"/> from a full source list by slicing in-memory.
        /// Useful in tests and simple scenarios. For production, prefer database-level paging.
        /// </summary>
        public static PagedResult<T> From(
            IEnumerable<T> source,
            int pageNumber,
            int pageSize)
        {
            var list = source as IList<T> ?? source.ToList();
            var items = list
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = list.Count,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Creates a <see cref="PagedResult{T}"/> when you already have the page items
        /// and the total count from the database.
        /// </summary>
        public static PagedResult<T> Create(
            IEnumerable<T> items,
            int totalCount,
            int pageNumber,
            int pageSize)
            => new()
            {
                Items = [.. items],
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

        /// <summary>Returns an empty paged result.</summary>
        public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 20)
            => new()
            {
                Items = [],
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

        /// <inheritdoc />
        public override string ToString()
            => $"Page {PageNumber}/{TotalPages} — {Items.Count} of {TotalCount} items";
    }

}
