using System;
using System.Collections.Generic;
using System.Text;
using ResultKit;
using ResultKit.Extensions;
using ResultKit.Extentions;
using ResultKit.Models;
using Xunit;

namespace Tests
{

    public class ResultOfTTests
    {
        [Fact]
        public void Ok_WithValue_IsSuccess()
        {
            var result = Result<int>.Ok(42);
            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.Equal(42, result.Value);
            Assert.Equal(ResultStatus.Ok, result.Status);
        }

        [Fact]
        public void Ok_WithValueAndMessage_CarriesMessage()
        {
            var result = Result<string>.Ok("hello", "Loaded fine.");
            Assert.True(result.IsSuccess);
            Assert.Equal("hello", result.Value);
            Assert.Equal("Loaded fine.", result.Message);
        }

        [Fact]
        public void Fail_WithMessage_IsFailure()
        {
            var result = Result<int>.Fail("Something went wrong.");
            Assert.True(result.IsFailure);
            Assert.Equal("Something went wrong.", result.Message);
            Assert.Equal(ResultStatus.Error, result.Status);
        }

        [Fact]
        public void NotFound_SetsCorrectStatus()
        {
            var result = Result<string>.NotFound("User not found.");
            Assert.True(result.IsFailure);
            Assert.Equal(ResultStatus.NotFound, result.Status);
        }

        [Fact]
        public void Unauthorized_SetsCorrectStatus()
        {
            var result = Result<string>.Unauthorized();
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
        }

        [Fact]
        public void Invalid_WithErrors_CarriesErrors()
        {
            var errors = new[]
            {
            ErrorDetail.Validation("Email",    "Email is required."),
            ErrorDetail.Validation("Password", "Password too short.")
        };
            var result = Result<string>.Invalid(errors);

            Assert.True(result.IsFailure);
            Assert.Equal(ResultStatus.Invalid, result.Status);
            Assert.Equal(2, result.Errors.Count);
            Assert.Equal("Email", result.Errors[0].Field);
        }

        [Fact]
        public void Map_OnSuccess_ProjectsValue()
        {
            var result = Result<int>.Ok(5).Map(x => x * 2);
            Assert.True(result.IsSuccess);
            Assert.Equal(10, result.Value);
        }

        [Fact]
        public void Map_OnFailure_PropagatesFailure()
        {
            var result = Result<int>.Fail("error").Map(x => x * 2);
            Assert.True(result.IsFailure);
            Assert.Equal("error", result.Message);
        }

        [Fact]
        public async Task MapAsync_OnSuccess_ProjectsValue()
        {
            var result = await Result<int>.Ok(5)
                .MapAsync(x => Task.FromResult(x * 3));
            Assert.Equal(15, result.Value);
        }

        [Fact]
        public void Then_OnSuccess_ContinuesChain()
        {
            var result = Result<int>.Ok(5)
                .Then(x => Result<string>.Ok($"value is {x}"));
            Assert.Equal("value is 5", result.Value);
        }

        [Fact]
        public void Then_OnFailure_ShortCircuits()
        {
            var called = false;
            Result<int>.Fail("already failed")
                .Then(x => { called = true; return Result<string>.Ok("ok"); });
            Assert.False(called);
        }

        [Fact]
        public void Tap_OnSuccess_RunsSideEffect()
        {
            var captured = 0;
            var result = Result<int>.Ok(7).Tap(x => captured = x);
            Assert.Equal(7, captured);
            Assert.True(result.IsSuccess); // unchanged
        }

        [Fact]
        public void Tap_OnFailure_DoesNotRunSideEffect()
        {
            var called = false;
            Result<int>.Fail("bad").Tap(_ => called = true);
            Assert.False(called);
        }

        [Fact]
        public void TapError_OnFailure_RunsSideEffect()
        {
            string? captured = null;
            Result<int>.Fail("oops").TapError(r => captured = r.Message);
            Assert.Equal("oops", captured);
        }

        [Fact]
        public void Ensure_FailsWhenPredicateFalse()
        {
            var result = Result<int>.Ok(5)
                .Ensure(x => x > 10, "Must be greater than 10.");
            Assert.True(result.IsFailure);
            Assert.Equal("Must be greater than 10.", result.Message);
        }

