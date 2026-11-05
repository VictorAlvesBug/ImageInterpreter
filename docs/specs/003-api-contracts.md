# Specification 003 — API contracts

| | |
| --- | --- |
| Product | ImageInterpreter |
| Status | Active |
| Version | v1 |
| Date | 2026-11-05 |

This file is the HTTP contract for v1. The success body is the interpretation document defined in [004-data-model.md](004-data-model.md). The domain behind that document is [001-domain-model.md](001-domain-model.md).

Normative references: [RFC 7807](https://www.rfc-editor.org/rfc/rfc7807) (problem details), [RFC 4122](https://www.rfc-editor.org/rfc/rfc4122) (UUID), [RFC 7578](https://www.rfc-editor.org/rfc/rfc7578) (multipart form data), [RFC 6750](https://www.rfc-editor.org/rfc/rfc6750) (`Authorization: Bearer`), [RFC 9068](https://www.rfc-editor.org/rfc/rfc9068) (`scope` as a space-delimited string).

## Operation

```http
POST /api/v1/interpretations
```

This is the only product operation. The server accepts one image and returns one interpretation document. The call is not idempotent: each success carries a new `id`.

| Item | Rule |
| --- | --- |
| Method and path | `POST /api/v1/interpretations` |
| Request content type | `multipart/form-data` with a boundary parameter |
| Authorization | `Authorization: Bearer <token>`, scope `interpretations:write` |
| Success | `200` and `Content-Type: application/json` |
| Success body | One interpretation document. The document is the JSON root. It is not wrapped in another object. |
| Errors | `400`, `401`, `403`, `413`, `415`, `422`, `503` as problem details |

No query parameter is defined. Query parameters are ignored.

## Authentication and authorization

Every call to this operation requires a bearer token. The product requirement is [decisions/2026-11-05-authentication.md](decisions/2026-11-05-authentication.md). The mechanism is [ADR 0001](../adr/0001-use-jwt-authentication.md). This section defines the header, the `scope` wire format, and the status codes. It does not restate the token decision.

### Header

The client sends:

```http
Authorization: Bearer <token>
```

The scheme is `Bearer` (case-insensitive). One space separates the scheme from the token. A missing header, an empty token, or a scheme other than `Bearer` fails authentication.

### `scope` wire format

The `scope` claim is a JSON string. Scope tokens inside that string are separated by the space character U+0020. Several spaces in a row are one separator. Empty tokens are ignored.

The call is authorized when at least one token equals `interpretations:write` by ordinal, case-sensitive comparison. Further tokens are allowed.

| Claim value | Result when the token is otherwise authentic |
| --- | --- |
| `"interpretations:write"` | Authorized |
| `"images:read interpretations:write"` | Authorized |
| `"interpretations:WRITE"` | `403` |
| `""`, absent, or a JSON type other than string | `403` |

### Status mapping

Checks run in this order once the request reaches the operation: authentication, then authorization, then the body rules below.

| Situation | Status |
| --- | --- |
| The header is missing, the scheme is not `Bearer`, or the token is not accepted under the authentication decision and ADR 0001 | `401` |
| The token is accepted and `scope` does not include `interpretations:write` | `403` |

A token that fails authentication is `401` even when it also lacks the scope. `401` and `403` problem bodies use the catalog in this file.

`WWW-Authenticate` on those responses:

| Status | `WWW-Authenticate` |
| --- | --- |
| `401` because the header is missing or the scheme is not `Bearer` | `Bearer` |
| `401` because the bearer token was not accepted | `Bearer error="invalid_token"` |
| `403` | A `Bearer` challenge that includes `error="insufficient_scope"` and `scope="interpretations:write"` |

## Request body

The body is `multipart/form-data` containing exactly one file part. That part's field name is `image`.

| Rule | Result |
| --- | --- |
| The request `Content-Type` is missing or is not `multipart/form-data` | `400` |
| The `image` file part is absent, or the `image` part is not a file | `400` |
| The request contains any file part other than the single part named `image`, including a second part named `image` | `400` |
| The `image` part has length 0 | `400` |
| The `image` part is longer than 10,485,760 bytes | `413` |
| The media type rules below reject the part | `415` |
| The part passes those rules and the interpreter reports it uninterpretable | `422` |

Additional non-file form fields are ignored, whatever their names and values. The file name of the `image` part is ignored when deciding the media type. A `Content-Transfer-Encoding` of `binary`, `8bit`, `7bit`, or absent is the expected encoding of the part. The bytes that count toward the size limit are the part's file content after that decoding.

The interpreter is invoked only after authentication, authorization, and these file rules succeed.

### Size

10 MB in this contract means **10,485,760 bytes** (10 × 1024 × 1024).

- An `image` part of exactly 10,485,760 bytes is within the limit.
- An `image` part of 10,485,761 bytes or more is `413`.
- Multipart boundaries and part headers are not part of that count.
- A request body larger than 10,485,760 bytes plus 65,536 bytes of multipart overhead may be rejected with `413` while it is being read, so the server does not have to buffer an unbounded body.
- The `413` detail string is `The image is larger than 10 MB.`

### Media types and signatures

The resolved media type must be one of `image/jpeg`, `image/png`, or `image/webp`. Comparison of the type and subtype is case-insensitive. Parameters on the part `Content-Type`, such as `charset`, are ignored.

Signatures:

| Media type | Signature |
| --- | --- |
| `image/jpeg` | The first three bytes are `FF D8 FF`. |
| `image/png` | The first eight bytes are `89 50 4E 47 0D 0A 1A 0A`. |
| `image/webp` | The first four bytes are `52 49 46 46` (`RIFF`) and bytes 8 through 11 are `57 45 42 50` (`WEBP`). |

Resolution:

1. If the part `Content-Type` is absent, empty, or `application/octet-stream`, classify the part by its signature. A signature that matches exactly one row above is that media type. Otherwise the result is `415`.
2. If the part `Content-Type` is `image/jpeg`, `image/png`, or `image/webp`, the signature must match that same row. A match resolves to that media type. A mismatch or a file too short to hold the signature is `415`.
3. Any other part `Content-Type`, including `image/jpg`, `image/gif`, and `image/pjpeg`, is `415`.

Api checks the signature. It does not need to fully decode the image. A part that passes the signature and that `IVisionInterpreter` cannot interpret is `422`.

## Success

| Item | Value |
| --- | --- |
| Status | `200` |
| `Content-Type` | `application/json` (a `charset=utf-8` parameter is allowed) |
| Body | The interpretation document from spec 004 |

`id` is a UUID serialized in lowercase hexadecimal with hyphens (`8-4-4-4-12`). Application assigns a new `id` for every success. The sample `id` in the example below is an illustration of that form.

`confidence`, each `objects[].confidence`, and each `extractedText[].confidence` is a JSON number from 0 to 1 inclusive. Integer `0` and `1` are valid JSON numbers on that scale.

Property names are case-sensitive. Clients must not depend on property order. The schema in spec 004 allows no additional properties.

### Sample document

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

`provider` and `name` in the sample are placeholders. v1 does not choose a vision vendor.

## Problem details

Every error response listed in this contract uses `Content-Type: application/problem+json` (a `charset=utf-8` parameter is allowed) and a JSON object with these members:

| Member | Rule |
| --- | --- |
| `type` | Stable URI from the catalog below. |
| `title` | Stable title from the catalog. |
| `status` | Number equal to the HTTP status code. |
| `detail` | Exact detail string from the catalog for that situation. |
| `instance` | Request path. For this operation, `/api/v1/interpretations`. |

The problem object is the JSON root. It does not include `id`, `summary`, `objects`, `extractedText`, `confidence`, or `model`. `detail` must not contain the bearer token, the image bytes, a stack trace, or a raw provider payload.

### Catalog

| HTTP | When | `type` | `title` | `detail` |
| --- | --- | --- | --- | --- |
| `400` | `Content-Type` is not `multipart/form-data` | `urn:imageinterpreter:problem:invalid-request` | `Bad Request` | `Content-Type must be multipart/form-data.` |
| `400` | `image` is missing or is not a file | `urn:imageinterpreter:problem:invalid-request` | `Bad Request` | `The request must include one file field named image.` |
| `400` | More than one file part, or the only file part is not named `image` | `urn:imageinterpreter:problem:invalid-request` | `Bad Request` | `The request must include exactly one file field named image.` |
| `400` | The `image` part has length 0 | `urn:imageinterpreter:problem:invalid-request` | `Bad Request` | `The image file is empty.` |
| `401` | No `Bearer` credential was presented | `urn:imageinterpreter:problem:unauthorized` | `Unauthorized` | `A bearer token is required.` |
| `401` | A bearer token was presented and was not accepted | `urn:imageinterpreter:problem:unauthorized` | `Unauthorized` | `The bearer token was not accepted.` |
| `403` | The token is accepted and lacks `interpretations:write` | `urn:imageinterpreter:problem:forbidden` | `Forbidden` | `The interpretations:write scope is required.` |
| `413` | The image exceeds 10,485,760 bytes | `urn:imageinterpreter:problem:payload-too-large` | `Payload Too Large` | `The image is larger than 10 MB.` |
| `415` | The media type or signature rules reject the part | `urn:imageinterpreter:problem:unsupported-media-type` | `Unsupported Media Type` | `The image must be JPEG, PNG, or WebP.` |
| `422` | The interpreter reports the image uninterpretable | `urn:imageinterpreter:problem:uninterpretable-image` | `Unprocessable Content` | `The image could not be interpreted.` |
| `503` | The interpreter is unavailable, times out, or returns a result that breaks invariants | `urn:imageinterpreter:problem:interpreter-unavailable` | `Service Unavailable` | `The image interpreter is unavailable.` |
| `405` | A method other than `POST` on `/api/v1/interpretations` | `urn:imageinterpreter:problem:method-not-allowed` | `Method Not Allowed` | `POST is the only method on this resource.` |
| `404` | A request under `/api/v1/interpretations/{id}` | `urn:imageinterpreter:problem:not-found` | `Not Found` | `No interpretation resource is stored.` |

Sample problem:

```json
{
  "type": "urn:imageinterpreter:problem:payload-too-large",
  "title": "Payload Too Large",
  "status": 413,
  "detail": "The image is larger than 10 MB.",
  "instance": "/api/v1/interpretations"
}
```

When several `400` rules match, the server returns the one that appears first in the `400` rows of the catalog.

## Routes that are not the operation

v1 stores nothing, so there is no interpretation resource to read.

| Request | Response |
| --- | --- |
| `GET`, `PUT`, `PATCH`, `DELETE`, or `HEAD` on `/api/v1/interpretations` | `405`, `Allow: POST`, and the method-not-allowed problem |
| Any method on `/api/v1/interpretations/{id}` | `404` and the not-found problem. `instance` is that request path. |

`GET /WeatherForecast` is the template endpoint. It is outside this contract. See [000-overview.md](000-overview.md).

## Application mapping

| After the HTTP checks | Use case | HTTP |
| --- | --- | --- |
| File accepted | `InterpretImage` builds an `Interpretation` | `200` and the document |
| File accepted | Port outcome uninterpretable | `422` |
| File accepted | Port outcome unavailable, or the vision result fails domain construction | `503` |
| Authentication, authorization, or file validation failed | `IVisionInterpreter` is not called | The catalog status for that failure |

OpenAPI, when the host exposes it, must describe this operation with the same path, fields, and status codes. This document remains the contract if the two disagree.

## Related documents

- [000-overview.md](000-overview.md) — purpose, layers, and reading order.
- [002-user-stories.md](002-user-stories.md) — stories for this operation.
- [004-data-model.md](004-data-model.md) — schema of the `200` body.
- [005-acceptance-criteria.md](005-acceptance-criteria.md) — Given/When/Then checks.
- [decisions/2026-11-05-authentication.md](decisions/2026-11-05-authentication.md) — product authentication requirement.
- [../adr/0001-use-jwt-authentication.md](../adr/0001-use-jwt-authentication.md) — token mechanism.
