# ImageInterpreter

ImageInterpreter is a .NET 10 ASP.NET Core API. A client sends one image and receives a generic interpretation document: a summary, detected objects, extracted text, and confidence. Version 1 is stateless. The data model is that JSON document. There is no database.

The vision provider stays behind the `IVisionInterpreter` port. These documents do not select a vendor. Infrastructure is the only code that calls a model.

Every interpretation call requires a JWT. The API is a resource server: it validates bearer tokens and does not register users or issue refresh tokens.

## Purpose

The product operation is `POST /api/v1/interpretations`.

- The caller sends `Authorization: Bearer` and a token whose `scope` claim includes `interpretations:write`.
- The body is `multipart/form-data` with one image file (`image`): JPEG, PNG, or WebP, up to 10 MB.
- A `200` response returns a correlation `id` (UUID), `summary`, `objects[]` (`label`, `confidence`, `description`), `extractedText[]` (`text`, `confidence`), an overall `confidence` from 0 to 1, and model metadata (`provider`, `name`).
- Failures use `400`, `401`, `403`, `413`, `415`, `422`, and `503`. Problem responses follow RFC 7807.

The specs below are the source of truth for behavior. The WeatherForecast template still in `ImageInterpreter/` is outside the product.

## Spec reading order

Read the specifications in this order, then the authentication decision, then the architecture decision record:

1. [docs/specs/000-overview.md](docs/specs/000-overview.md) — purpose, stack, and how the other specs fit together.
2. [docs/specs/001-domain-model.md](docs/specs/001-domain-model.md) — `Interpretation` and the values it owns.
3. [docs/specs/002-user-stories.md](docs/specs/002-user-stories.md) — user stories (`US-00N`).
4. [docs/specs/003-api-contracts.md](docs/specs/003-api-contracts.md) — the HTTP contract, including status codes and problem bodies.
5. [docs/specs/004-data-model.md](docs/specs/004-data-model.md) — the interpretation JSON document.
6. [docs/specs/005-acceptance-criteria.md](docs/specs/005-acceptance-criteria.md) — Given/When/Then criteria traced to the stories.
7. [docs/specs/decisions/2026-11-05-authentication.md](docs/specs/decisions/2026-11-05-authentication.md) — what the product requires of every interpretation call.
8. [docs/adr/0001-use-jwt-authentication.md](docs/adr/0001-use-jwt-authentication.md) — why the mechanism is JWT Bearer with RS256, and what that costs.

The product decision states the requirement. The ADR states the mechanism: RS256, validating issuer, audience, signature, and expiry. Opaque API keys and session cookies were rejected.

## Layer boundaries

Dependencies point inward. The HTTP contract and the model SDK stay at the edges.

| Layer | Responsibility |
| --- | --- |
| Api | JWT authentication, file validation, and the versioned HTTP contract. |
| Application | The `InterpretImage` use case. It depends on the domain and on `IVisionInterpreter`. |
| Domain | `Interpretation`, detected objects, extracted text, and confidence. No ASP.NET types and no model-SDK types. |
| Infrastructure | The only LLM caller. It implements `IVisionInterpreter`. Swapping the provider leaves the HTTP contract unchanged. |

```text
Api → Application (InterpretImage) → Domain (Interpretation)
Application → IVisionInterpreter ← Infrastructure
```

Domain code refers to interpretation values only. It does not take `IFormFile`, `HttpContext`, `ControllerBase`, or a vendor client. Application asks the port for an interpretation. The adapter in Infrastructure is the translation point between that port and a model SDK.

## Commands

From the repository root (the directory that contains `ImageInterpreter.slnx`):

```text
dotnet build
dotnet test
```

`dotnet build` compiles the solution. `dotnet test` runs the xUnit suite. Name each test with the acceptance-criterion id it covers (`AC-00N`), and reach the model only through a fake of `IVisionInterpreter`.

## Behavior changes

A change in behavior updates the spec first. Adjust the overview, domain model, stories, API contract, data model, or acceptance criteria — whichever documents own the rule — and then change code so it matches. A story or criterion that the code does not implement is unfinished work. Code that the specs do not describe is out of scope.

Authentication requirements change in [docs/specs/decisions/2026-11-05-authentication.md](docs/specs/decisions/2026-11-05-authentication.md). The token mechanism changes in [docs/adr/0001-use-jwt-authentication.md](docs/adr/0001-use-jwt-authentication.md).

## Further reading

- [.cursor/rules/](.cursor/rules/) — `architecture.mdc` (always applied), `api-conventions.mdc`, and `testing.mdc`.
- [docs/adr/0001-use-jwt-authentication.md](docs/adr/0001-use-jwt-authentication.md) — JWT Bearer RS256 for the resource server.