        [Fact]
        public void Ensure_PassesWhenPredicateTrue()
        {
            var result = Result<int>.Ok(15)
                .Ensure(x => x > 10, "Must be greater than 10.");
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Match_OnSuccess_CallsSuccessBranch()
        {
            var output = Result<int>.Ok(3).Match(
                onSuccess: v => v * 2,
                onFailure: _ => -1);
            Assert.Equal(6, output);
        }

        [Fact]
        public void Match_OnFailure_CallsFailureBranch()
        {
            var output = Result<int>.Fail("err").Match(
                onSuccess: v => v * 2,
                onFailure: _ => -1);
            Assert.Equal(-1, output);
        }

        [Fact]
        public void Try_CatchesException_ReturnsFailure()
        {
            var result = Result<int>.Try(() => throw new InvalidOperationException("boom"));
            Assert.True(result.IsFailure);
            Assert.Equal("boom", result.Message);
        }

        [Fact]
        public async Task TryAsync_CatchesException_ReturnsFailure()
        {
            var result = await Result<int>.TryAsync(
                () => throw new InvalidOperationException("async boom"),
                errorMessage: "Custom error");
            Assert.True(result.IsFailure);
            Assert.Equal("Custom error", result.Message);
        }

        [Fact]
        public async Task TryAsync_OnSuccess_ReturnsValue()
        {
            var result = await Result<int>.TryAsync(() => Task.FromResult(99));
            Assert.True(result.IsSuccess);
            Assert.Equal(99, result.Value);
        }

        [Fact]
        public void ImplicitConversion_FromValue_IsSuccess()
        {
            Result<string> result = "hello";
            Assert.True(result.IsSuccess);
            Assert.Equal("hello", result.Value);
        }

        [Fact]
        public void Deconstruct_ExposesComponents()
        {
            var (isSuccess, value, message) = Result<int>.Ok(42, "done");
            Assert.True(isSuccess);
            Assert.Equal(42, value);
            Assert.Equal("done", message);
        }
    }

    // ── Non-generic Result tests ───────────────────────────────────────────────

    public class ResultTests
    {
        [Fact]
        public void Ok_IsSuccess()
        {
            var result = Result.Ok();
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Ok, result.Status);
        }

        [Fact]
        public void Fail_WithMessage_IsFailure()
        {
            var result = Result.Fail("Deletion failed.");
            Assert.True(result.IsFailure);
            Assert.Equal("Deletion failed.", result.Message);
        }

        [Fact]
        public void Then_OnSuccess_ContinuesChain()
        {
            var reached = false;
            var result = Result.Ok().Then(() => { reached = true; return Result.Ok(); });
            Assert.True(reached);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Then_OnFailure_ShortCircuits()
        {
            var reached = false;
            Result.Fail("fail").Then(() => { reached = true; return Result.Ok(); });
            Assert.False(reached);
        }

        [Fact]
        public async Task TryAsync_VoidDelegate_Success()
        {
            var result = await Result.TryAsync(() => Task.CompletedTask, successMessage: "Done.");
            Assert.True(result.IsSuccess);
            Assert.Equal("Done.", result.Message);
        }

        [Fact]
        public async Task TryAsync_VoidDelegate_Failure()
        {
            var result = await Result.TryAsync(
                () => throw new Exception("fail"),
                errorMessage: "Custom error");
            Assert.True(result.IsFailure);
            Assert.Equal("Custom error", result.Message);
        }
    }

    // ── PagedResult tests ──────────────────────────────────────────────────────

    public class PagedResultTests
    {
        [Fact]
        public void TotalPages_CalculatedCorrectly()
        {
            var paged = PagedResult<int>.Create([1, 2, 3], totalCount: 25, pageNumber: 1, pageSize: 10);
            Assert.Equal(3, paged.TotalPages);
        }

        [Fact]
        public void HasNextPage_TrueOnIntermediatePages()
        {
            var paged = PagedResult<int>.Create([], totalCount: 25, pageNumber: 1, pageSize: 10);
            Assert.True(paged.HasNextPage);
        }

        [Fact]
        public void HasPreviousPage_TrueOnSecondPage()
        {
            var paged = PagedResult<int>.Create([], totalCount: 25, pageNumber: 2, pageSize: 10);
            Assert.True(paged.HasPreviousPage);
        }

        [Fact]
        public void From_SlicesCorrectly()
        {
            var source = Enumerable.Range(1, 50);
            var paged = PagedResult<int>.From(source, pageNumber: 2, pageSize: 10);

            Assert.Equal(10, paged.Items.Count);
            Assert.Equal(11, paged.Items[0]);
            Assert.Equal(20, paged.Items[9]);
            Assert.Equal(50, paged.TotalCount);
        }

        [Fact]
        public void Empty_ReturnsZeroItems()
        {
            var paged = PagedResult<string>.Empty();
            Assert.True(paged.IsEmpty);
            Assert.Equal(0, paged.TotalPages);
        }
    }

    // ── ErrorDetail tests ──────────────────────────────────────────────────────

    public class ErrorDetailTests
    {
        [Fact]
        public void Validation_SetsFieldAndMessage()
        {
            var e = ErrorDetail.Validation("Email", "Invalid email.");
            Assert.Equal("Email", e.Field);
            Assert.Equal("Invalid email.", e.Message);
            Assert.Equal("VALIDATION_ERROR", e.Code);
        }

