# Authentication

**Date:** 2026-11-05

**Status:** Accepted

## Requirement

Every interpretation call requires a valid JSON Web Token. The protected operation is `POST /api/v1/interpretations`. A request without a valid token is not interpreted.

v1 is stateless and has no database. The API does not look up a user, a session, or a stored credential in order to accept a call. The token itself is what the caller presents.

How that token is signed and checked is recorded in [docs/adr/0001-use-jwt-authentication.md](../../adr/0001-use-jwt-authentication.md). This document states what the product requires. It does not choose a token issuer product or a vision-model vendor. No LLM vendor is selected for v1.

## Resource server

In v1 the API is a resource server.

- It validates access tokens presented with the interpretation request.
- It does not register users.
- It does not issue refresh tokens.

Account creation, sign-in, and token issuance belong to an external issuer. This service does not add a user directory or a token store to support them.

## Scope

The `scope` claim must include `interpretations:write`. A token that the API accepts as authentic still does not authorize interpretation unless that scope is present.

The wire format of the claim, and the rest of the HTTP contract, are defined in [docs/specs/003-api-contracts.md](../003-api-contracts.md).

## Status codes

Authentication and authorization failures are different outcomes.

| Situation | Status |
| --- | --- |
| The token is missing, invalid, or expired. | `401 Unauthorized` |
| The caller is authenticated, but the token does not include `interpretations:write`. | `403 Forbidden` |

`401` means the API could not establish the caller. `403` means the caller is authenticated and is not allowed to perform this operation. The response body for both codes follows the API contract.

## Related documents

- [docs/adr/0001-use-jwt-authentication.md](../../adr/0001-use-jwt-authentication.md) — why JWT Bearer is the mechanism, and what that choice costs.
- [docs/specs/003-api-contracts.md](../003-api-contracts.md) — the HTTP contract for `POST /api/v1/interpretations`, including `scope` and error bodies.
