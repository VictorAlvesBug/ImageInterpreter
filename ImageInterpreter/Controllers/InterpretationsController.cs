using ImageInterpreter.Authentication;
using ImageInterpreter.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageInterpreter.Controllers;

[ApiController]
[Route("api/v1/interpretations")]
public sealed class InterpretationsController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = InterpretationsPolicies.Write)]
    public Task Post()
    {
        return ProblemJson.WriteAsync(
            HttpContext,
            ProblemCatalog.InterpreterUnavailable(HttpContext.Request.Path.Value ?? string.Empty));
    }
}