        [Fact]
        public void NotFound_BuildsMessage()
        {
            var e = ErrorDetail.NotFound("User", "abc-123");
            Assert.Contains("abc-123", e.Message);
            Assert.Equal("NOT_FOUND", e.Code);
        }

        [Fact]
        public void EmptyCode_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new ErrorDetail("", "msg"));
        }
    }

    // ── ResultState<T> (Blazor) tests ──────────────────────────────────────────

    public class ResultStateTests
    {
        [Fact]
        public async Task ExecuteAsync_AppliesSuccessResult()
        {
            var state = new ResultState<int>();
            await state.ExecuteAsync(() => Task.FromResult(Result<int>.Ok(42)));

            Assert.True(state.IsSuccess);
            Assert.False(state.IsLoading);
            Assert.Equal(42, state.Value);
        }

        [Fact]
        public async Task ExecuteAsync_AppliesFailureResult()
        {
            var state = new ResultState<int>();
            await state.ExecuteAsync(() => Task.FromResult(Result<int>.Fail("Error!")));

            Assert.True(state.IsFailure);
            Assert.Equal("Error!", state.Message);
        }

        [Fact]
        public async Task ExecuteAsync_SetsLoadingBeforeOperation()
        {
            var loadingWasTrue = false;
            var state = new ResultState<int>();

            await state.ExecuteAsync(
                async () =>
                {
                    await Task.Yield();
                    return Result<int>.Ok(1);
                },
                stateChanged: () => { if (state.IsLoading) loadingWasTrue = true; });

            Assert.True(loadingWasTrue);
        }

        [Fact]
        public void Reset_ClearsState()
        {
            var state = new ResultState<int>();
            state.ExecuteAsync(() => Task.FromResult(Result<int>.Ok(5))).Wait();
            state.Reset();

            Assert.True(state.IsIdle);
            Assert.Equal(default, state.Value); 
        }
    }

    // ── Blazor extension tests ─────────────────────────────────────────────────

    public class ResultBlazorExtensionTests
    {
        [Fact]
        public void ErrorsForField_ReturnsOnlyMatchingField()
        {
            var result = Result<string>.Invalid(new[]
            {
            ErrorDetail.Validation("Email",    "Required."),
            ErrorDetail.Validation("Password", "Too short.")
        });

            var emailErrors = result.ErrorsForField("Email").ToList();
            Assert.Single(emailErrors);
            Assert.Equal("Required.", emailErrors[0]);
        }

        [Fact]
        public void HasFieldError_ReturnsTrueForKnownField()
        {
            var result = Result<string>.Invalid(new[]
            {
            ErrorDetail.Validation("Email", "Required.")
        });
            Assert.True(result.HasFieldError("Email"));
            Assert.False(result.HasFieldError("Phone"));
        }

        [Fact]
        public void ToCssClass_ReturnsSuccessClass_WhenSuccess()
        {
            var r = Result<int>.Ok(1);
            Assert.Equal("alert-success", r.ToCssClass());
        }

        [Fact]
        public void ToCssClass_ReturnsFailureClass_WhenFailure()
        {
            var r = Result<int>.Fail("err");
            Assert.Equal("alert-danger", r.ToCssClass());
        }

        [Fact]
        public void ValueOrDefault_ReturnsSelector_OnSuccess()
        {
            var r = Result<string>.Ok("hello");
            Assert.Equal("HELLO", r.ValueOrDefault(s => s.ToUpper(), "FALLBACK"));
        }

        [Fact]
        public void ValueOrDefault_ReturnsFallback_OnFailure()
        {
            var r = Result<string>.Fail("err");
            Assert.Equal("FALLBACK", r.ValueOrDefault(s => s.ToUpper(), "FALLBACK"));
        }
    }

    // ── Task chaining extension tests ──────────────────────────────────────────

    public class ResultTaskExtensionTests
    {
        [Fact]
        public async Task Map_OnTaskResult_ProjectsValue()
        {
            var result = await Task.FromResult(Result<int>.Ok(5))
                .Map(x => x * 10);
            Assert.Equal(50, result.Value);
        }

        [Fact]
        public async Task TapError_OnTaskResult_LogsError()
        {
            string? captured = null;
            await Task.FromResult(Result<int>.Fail("bad"))
                .TapError(r => captured = r.Message);
            Assert.Equal("bad", captured);
        }

        [Fact]
        public async Task Match_OnTaskResult_BranchesCorrectly()
        {
            var value = await Task.FromResult(Result<int>.Ok(9))
                .Match(v => v + 1, _ => -1);
            Assert.Equal(10, value);
        }

        [Fact]
        public async Task Ensure_OnTaskResult_FailsWhenPredicateFalse()
        {
            var result = await Task.FromResult(Result<int>.Ok(3))
                .Ensure(x => x > 10, "Must exceed 10.");
            Assert.True(result.IsFailure);
        }
    }
}
