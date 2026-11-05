# Specification 004 — Data model

| | |
| --- | --- |
| Product | ImageInterpreter |
| Status | Active |
| Version | v1 |
| Date | 2026-11-05 |

## What this data model is

The data model is the canonical interpretation document schema. It is the JSON document returned by `POST /api/v1/interpretations`. It is not a set of relational tables.

The document is one `Interpretation` from [001-domain-model.md](001-domain-model.md), serialized with the field names below. Spec 003 carries this document as the `200` body.

## Persistence

There is no persistence in v1. The API does not write the document, the image, the correlation `id`, or any other record to a database, a file, or object storage. The document exists in the response and is then discarded. A client that needs the result keeps the response it already received.

History or storage would be a future ADR. Until that ADR exists, the schema in this file is the whole data model.

## Document fields

Names are identical to the domain fields and to the HTTP contract.

| Field | JSON type | Required | Rule |
| --- | --- | --- | --- |
| `id` | string | yes | Lowercase UUID, `8-4-4-4-12`. |
| `summary` | string | yes | At least one non-whitespace character. |
| `objects` | array | yes | May be empty. Each item is a detected object. |
| `objects[].label` | string | yes | At least one non-whitespace character. |
| `objects[].confidence` | number | yes | From 0 to 1 inclusive. |
| `objects[].description` | string | yes | May be empty. |
| `extractedText` | array | yes | May be empty. Each item is an extracted text span. |
| `extractedText[].text` | string | yes | At least one non-whitespace character. |
| `extractedText[].confidence` | number | yes | From 0 to 1 inclusive. |
| `confidence` | number | yes | Overall confidence, from 0 to 1 inclusive. |
| `model` | object | yes | Model metadata. |
| `model.provider` | string | yes | At least one non-whitespace character. Opaque. |
| `model.name` | string | yes | At least one non-whitespace character. Opaque. |

No property is null. Additional properties are not part of the document. Duplicate labels are allowed. Array order is the interpreter order and is part of the document.

`confidence` on the root is the overall score. `objects[].confidence` and `extractedText[].confidence` are scores for those items. They share the same 0-to-1 scale and are not required to average to the root value.

## Schema

The following JSON Schema (draft 2020-12) is normative for the `200` body.

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "urn:imageinterpreter:schema:interpretation:v1",
  "title": "Interpretation",
  "type": "object",
  "additionalProperties": false,
  "required": ["id", "summary", "objects", "extractedText", "confidence", "model"],
  "properties": {
    "id": {
      "type": "string",
      "pattern": "^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$"
    },
    "summary": {
      "type": "string",
      "pattern": "\\S"
    },
    "objects": {
      "type": "array",
      "items": { "$ref": "#/$defs/detectedObject" }
    },
    "extractedText": {
      "type": "array",
      "items": { "$ref": "#/$defs/extractedTextSpan" }
    },
    "confidence": { "$ref": "#/$defs/confidence" },
    "model": { "$ref": "#/$defs/modelIdentity" }
  },
  "$defs": {
    "confidence": {
      "type": "number",
      "minimum": 0,
      "maximum": 1
    },
    "detectedObject": {
      "type": "object",
      "additionalProperties": false,
      "required": ["label", "confidence", "description"],
      "properties": {
        "label": { "type": "string", "pattern": "\\S" },
        "confidence": { "$ref": "#/$defs/confidence" },
        "description": { "type": "string" }
      }
    },
    "extractedTextSpan": {
      "type": "object",
      "additionalProperties": false,
      "required": ["text", "confidence"],
      "properties": {
        "text": { "type": "string", "pattern": "\\S" },
        "confidence": { "$ref": "#/$defs/confidence" }
      }
    },
    "modelIdentity": {
      "type": "object",
      "additionalProperties": false,
      "required": ["provider", "name"],
      "properties": {
        "provider": { "type": "string", "pattern": "\\S" },
        "name": { "type": "string", "pattern": "\\S" }
      }
    }
  }
}
```

## Sample document

This sample is a valid document. The strings are an example, not a required reading of any particular image. `model.provider` and `model.name` are placeholders because v1 has not chosen a vendor.

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

A second valid document shows an empty `description`, an empty `extractedText` array, and the endpoint confidence values `0` and `1`:

```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "summary": "A plain gray square.",
  "objects": [
    {
      "label": "square",
      "confidence": 1,
      "description": ""
    }
  ],
  "extractedText": [],
  "confidence": 0,
  "model": {
    "provider": "provider",
    "name": "model"
  }
}
```

Both `objects` and `extractedText` may be empty in the same document, as long as `summary`, `confidence`, and `model` still satisfy the schema.

## What the document does not contain

The schema has no image bytes, no file name, no content type, no bounding box, no language, and no storage key. Those values are not added beside the fields above.

A vision result that cannot be built into this schema does not become a `200` body. Spec 003 maps that failure to `503`.

## Related documents

- [001-domain-model.md](001-domain-model.md) — domain types and invariants for this schema.
- [003-api-contracts.md](003-api-contracts.md) — the operation that returns the document.
- [000-overview.md](000-overview.md) — why v1 has no database and no chosen vendor.
