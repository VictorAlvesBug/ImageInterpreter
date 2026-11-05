# 0001. Use JWT Bearer authentication

**Date:** 2026-11-05

## Status

Accepted

## Context

`POST /api/v1/interpretations` must reject callers the product does not accept. The product decision in [docs/specs/decisions/2026-11-05-authentication.md](../specs/decisions/2026-11-05-authentication.md) requires a valid token on every interpretation call and places this API in the resource-server role: it does not register users and it does not issue refresh tokens. What the product requires is fixed there. This record decides the mechanism and records its cost.

v1 is stateless and has no database. The API cannot resolve a session row, a user row, or a stored secret on each request. Whatever proof the caller sends has to be checked from the request and from key material that is not a per-user record in this service. No LLM vendor is chosen; this record does not select a vision provider, and the vision adapter is not part of authentication.

Callers are expected to obtain tokens from an issuer this API does not operate, then send them on the interpretation request. The API's job is to accept or reject that proof.

### Rejected alternatives

**Opaque API keys.** A shared random key is easy to send and easy to rotate only when the resource server stores the key (or a hash of it) and compares it on each call. v1 has no database to hold that material. An opaque key also carries no signed issuer, audience, expiry, or scope, so the API cannot enforce those properties without a lookup. That combination does not fit a stateless resource server.

**Session cookies.** A cookie session identifies a browser login by an identifier the server remembers. Remembering it requires a session store, and the cookie model brings browser concerns such as cross-site request forgery. This API does not log people in and does not keep session state. Clients call one protected operation with a bearer credential, which a cookie session does not provide without that store.

## Decision

Use JWT Bearer authentication with asymmetric **RS256** signatures.

The API validates each protected token for:

- issuer
- audience
- signature
- expiry

The API holds only public key material. It does not hold the issuer's private key and it does not mint tokens. Product rules for the required scope and for how failures map to HTTP status codes stay in the product decision and in [docs/specs/003-api-contracts.md](../specs/003-api-contracts.md). They are not restated here.

## Consequences

- A JWKS endpoint or a configured public key is required. The API cannot check an RS256 signature until that material is available, and rotation depends on refreshing it.
- Clock skew matters for expiry. Tokens near `exp` or `nbf` will be rejected or accepted according to how far this host's clock may differ from the issuer's. The allowed skew has to be an explicit operational setting; it is not implied by the algorithm.
- Validation stays local to the request. Authentication does not add a database, a session cache, or a user table.
- The API cannot revoke a single token by deleting a row. Lifetime is bounded by expiry, which is why clock skew is part of operating the decision. Short-lived tokens are the control v1 has.
- The private key remains with the issuer. Compromise of the API process does not by itself yield a key that can sign new tokens.
- Issuers that can only produce symmetric tokens (a shared HMAC secret) are outside this decision. Adopting one would mean placing a signing secret on the resource server, which RS256 is chosen to avoid.
