using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebExtract.Controllers;

[ApiController]
[Route("[controller]")]
public class JsonController : ControllerBase
{
    private readonly ILogger<JsonController> logger;

    public JsonController(ILogger<JsonController> logger)
    {
        this.logger = logger;
    }

    [HttpGet(Name = "ExtractJson")]
    public async Task<ActionResult> ExtractJson([FromQuery] Uri url, [FromQuery] string path)
    {
        HttpClient client = new HttpClient();

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        try
        {
            HttpResponseMessage responseMessage = await client.GetAsync(url);

            if (!responseMessage.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(new ProblemDetails
                {
                    Title = "Unsuccessful response",
                    Detail = "The URL did not return a success response."
                });
            }

            if (!responseMessage.Content.Headers.TryGetValues("Content-Type", out IEnumerable<string>? contentTypes) ||
                !contentTypes.Contains("application/json"))
            {
                return new BadRequestObjectResult(new ProblemDetails
                {
                    Title = "Unexpected response",
                    Detail = "The URL did not return a JSON response."
                });
            }

            string json = await responseMessage.Content.ReadAsStringAsync();

            JToken obj = JToken.Parse(json);
            JToken? result = obj.SelectToken(path);

            return new OkObjectResult(result?.ToString());
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Request could not be made");

            return new BadRequestObjectResult(new ProblemDetails
            {
                Title = "Request could not be made",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid url");

            return new BadRequestObjectResult(new ProblemDetails
            {
                Title = "Invalid URL",
                Detail = ex.Message
            });
        }
        catch (JsonReaderException ex)
        {
            logger.LogWarning(ex, "Invalid json");

            return new BadRequestObjectResult(new ProblemDetails
            {
                Title = "Invalid JSON returned from URL",
                Detail = ex.Message
            });
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid path");

            return new BadRequestObjectResult(new ProblemDetails
            {
                Title = "Unable to process path",
                Detail = ex.Message
            });
        }
    }
}