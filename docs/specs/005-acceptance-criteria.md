# Specification 005 — Acceptance criteria

| | |
| --- | --- |
| Product | ImageInterpreter |
| Status | Active |
| Version | v1 |
| Date | 2026-11-05 |

Each criterion is a Given/When/Then check for `POST /api/v1/interpretations`, and each one traces to a single story in [002-user-stories.md](002-user-stories.md). The request rules, problem catalog, and sample document are in [003-api-contracts.md](003-api-contracts.md). The body schema is in [004-data-model.md](004-data-model.md).

A criterion that depends on the vision provider reaches it only through `IVisionInterpreter`. "The canonical sample" means the bicycle document in specs 003 and 004. Unless a criterion says the response `id` equals that sample, the server assigns a new `id`.

An authorized caller in these criteria is a client the authentication decision accepts whose `scope` string includes `interpretations:write`.

## Criteria

### <a id="ac-001"></a>AC-001 — Returns id, summary, objects, and extracted text

- **Story:** [US-001](002-user-stories.md#us-001)
- **Given** an authorized caller, a JPEG at most 10,485,760 bytes whose signature is `FF D8 FF`, and a vision result equal to the canonical sample's `summary`, `objects`, `extractedText`, `confidence`, and `model`
- **When** the client posts that file as the `image` part of `multipart/form-data` to `POST /api/v1/interpretations`
- **Then** the status is `200`, the content type is `application/json`, and the body is an interpretation document whose `summary`, `objects` (`label`, `confidence`, `description`), `extractedText` (`text`, `confidence`), `confidence`, and `model` (`provider`, `name`) equal the canonical sample, with a lowercase UUID in `id`

### <a id="ac-002"></a>AC-002 — PNG upload returns the same document shape

- **Story:** [US-001](002-user-stories.md#us-001)
- **Given** an authorized caller, a PNG at most 10,485,760 bytes whose signature is `89 50 4E 47 0D 0A 1A 0A` and whose part `Content-Type` is `image/png`, and the same vision result as AC-001
- **When** the client posts that file in the `image` field
- **Then** the response matches the Then of AC-001

### <a id="ac-003"></a>AC-003 — WebP upload returns the same document shape

- **Story:** [US-001](002-user-stories.md#us-001)
- **Given** an authorized caller, a WebP at most 10,485,760 bytes whose signature is `RIFF` at bytes 0–3 and `WEBP` at bytes 8–11 and whose part `Content-Type` is `image/webp`, and the same vision result as AC-001
- **When** the client posts that file in the `image` field
- **Then** the response matches the Then of AC-001

### <a id="ac-004"></a>AC-004 — Empty object and text lists are valid

- **Story:** [US-001](002-user-stories.md#us-001)
- **Given** an authorized caller, a valid JPEG, and a vision result whose `objects` and `extractedText` are both empty, whose `summary` is `A plain gray square.`, whose `confidence` is `0`, and whose `model` is the sample `provider` and `name`
- **When** the client posts the image
- **Then** the status is `200` and the body contains `"objects": []` and `"extractedText": []` together with that `summary`, `confidence`, and `model`

### <a id="ac-005"></a>AC-005 — Description may be empty

- **Story:** [US-001](002-user-stories.md#us-001)
- **Given** an authorized caller, a valid JPEG, and a vision result with one object whose `label` is `square`, whose `confidence` is `1`, and whose `description` is `""`, plus one extracted text span whose `text` is `MAIN ST`
- **When** the client posts the image
- **Then** the status is `200`, that object's `description` is the empty string, `label` and `text` are the non-empty strings above, and the body validates against the schema in spec 004

### <a id="ac-006"></a>AC-006 — Confidence stays on the closed 0 to 1 scale

- **Story:** [US-012](002-user-stories.md#us-012)
- **Given** an authorized caller, a valid JPEG, and a vision result whose root `confidence` is `0`, whose object `confidence` is `1`, and whose extracted-text `confidence` is `0.81`
- **When** the client posts the image
- **Then** the status is `200` and every `confidence` in the body is a JSON number greater than or equal to 0 and less than or equal to 1, including those three values

### <a id="ac-007"></a>AC-007 — A bearer token with the write scope is authorized

- **Story:** [US-002](002-user-stories.md#us-002)
- **Given** a token the authentication decision accepts whose `scope` claim is the string `interpretations:write`, and a valid JPEG the interpreter can interpret
- **When** the client sends `Authorization: Bearer <token>` with that image
- **Then** the status is `200`

### <a id="ac-008"></a>AC-008 — Additional scopes remain authorized

- **Story:** [US-002](002-user-stories.md#us-002)
- **Given** a token the authentication decision accepts whose `scope` claim is the string `images:read interpretations:write`, and a valid JPEG the interpreter can interpret
- **When** the client posts the image with that token
- **Then** the response is `200`

### <a id="ac-009"></a>AC-009 — A missing token is 401

- **Story:** [US-003](002-user-stories.md#us-003)
- **Given** a valid JPEG and no `Authorization` header
- **When** the client posts the image
- **Then** the status is `401`, `WWW-Authenticate` is `Bearer`, and the body is the problem whose `type` is `urn:imageinterpreter:problem:unauthorized`, `title` is `Unauthorized`, `status` is `401`, `detail` is `A bearer token is required.`, and `instance` is `/api/v1/interpretations`

### <a id="ac-010"></a>AC-010 — A rejected bearer token is 401

- **Story:** [US-003](002-user-stories.md#us-003)
- **Given** a valid JPEG and a bearer token the authentication decision and ADR 0001 do not accept
- **When** the client posts the image with `Authorization: Bearer <token>`
- **Then** the status is `401`, `WWW-Authenticate` contains `error="invalid_token"`, and the problem `detail` is `The bearer token was not accepted.` with the unauthorized `type` and `title`

### <a id="ac-011"></a>AC-011 — Failed authentication stays 401 when the scope is also absent

- **Story:** [US-003](002-user-stories.md#us-003)
- **Given** a bearer token that is not accepted and that also lacks `interpretations:write`
- **When** the client posts a valid JPEG
- **Then** the status is `401` and the problem `type` is `urn:imageinterpreter:problem:unauthorized`

### <a id="ac-012"></a>AC-012 — An authentic token without the scope is 403

- **Story:** [US-004](002-user-stories.md#us-004)
- **Given** a token the authentication decision accepts whose `scope` is `images:read`
- **When** the client posts a valid JPEG
- **Then** the status is `403`, `WWW-Authenticate` includes `error="insufficient_scope"` and `scope="interpretations:write"`, and the problem `type` is `urn:imageinterpreter:problem:forbidden`, `title` is `Forbidden`, `status` is `403`, and `detail` is `The interpretations:write scope is required.`

### <a id="ac-013"></a>AC-013 — A non-multipart body is 400

- **Story:** [US-005](002-user-stories.md#us-005)
- **Given** an authorized caller and a request whose `Content-Type` is `application/json`
- **When** the client posts that body to `/api/v1/interpretations`
- **Then** the status is `400` and the problem `detail` is `Content-Type must be multipart/form-data.` with `type` `urn:imageinterpreter:problem:invalid-request` and `title` `Bad Request`

### <a id="ac-014"></a>AC-014 — A missing image field is 400

- **Story:** [US-005](002-user-stories.md#us-005)
- **Given** an authorized caller and a `multipart/form-data` body with no part named `image`
- **When** the client posts it
- **Then** the status is `400` and the problem `detail` is `The request must include one file field named image.`

### <a id="ac-015"></a>AC-015 — More than one file is 400

- **Story:** [US-005](002-user-stories.md#us-005)
- **Given** an authorized caller and a multipart body with two file parts, either both named `image` or one named `image` and one named something else
- **When** the client posts it
- **Then** the status is `400` and the problem `detail` is `The request must include exactly one file field named image.`

### <a id="ac-016"></a>AC-016 — An empty image is 400

- **Story:** [US-005](002-user-stories.md#us-005)
- **Given** an authorized caller and a multipart body whose only file part is named `image` and has length 0
- **When** the client posts it
- **Then** the status is `400` and the problem `detail` is `The image file is empty.`

### <a id="ac-017"></a>AC-017 — An image of exactly 10,485,760 bytes is within the limit

- **Story:** [US-006](002-user-stories.md#us-006)
- **Given** an authorized caller and a JPEG of exactly 10,485,760 bytes that passes the JPEG signature rule and that the interpreter can interpret
- **When** the client posts it in the `image` field
- **Then** the status is `200`

### <a id="ac-018"></a>AC-018 — One byte over the limit is 413

- **Story:** [US-006](002-user-stories.md#us-006)
- **Given** an authorized caller and an `image` part of 10,485,761 bytes
- **When** the client posts it
- **Then** the status is `413` and the body is the problem whose `type` is `urn:imageinterpreter:problem:payload-too-large`, `title` is `Payload Too Large`, `status` is `413`, and `detail` is `The image is larger than 10 MB.`

### <a id="ac-019"></a>AC-019 — GIF and image/jpg are unsupported

- **Story:** [US-007](002-user-stories.md#us-007)
- **Given** an authorized caller and a file part named `image` whose `Content-Type` is `image/gif`, and a second request whose `Content-Type` is `image/jpg`
- **When** the client posts each request
- **Then** each response is `415` with `type` `urn:imageinterpreter:problem:unsupported-media-type`, `title` `Unsupported Media Type`, and `detail` `The image must be JPEG, PNG, or WebP.`

### <a id="ac-020"></a>AC-020 — A declared type that disagrees with the signature is 415

- **Story:** [US-007](002-user-stories.md#us-007)
- **Given** an authorized caller and a part named `image` whose `Content-Type` is `image/jpeg` and whose bytes are a PNG signature
- **When** the client posts it
- **Then** the status is `415` with the unsupported-media-type problem, and `IVisionInterpreter` is not called

### <a id="ac-021"></a>AC-021 — A missing type with a PNG signature is accepted as PNG

- **Story:** [US-007](002-user-stories.md#us-007)
- **Given** an authorized caller, a PNG the interpreter can interpret, and a part named `image` whose `Content-Type` is absent or is `application/octet-stream`
- **When** the client posts it
- **Then** the status is `200` and the body is an interpretation document

### <a id="ac-022"></a>AC-022 — The file extension does not decide the type

- **Story:** [US-007](002-user-stories.md#us-007)
- **Given** an authorized caller and a part named `image` with filename `photo.jpg`, `Content-Type` `image/gif`, and a GIF signature
- **When** the client posts it
- **Then** the status is `415` with the unsupported-media-type problem

### <a id="ac-023"></a>AC-023 — A truncated JPEG that the interpreter declines is 422

- **Story:** [US-008](002-user-stories.md#us-008)
- **Given** an authorized caller, a part named `image` with `Content-Type` `image/jpeg` and a signature of `FF D8 FF` whose remaining bytes are not a complete image, and a port outcome of uninterpretable
- **When** the client posts it
- **Then** the status is `422` and the problem `type` is `urn:imageinterpreter:problem:uninterpretable-image`

### <a id="ac-024"></a>AC-024 — An uninterpretable outcome is a problem, not a partial document

- **Story:** [US-008](002-user-stories.md#us-008)
- **Given** an authorized caller, a valid JPEG, and a port outcome of uninterpretable
- **When** the client posts the image
- **Then** the status is `422`, the content type is `application/problem+json`, the `title` is `Unprocessable Content`, the `detail` is `The image could not be interpreted.`, and the body does not contain `summary`, `objects`, or `extractedText`

### <a id="ac-025"></a>AC-025 — An unavailable interpreter is 503

- **Story:** [US-009](002-user-stories.md#us-009)
- **Given** an authorized caller, a valid JPEG, and a port outcome of unavailable or a port call that times out
- **When** the client posts the image
- **Then** the status is `503` and the problem `type` is `urn:imageinterpreter:problem:interpreter-unavailable`, `title` is `Service Unavailable`, and `detail` is `The image interpreter is unavailable.`

### <a id="ac-026"></a>AC-026 — A result that breaks invariants is 503

- **Story:** [US-009](002-user-stories.md#us-009)
- **Given** an authorized caller, a valid JPEG, and a vision result whose root `confidence` is `1.1` or whose `summary` is only whitespace
- **When** the client posts the image
- **Then** the status is `503` with the interpreter-unavailable problem, and the body is not an interpretation document

### <a id="ac-027"></a>AC-027 — The 503 detail does not leak provider internals

- **Story:** [US-009](002-user-stories.md#us-009)
- **Given** an authorized caller and a port failure that carries a raw provider message, a credential, or a stack trace
- **When** the client posts a valid JPEG
- **Then** the `503` body `detail` is `The image interpreter is unavailable.` and the body contains neither the bearer token, nor the image bytes, nor that raw provider text

### <a id="ac-028"></a>AC-028 — Errors use the problem catalog

- **Story:** [US-010](002-user-stories.md#us-010)
- **Given** a request that produces each of `400`, `401`, `403`, `413`, `415`, `422`, and `503`
- **When** the client reads each response
- **Then** each content type is `application/problem+json`, each body is a JSON object whose `type`, `title`, `status`, and `detail` match the catalog row in spec 003 for that situation, `status` equals the HTTP status, and `instance` is `/api/v1/interpretations`

### <a id="ac-029"></a>AC-029 — Each success id is a new lowercase UUID

- **Story:** [US-011](002-user-stories.md#us-011)
- **Given** an authorized caller and two posts of the same valid JPEG with the same vision result
- **When** the client compares the two `200` bodies
- **Then** each `id` matches `^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$` and the two `id` values differ

### <a id="ac-030"></a>AC-030 — The id cannot be fetched later

- **Story:** [US-011](002-user-stories.md#us-011)
- **Given** a successful interpretation whose `id` is known
- **When** the client sends `GET`, `PUT`, `PATCH`, `DELETE`, or `HEAD` to `/api/v1/interpretations`
- **Then** the status is `405`, the `Allow` header is `POST`, and the problem `type` is `urn:imageinterpreter:problem:method-not-allowed`
- **And when** the client sends `GET /api/v1/interpretations/{id}` using that `id`
- **Then** the status is `404` and the problem `type` is `urn:imageinterpreter:problem:not-found` with `detail` `No interpretation resource is stored.`

### <a id="ac-031"></a>AC-031 — Repeating the upload does not create a stored resource

- **Story:** [US-011](002-user-stories.md#us-011)
- **Given** an authorized caller and a valid JPEG
- **When** the client posts it twice
- **Then** both responses are `200` with different `id` values, and `GET /api/v1/interpretations/{id}` for each `id` is `404`

### <a id="ac-032"></a>AC-032 — Model metadata is provider and name

- **Story:** [US-001](002-user-stories.md#us-001)
- **Given** an authorized caller, a valid JPEG, and a vision result whose `model.provider` is `provider` and whose `model.name` is `model`
- **When** the client posts the image
- **Then** the `200` body `model` object has exactly the properties `provider` and `name`, with those values

### <a id="ac-033"></a>AC-033 — Extra non-file fields are ignored

- **Story:** [US-005](002-user-stories.md#us-005)
- **Given** an authorized caller, a valid JPEG in the `image` file part, and an additional text field named `note`
- **When** the client posts the multipart body
- **Then** the status is `200` and the interpretation document is unchanged by the text field

### <a id="ac-034"></a>AC-034 — 401 challenges use the Bearer scheme

- **Story:** [US-003](002-user-stories.md#us-003)
- **Given** the missing-token case from AC-009 and the rejected-token case from AC-010
- **When** the client reads `WWW-Authenticate`
- **Then** the missing-token value is `Bearer`, and the rejected-token value includes the scheme `Bearer` and `error="invalid_token"`

### <a id="ac-035"></a>AC-035 — Validation failures do not call the interpreter

- **Story:** [US-009](002-user-stories.md#us-009)
- **Given** requests that the contract rejects with `400`, `401`, `403`, `413`, or `415`
- **When** each request is handled
- **Then** `IVisionInterpreter` is not called

## Traceability

| Criterion | Story | Asserts |
| --- | --- | --- |
| AC-001 | US-001 | JPEG `200` with `id`, `summary`, `objects`, `extractedText`, `confidence`, and `model` |
| AC-002 | US-001 | PNG `200` with the same shape |
| AC-003 | US-001 | WebP `200` with the same shape |
| AC-004 | US-001 | Empty `objects` and `extractedText` |
| AC-005 | US-001 | Empty `description` |
| AC-006 | US-012 | `confidence` from 0 to 1 inclusive |
| AC-007 | US-002 | Scope `interpretations:write` |
| AC-008 | US-002 | Scope among other scopes |
| AC-009 | US-003 | Missing credential `401` |
| AC-010 | US-003 | Rejected token `401` |
| AC-011 | US-003 | Authentication before authorization |
| AC-012 | US-004 | Missing scope `403` |
| AC-013 | US-005 | Non-multipart `400` |
| AC-014 | US-005 | Missing `image` `400` |
| AC-015 | US-005 | Extra file `400` |
| AC-016 | US-005 | Empty file `400` |
| AC-017 | US-006 | Exactly 10,485,760 bytes allowed |
| AC-018 | US-006 | 10,485,761 bytes `413` |
| AC-019 | US-007 | `image/gif` and `image/jpg` `415` |
| AC-020 | US-007 | Type and signature mismatch `415` |
| AC-021 | US-007 | Sniffed PNG `200` |
| AC-022 | US-007 | Extension ignored |
| AC-023 | US-008 | Truncated JPEG `422` |
| AC-024 | US-008 | Uninterpretable problem body |
| AC-025 | US-009 | Unavailable `503` |
| AC-026 | US-009 | Invariant failure `503` |
| AC-027 | US-009 | Safe `503` detail |
| AC-028 | US-010 | RFC 7807 catalog |
| AC-029 | US-011 | New lowercase UUID `id` |
| AC-030 | US-011 | `405` and `404` |
| AC-031 | US-011 | Repeated upload is not stored |
| AC-032 | US-001 | `model.provider` and `model.name` |
| AC-033 | US-005 | Non-file fields ignored |
| AC-034 | US-003 | `WWW-Authenticate: Bearer` |
| AC-035 | US-009 | Port not called on validation failure |
