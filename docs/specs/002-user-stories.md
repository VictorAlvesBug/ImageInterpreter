# Specification 002 — User stories

| | |
| --- | --- |
| Product | ImageInterpreter |
| Status | Active |
| Version | v1 |
| Date | 2026-11-05 |

These stories are for an API client that uploads one image and receives the generic interpretation JSON. The HTTP rules are in [003-api-contracts.md](003-api-contracts.md). The document shape is in [004-data-model.md](004-data-model.md). Each story lists the acceptance criteria in [005-acceptance-criteria.md](005-acceptance-criteria.md) that trace to it.

The caller is an API client holding a bearer token. v1 does not define an end-user account in this service. What the token must satisfy is the [authentication decision](decisions/2026-11-05-authentication.md); how the token is checked is [ADR 0001](../adr/0001-use-jwt-authentication.md).

## Index

| Id | Story |
| --- | --- |
| [US-001](#us-001) | Upload one image and receive an interpretation document. |
| [US-002](#us-002) | Call the operation with scope `interpretations:write`. |
| [US-003](#us-003) | See a clear failure when the token is missing or rejected. |
| [US-004](#us-004) | See a clear failure when the scope is absent. |
| [US-005](#us-005) | Send one multipart file field named `image`. |
| [US-006](#us-006) | Stay within the 10 MB image limit. |
| [US-007](#us-007) | Send JPEG, PNG, or WebP. |
| [US-008](#us-008) | Hear when an accepted image cannot be interpreted. |
| [US-009](#us-009) | Hear when the interpreter is unavailable. |
| [US-010](#us-010) | Read every error as an RFC 7807 problem. |
| [US-011](#us-011) | Correlate a result the API does not store. |
| [US-012](#us-012) | Read every confidence on a 0 to 1 scale. |

## Stories

### <a id="us-001"></a>US-001 — Upload one image and receive an interpretation document

**As an** API client, **I want** to upload one supported image, **so that** I receive a generic JSON document with `id`, `summary`, `objects`, `extractedText`, `confidence`, and `model`.

The document uses the same field names for every provider behind `IVisionInterpreter`. `objects[]` carries `label`, `confidence`, and `description`. `extractedText[]` carries `text` and `confidence`. `model` carries `provider` and `name`.

**Criteria:** [AC-001](005-acceptance-criteria.md#ac-001), [AC-002](005-acceptance-criteria.md#ac-002), [AC-003](005-acceptance-criteria.md#ac-003), [AC-004](005-acceptance-criteria.md#ac-004), [AC-005](005-acceptance-criteria.md#ac-005), [AC-032](005-acceptance-criteria.md#ac-032).

### <a id="us-002"></a>US-002 — Call the operation with scope `interpretations:write`

**As an** API client, **I want** to send `Authorization: Bearer` with a token whose `scope` includes `interpretations:write`, **so that** the API accepts the call and interprets my image.

Other scopes may appear beside `interpretations:write`. The wire format of `scope` is specified in spec 003. The product requirement is the authentication decision.

**Criteria:** [AC-007](005-acceptance-criteria.md#ac-007), [AC-008](005-acceptance-criteria.md#ac-008).

### <a id="us-003"></a>US-003 — See a clear failure when the token is missing or rejected

**As an** API client, **I want** a missing or rejected bearer token to fail with `401`, **so that** I can tell that the caller was not authenticated.

A token the API does not accept is `401` even when that token also lacks `interpretations:write`. Which tokens count as accepted is defined by the authentication decision and ADR 0001.

**Criteria:** [AC-009](005-acceptance-criteria.md#ac-009), [AC-010](005-acceptance-criteria.md#ac-010), [AC-011](005-acceptance-criteria.md#ac-011), [AC-034](005-acceptance-criteria.md#ac-034).

### <a id="us-004"></a>US-004 — See a clear failure when the scope is absent

**As an** API client, **I want** an authenticated token that does not include `interpretations:write` to fail with `403`, **so that** I can tell authorization apart from authentication.

**Criteria:** [AC-012](005-acceptance-criteria.md#ac-012).

### <a id="us-005"></a>US-005 — Send one multipart file field named `image`

**As an** API client, **I want** to post `multipart/form-data` with exactly one file field named `image`, **so that** the request shape is fixed.

A request that is not multipart, that omits `image`, that sends `image` as something other than a file, that includes any extra file part, or that sends an empty file, fails with `400`. Extra non-file form fields do not change the outcome.

**Criteria:** [AC-013](005-acceptance-criteria.md#ac-013), [AC-014](005-acceptance-criteria.md#ac-014), [AC-015](005-acceptance-criteria.md#ac-015), [AC-016](005-acceptance-criteria.md#ac-016), [AC-033](005-acceptance-criteria.md#ac-033).

### <a id="us-006"></a>US-006 — Stay within the 10 MB image limit

**As an** API client, **I want** an image of up to 10 MB to be accepted and a larger image to fail with `413`, **so that** I know the size limit before I upload.

In this product 10 MB means 10,485,760 bytes. That count is the file content of `image`, not the multipart framing around it.

**Criteria:** [AC-017](005-acceptance-criteria.md#ac-017), [AC-018](005-acceptance-criteria.md#ac-018).

### <a id="us-007"></a>US-007 — Send JPEG, PNG, or WebP

**As an** API client, **I want** JPEG, PNG, and WebP images to be accepted, **so that** those formats return an interpretation, and any other format fails with `415`.

The file extension is not the rule. Spec 003 defines the `Content-Type` and signature rules.

**Criteria:** [AC-019](005-acceptance-criteria.md#ac-019), [AC-020](005-acceptance-criteria.md#ac-020), [AC-021](005-acceptance-criteria.md#ac-021), [AC-022](005-acceptance-criteria.md#ac-022).

### <a id="us-008"></a>US-008 — Hear when an accepted image cannot be interpreted

**As an** API client, **I want** a file that passed validation and still cannot be interpreted to fail with `422`, **so that** I know the bytes arrived as JPEG, PNG, or WebP and the interpreter declined the content.

The response is a problem document. It is not a partial interpretation.

**Criteria:** [AC-023](005-acceptance-criteria.md#ac-023), [AC-024](005-acceptance-criteria.md#ac-024).

### <a id="us-009"></a>US-009 — Hear when the interpreter is unavailable

**As an** API client, **I want** an outage, a timeout, or a result that breaks the interpretation invariants to fail with `503`, **so that** I can retry later without parsing a vendor payload.

Validation and authentication failures do not call `IVisionInterpreter`.

**Criteria:** [AC-025](005-acceptance-criteria.md#ac-025), [AC-026](005-acceptance-criteria.md#ac-026), [AC-027](005-acceptance-criteria.md#ac-027), [AC-035](005-acceptance-criteria.md#ac-035).

### <a id="us-010"></a>US-010 — Read every error as an RFC 7807 problem

**As an** API client, **I want** every error body to be an RFC 7807 problem document, **so that** I can handle `400`, `401`, `403`, `413`, `415`, `422`, and `503` the same way.

**Criteria:** [AC-028](005-acceptance-criteria.md#ac-028).

### <a id="us-011"></a>US-011 — Correlate a result the API does not store

**As an** API client, **I want** each success to include a new `id`, **so that** I can correlate my logs even though v1 stores nothing and offers no way to fetch that id later.

Repeating the same upload yields a different `id`. `POST /api/v1/interpretations` is the only product operation.

**Criteria:** [AC-029](005-acceptance-criteria.md#ac-029), [AC-030](005-acceptance-criteria.md#ac-030), [AC-031](005-acceptance-criteria.md#ac-031).

### <a id="us-012"></a>US-012 — Read every confidence on a 0 to 1 scale

**As an** API client, **I want** every `confidence` value — the interpretation, each object, and each text span — to be a number from 0 to 1 inclusive, **so that** I can treat them as the same scale.

**Criteria:** [AC-006](005-acceptance-criteria.md#ac-006).

## Traceability

| Story | Criteria |
| --- | --- |
| US-001 | AC-001, AC-002, AC-003, AC-004, AC-005, AC-032 |
| US-002 | AC-007, AC-008 |
| US-003 | AC-009, AC-010, AC-011, AC-034 |
| US-004 | AC-012 |
| US-005 | AC-013, AC-014, AC-015, AC-016, AC-033 |
| US-006 | AC-017, AC-018 |
| US-007 | AC-019, AC-020, AC-021, AC-022 |
| US-008 | AC-023, AC-024 |
| US-009 | AC-025, AC-026, AC-027, AC-035 |
| US-010 | AC-028 |
| US-011 | AC-029, AC-030, AC-031 |
| US-012 | AC-006 |
