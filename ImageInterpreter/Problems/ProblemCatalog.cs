using Microsoft.AspNetCore.Mvc;

namespace ImageInterpreter.Problems;

public static class ProblemCatalog
{
    public static ProblemDetails ContentTypeMustBeMultipart(string instance) =>
        Create(
            "urn:imageinterpreter:problem:invalid-request",
            "Bad Request",
            400,
            "Content-Type must be multipart/form-data.",
            instance);

    public static ProblemDetails ImageFileMissing(string instance) =>
        Create(
            "urn:imageinterpreter:problem:invalid-request",
            "Bad Request",
            400,
            "The request must include one file field named image.",
            instance);

    public static ProblemDetails ImageFileNotExactlyOne(string instance) =>
        Create(
            "urn:imageinterpreter:problem:invalid-request",
            "Bad Request",
            400,
            "The request must include exactly one file field named image.",
            instance);

    public static ProblemDetails ImageFileEmpty(string instance) =>
        Create(
            "urn:imageinterpreter:problem:invalid-request",
            "Bad Request",
            400,
            "The image file is empty.",
            instance);

    public static ProblemDetails BearerTokenRequired(string instance) =>
        Create(
            "urn:imageinterpreter:problem:unauthorized",
            "Unauthorized",
            401,
            "A bearer token is required.",
            instance);

    public static ProblemDetails BearerTokenNotAccepted(string instance) =>
        Create(
            "urn:imageinterpreter:problem:unauthorized",
            "Unauthorized",
            401,
            "The bearer token was not accepted.",
            instance);

    public static ProblemDetails ScopeRequired(string instance) =>
        Create(
            "urn:imageinterpreter:problem:forbidden",
            "Forbidden",
            403,
            "The interpretations:write scope is required.",
            instance);

    public static ProblemDetails PayloadTooLarge(string instance) =>
        Create(
            "urn:imageinterpreter:problem:payload-too-large",
            "Payload Too Large",
            413,
            "The image is larger than 10 MB.",
            instance);

    public static ProblemDetails UnsupportedMediaType(string instance) =>
        Create(
            "urn:imageinterpreter:problem:unsupported-media-type",
            "Unsupported Media Type",
            415,
            "The image must be JPEG, PNG, or WebP.",
            instance);

    public static ProblemDetails UninterpretableImage(string instance) =>
        Create(
            "urn:imageinterpreter:problem:uninterpretable-image",
            "Unprocessable Content",
            422,
            "The image could not be interpreted.",
            instance);

    public static ProblemDetails InterpreterUnavailable(string instance) =>
        Create(
            "urn:imageinterpreter:problem:interpreter-unavailable",
            "Service Unavailable",
            503,
            "The image interpreter is unavailable.",
            instance);

    public static ProblemDetails MethodNotAllowed(string instance) =>
        Create(
            "urn:imageinterpreter:problem:method-not-allowed",
            "Method Not Allowed",
            405,
            "POST is the only method on this resource.",
            instance);

    public static ProblemDetails NotFound(string instance) =>
        Create(
            "urn:imageinterpreter:problem:not-found",
            "Not Found",
            404,
            "No interpretation resource is stored.",
            instance);

    private static ProblemDetails Create(string type, string title, int status, string detail, string instance) =>
        new()
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail,
            Instance = instance,
        };
}
