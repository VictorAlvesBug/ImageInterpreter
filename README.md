# ImageInterpreter

ImageInterpreter is an HTTP API that accepts one image and returns a generic interpretation: a short summary, the objects it found, any text it read, and confidence scores. Version 1 is stateless. The data model is the interpretation JSON document. There is no database, and the vision provider stays behind a port. No vendor is selected in this repository.

## What the API does

The product operation is:

```http
POST /api/v1/interpretations
Authorization: Bearer <access-token>
Content-Type: multipart/form-data
```

The token's `scope` claim must include `interpretations:write`. The body contains one file part named `image`. The file is JPEG, PNG, or WebP, and it is at most 10 MB.

A successful response is `200` with a JSON document:

```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "summary": "A red bicycle leaning against a brick wall.",
  "objects": [
    {
      "label": "bicycle",
      "confidence": 0.94,
      "description": "A red bicycle in the foreground."
    }
  ],
  "extractedText": [
    {
      "text": "MAIN ST",
      "confidence": 0.81
    }
  ],
  "confidence": 0.9,
  "model": {
    "provider": "provider",
    "name": "model"
  }
}
```

`id` is a correlation UUID. `objects` entries carry `label`, `confidence`, and `description`. `extractedText` entries carry `text` and `confidence`. The top-level `confidence` is the overall score, from 0 to 1. `model` is metadata (`provider` and `name`) reported with the result. The sample `provider` and `name` above are placeholders. This repository does not choose a vision vendor.

The API can respond with `400`, `401`, `403`, `413`, `415`, `422`, or `503`. Problem bodies use [RFC 7807](https://www.rfc-editor.org/rfc/rfc7807). `401` means the bearer token is missing, invalid, or expired. `403` means the token is authentic and does not include `interpretations:write`. The API contract specifies when each code is returned.

### Authentication

The API is a JWT Bearer resource server using RS256. It validates issuer, audience, signature, and expiry. It does not register users and it does not issue refresh tokens. Callers obtain tokens from an external issuer.

The product requirement is [docs/specs/decisions/2026-11-05-authentication.md](docs/specs/decisions/2026-11-05-authentication.md) (2026-11-05). The mechanism, and the rejection of opaque API keys and session cookies, is [docs/adr/0001-use-jwt-authentication.md](docs/adr/0001-use-jwt-authentication.md).

## Current status

`ImageInterpreter/` is still the ASP.NET Core 10 WeatherForecast template. `Program.cs` registers controllers and OpenAPI, and the WeatherForecast controller is still present. That template is not the product. The interpretation endpoint, the layer split, and JWT validation are specified and are not implemented in this tree yet.

## Documentation map

Read the specs in order, then the authentication decision, then the ADR.

| Document | What it defines |
| --- | --- |
| [docs/specs/000-overview.md](docs/specs/000-overview.md) | Purpose, the .NET 10 stack, and how the rest of the docs fit together. |
| [docs/specs/001-domain-model.md](docs/specs/001-domain-model.md) | `Interpretation` and related domain values. |
| [docs/specs/002-user-stories.md](docs/specs/002-user-stories.md) | User stories (`US-00N`). |
| [docs/specs/003-api-contracts.md](docs/specs/003-api-contracts.md) | `POST /api/v1/interpretations`, status codes, and RFC 7807 problems. |
| [docs/specs/004-data-model.md](docs/specs/004-data-model.md) | The interpretation JSON document. v1 stores nothing else. |
| [docs/specs/005-acceptance-criteria.md](docs/specs/005-acceptance-criteria.md) | Given/When/Then criteria traced to the stories. |
| [docs/specs/decisions/2026-11-05-authentication.md](docs/specs/decisions/2026-11-05-authentication.md) | Product decision of 2026-11-05: every interpretation requires a JWT; resource-server role; `interpretations:write`. |
| [docs/adr/0001-use-jwt-authentication.md](docs/adr/0001-use-jwt-authentication.md) | ADR 0001: JWT Bearer RS256, the checks the API performs, and the rejected alternatives. |

Cursor rules for agents working in this repo:

| Rule | Scope |
| --- | --- |
| [.cursor/rules/architecture.mdc](.cursor/rules/architecture.mdc) | Always applied. Layers, the vision port, specs as the source of truth, and no secrets in the repo. |
| [.cursor/rules/api-conventions.mdc](.cursor/rules/api-conventions.mdc) | Versioned `/api/v1` routes, the status codes above, stable JSON, and RFC 7807. |
| [.cursor/rules/testing.mdc](.cursor/rules/testing.mdc) | xUnit, one acceptance criterion id (`AC-00N`) per test, and a fake of the vision port. |

[AGENTS.md](AGENTS.md) is the working guide for those boundaries and for `dotnet build` and `dotnet test`.

## Run the project today

The runnable project is the template. From the `ImageInterpreter` directory:

```text
dotnet run
```

Launch URLs are in `ImageInterpreter/Properties/launchSettings.json`. The `http` profile uses `http://localhost:5077`. The `https` profile uses `https://localhost:7062` and `http://localhost:5077`. In Development the template maps its OpenAPI document. The weather endpoint that answers is the template endpoint, and it is not the interpretation API.
