# ResultKit

[![NuGet](https://img.shields.io/nuget/v/ResultKit.svg)](https://www.nuget.org/packages/ResultKit)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9%20%7C%2010-purple.svg)](https://dotnet.microsoft.com)

**Expressive, zero-dependency result handling for .NET.**  
Replace exception-driven flow control with a clean `Result<T>` type that carries success/failure state, messages, structured errors, and semantic HTTP status. Works in APIs, MVC, Blazor, and background services.

```
dotnet add package ResultKit
```

---

## Quick start

```csharp
// Service
public async Task<Result<UserDto>> GetUserAsync(Guid id)
{
    return await Result.TryAsync(async () =>
    {
        var user = await _repo.FindAsync(id)
            ?? throw new KeyNotFoundException();
        return _mapper.Map<UserDto>(user);
    },
    errorMessage: "Failed to load user.");
}

// Controller
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id)
    => (await _service.GetUserAsync(id)).ToActionResult();

// Minimal API
app.MapGet("/users/{id}", async (Guid id, IUserService svc) =>
    (await svc.GetUserAsync(id)).ToHttpResult());
```

---

## Core types

| Type | Purpose |
|---|---|
| `Result<T>` | Operation that returns a value |
| `Result` | Void operation (command, delete, save) |
| `PagedResult<T>` | Paged collection with metadata |
| `ResultStatus` | Semantic status mapping to HTTP codes |
| `ErrorDetail` | Structured error with code, message, optional field |

---

## Creating results

```csharp
// Success
Result<UserDto>.Ok(dto);
Result<UserDto>.Ok(dto, "User loaded successfully.");
Result<UserDto>.Created(dto, "User created.");

// Failure
Result<UserDto>.Fail("User not found.", ResultStatus.NotFound);
Result<UserDto>.NotFound("User not found.");
Result<UserDto>.Unauthorized();
Result<UserDto>.Forbidden("Admin only.");
Result<UserDto>.Conflict("Email already in use.");
Result<UserDto>.Invalid("Name is required.");

// With structured errors (great for form validation)
Result<UserDto>.Invalid(new[]
{
    ErrorDetail.Validation("Email",    "Email is required."),
    ErrorDetail.Validation("Password", "Password must be at least 8 characters.")
});

// Wrap exception-throwing code
var result = await Result.TryAsync(() => _repo.SaveAsync(entity));
var result = Result.Try(() => JsonSerializer.Deserialize<Config>(raw));
```

---

## Fluent chaining

```csharp
var result = await GetUserAsync(id)
    .Ensure(u => u.IsActive,       "Account is disabled.",    ResultStatus.Forbidden)
    .Ensure(u => u.EmailConfirmed, "Email not confirmed.",    ResultStatus.Unauthorized)
    .ThenAsync(u => EnrichWithRolesAsync(u))
    .Map(u => _mapper.Map<UserDto>(u))
    .TapAsync(dto => _cache.SetAsync($"user:{dto.Id}", dto))
    .TapError(r => _logger.LogWarning("GetUser failed: {Msg}", r.Message));
```

### Chaining methods

| Method | Description |
|---|---|
| `.Then(fn)` | Continue with another `Result`-returning function |
| `.Map(fn)` | Project the value to a different type |
| `.Tap(fn)` | Side-effect on success (logging, caching) |
| `.TapError(fn)` | Side-effect on failure (logging, metrics) |
| `.Ensure(predicate, msg)` | Fail the result if a guard condition is false |
| `.Match(onSuccess, onFailure)` | Branch to two outcomes — end of the railway |

All chaining methods have `Async` overloads and work directly on `Task<Result<T>>`.

---

## ASP.NET Core

```csharp
// MVC controller
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id)
    => (await _service.GetUserAsync(id)).ToActionResult();

// With custom projection
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id)
    => (await _service.GetUserAsync(id))
        .ToActionResult(dto => new UserResponse(dto));

// Minimal API
app.MapGet("/users/{id}", async (Guid id, IUserService svc) =>
    (await svc.GetUserAsync(id)).ToHttpResult());

// RFC 7807 Problem Details
[HttpDelete("{id}")]
public async Task<IActionResult> Delete(Guid id)
    => (await _service.DeleteUserAsync(id)).ToProblemResult();
```

### ResultStatus → HTTP status code

| ResultStatus | HTTP |
|---|---|
| Ok | 200 |
| Created | 201 |
| NoContent | 204 |
| Invalid | 400 |
| Unauthorized | 401 |
| Forbidden | 403 |
| NotFound | 404 |
| Conflict | 409 |
| UnprocessableEntity | 422 |
| Error | 500 |

---

## Blazor

### `ResultState<T>` — the declarative component state machine

```razor
@* Component.razor *@
@inject IUserService UserService

@if (_users.IsLoading)
{
    <p>Loading...</p>
}
else if (_users.IsFailure)
{
    <div class="alert alert-danger">@_users.Message</div>
}
else if (_users.IsSuccess)
{
    <UserTable Items="@_users.Value!.Items" />
    <Pagination State="@_users.Value!" />
}

@code {
    private readonly ResultState<PagedResult<UserDto>> _users = new();

    protected override async Task OnInitializedAsync()
        => await _users.ExecuteAsync(() => UserService.GetPagedAsync(1, 20), StateHasChanged);
}
```

### Field-level validation errors

```razor
<input @bind="model.Email" class="@(result.HasFieldError("Email") ? "is-invalid" : "")" />
@foreach (var msg in result.ErrorsForField("Email"))
{
    <div class="invalid-feedback">@msg</div>
}
```

### Utility helpers

```csharp
// Safe default without null checks in markup
var displayName = result.ValueOrDefault(u => u.DisplayName, "Anonymous");

// CSS class for feedback banners
var cssClass = result.ToCssClass("alert-success", "alert-danger");
```

---

## PagedResult

```csharp
// Producing a paged result in your service
public async Task<Result<PagedResult<UserDto>>> GetPagedAsync(int page, int size)
{
    return await Result.TryAsync(async () =>
    {
        var (items, total) = await _repo.GetPagedAsync(page, size);
        return PagedResult<UserDto>.Create(
            items:      _mapper.Map<List<UserDto>>(items),
            totalCount: total,
            pageNumber: page,
            pageSize:   size);
    });
}

// Metadata is built in
result.Value.TotalPages       // derived
result.Value.HasNextPage      // bool
result.Value.HasPreviousPage  // bool
result.Value.IsEmpty          // bool
```

---

## Migrating from OutputHandler

Add the shim once — migrate service by service, no big bang refactor required.

```csharp
using ResultKit.Migration;

// Old service still returns OutputHandler? Bridge it at the call site:
var output = await _legacyService.GetUserAsync(id);
var result = OutputHandlerShim.ToResult(output.IsErrorOccured, output.Message, output.Result);

// New services return Result<T> directly — consumers calling the old contract:
var (isError, message, value) = newResult.ToOutputHandlerTuple();
```

---

## ErrorDetail

```csharp
// General
ErrorDetail.Create("USER_LOCKED", "Account is locked.");

// Validation (field-scoped)
ErrorDetail.Validation("Email",    "Must be a valid email address.");
ErrorDetail.Validation("Password", "Must be at least 8 characters.");

// Semantic shorthand
ErrorDetail.NotFound("User", id.ToString());
ErrorDetail.Unauthorized();
ErrorDetail.Forbidden("Only admins can perform this action.");
```

---

## Deconstruct / pattern matching

```csharp
var (isSuccess, value, message) = await _service.GetUserAsync(id);

var response = await _service.GetUserAsync(id) switch
{
    { IsSuccess: true,  Value: var u }   => Ok(u),
    { Status: ResultStatus.NotFound }    => NotFound(),
    { Status: ResultStatus.Forbidden }   => Forbid(),
    var r                                => StatusCode(500, r.Message)
};
```

---

## License

MIT © 2026
