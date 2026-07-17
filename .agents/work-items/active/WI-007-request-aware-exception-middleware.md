# WI-007: Request-Aware Exception Middleware

Last verified: 2026-07-14  
Status: Active

## Goal

Upgrade `CustomExceptionMiddleware` so it chooses an HTML error page or a structured JSON error payload based on the caller's expected response type, while preserving correct status codes and never converting unexpected failures into authorization responses.

## Verified Current State

- `PresentationLayer/Middleware/CustomExceptionMiddleware.cs` centrally handles application exceptions.
- The middleware is currently oriented toward full-page HTML handling and cannot reliably serve AJAX/fetch callers that expect JSON.
- JSON-returning Razor Page handlers therefore need local defensive catches for `EntityNotFoundException` and `EntityConstraintException`.
- Some existing handlers have historically used broad catches or returned authorization responses for unrelated failures; that behavior must not be copied.
- Frozen sprint contracts distinguish `400`, `404`, and `409` JSON outcomes for expected request, absence, and state-conflict failures.

Implementation anchors to inspect before work:

- `PresentationLayer/Middleware/CustomExceptionMiddleware.cs`
- `PresentationLayer/Program.cs`
- JSON-returning handlers under `PresentationLayer/Pages/**`
- `Domain/Exceptions/EntityNotFoundException.cs`
- `Domain/Exceptions/EntityConstraintException.cs`
- current error page and problem-details configuration
- integration tests for middleware and Razor Page handlers

## Temporary Convention

Until this work item is complete:

- JSON-returning handlers catch `EntityNotFoundException` and return their documented `404` JSON response.
- JSON-returning handlers catch `EntityConstraintException` and return their documented `400` or `409` JSON response.
- HTML-returning handlers may rely on `CustomExceptionMiddleware`.
- Catches remain narrow; cancellation and unexpected exceptions propagate.
- No handler may catch all exceptions and translate them into `Unauthorized()` or `Forbid()`.

## Required Request Classification

Design one centralized classifier used by the middleware. Classification should consider multiple signals rather than trusting one header:

1. **Endpoint metadata**
   - Prefer explicit metadata or an attribute indicating JSON/API response semantics.
   - This is the strongest signal when present.

2. **Accepted response media types**
   - Inspect `Accept`.
   - Treat `application/json`, `application/problem+json`, and compatible `+json` types as JSON preference.
   - Respect quality values where practical.

3. **AJAX/fetch signals**
   - Inspect `X-Requested-With: XMLHttpRequest`.
   - Treat this only as a supporting heuristic because `fetch` does not add it automatically.

4. **Request content type**
   - JSON request bodies can support classification but do not alone prove that the response should be JSON.

5. **Endpoint/handler behavior**
   - Razor Page handlers explicitly documented to return JSON should be classifiable without relying on route-name guessing.
   - Avoid broad heuristics such as “all non-GET requests are JSON.”

6. **Fallback**
   - Normal browser navigation and HTML-preferring requests receive the full error page.
   - Ambiguous callers should follow an explicitly documented default.

## Structured JSON Response

Use one consistent payload, preferably ASP.NET Core `ProblemDetails` or `ValidationProblemDetails`.

Required properties:

- HTTP status;
- stable machine-readable title/type;
- safe user-facing detail;
- request trace identifier;
- optional structured constraint errors when the exception exposes safe details.

Do not expose stack traces, SQL, connection information, internal file paths, secrets, provider credentials, or raw unexpected exception messages in production.

## Exception Mapping

| Exception | Status | JSON behavior | HTML behavior |
|---|---:|---|---|
| missing/invalid resource locator `BadRequestException` | 400 | JSON problem payload | Error page |
| no matching resource `EntityNotFoundException` | 404 | JSON problem payload | Not-found/error page |
| invalid data payload `EntityValidationException` | 400 | JSON problem payload | Error page |
| state/concurrency `EntityConflictException` | 409 | JSON problem payload | Error page |
| authentication failure | 401 | authentication-appropriate JSON or challenge | login/challenge behavior |
| authorization failure | 403 | authorization-appropriate JSON | access-denied behavior |
| request-abort cancellation | no replacement response after abort | stop processing | stop processing |
| unexpected exception | 500 | generic JSON problem payload | generic error page |

## Remaining Work

1. Inspect the middleware, exception types, error pages, authentication pipeline, and current handler patterns.
2. Inventory JSON-returning handlers and identify their current exception mappings.
3. Lock the request-classifier precedence and explicit metadata mechanism.
4. Write failing middleware integration tests covering HTML and JSON callers.
5. Implement request classification as a focused service/helper.
6. Implement `ProblemDetails` serialization with correct content type and status.
7. Preserve HTML redirect/error-page behavior for normal navigation.
8. Verify authentication, authorization, cancellation, response-started, and production-detail behavior.
9. Migrate JSON handlers away from duplicate local catches only after middleware behavior is verified and endpoint contracts remain unchanged.
10. Remove the temporary local-catch convention from `AGENTS.md` when migration is complete.

## Acceptance Criteria

- Normal browser navigation receives the intended HTML not-found/error experience.
- A caller accepting JSON receives a structured JSON problem response with status `404`.
- `EntityConstraintException` produces the endpoint-approved `400` or `409`.
- AJAX/fetch-style requests receive JSON when classified as JSON callers.
- A JSON request body alone does not force JSON for an HTML form flow.
- Unexpected exceptions produce safe `500` responses appropriate to caller type.
- Production responses do not expose stack traces or sensitive details.
- The middleware does not write a second response after the response has started.
- Request-abort cancellation is not logged or transformed as an application failure.
- Existing HTML error pages continue working.
- Focused integration tests and the applicable full suite pass.

## Verification Commands

```powershell
dotnet test UnitTests/UnitTests.csproj --no-restore --filter "FullyQualifiedName~CustomExceptionMiddleware|FullyQualifiedName~ExceptionHandling"
dotnet build EduChatAI.slnx --no-restore
dotnet test EduChatAI.slnx --no-build --no-restore
git diff --check
```

## Hazards And Boundaries

- Do not infer JSON solely from HTTP method.
- Do not infer JSON solely from a route containing `api`.
- Do not map all `EntityConstraintException` instances to one status without checking semantics.
- Do not replace authentication challenge/forbid behavior accidentally.
- Do not swallow cancellation.
- Do not remove local handler catches until middleware tests prove equivalent behavior.
- Do not broaden this into rewriting every error page or changing frontend architecture.

## Resume Prompt

Inspect `CustomExceptionMiddleware`, the two entity exception types, current ProblemDetails/error-page setup, and representative HTML and JSON Razor Page handlers. Propose the exact classifier precedence and the `400` versus `409` mechanism before implementation.
