using AngleSharp;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Mvc;

namespace WebExtract.Controllers;

[ApiController]
[Route("[controller]")]
public class HtmlController : Controller
{
    private readonly ILogger<HtmlController> logger;

    public HtmlController(ILogger<HtmlController> logger)
    {
        this.logger = logger;
    }

    [HttpGet(Name = "ExtractHtml")]
    public async Task<ActionResult> ExtractHtml([FromQuery] Uri url, [FromQuery] string path)
    {
        HttpClient client = new HttpClient();

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");


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
                !contentTypes.Any(type => type.Contains("text/html")))
            {
                return new BadRequestObjectResult(new ProblemDetails
                {
                    Title = "Unexpected response",
                    Detail = "The URL did not return an HTML response."
                });
            }

            string html = await responseMessage.Content.ReadAsStringAsync();

            IBrowsingContext context = BrowsingContext.New(Configuration.Default);
            IDocument document = await context.OpenAsync(request => request.Content(html));

            var result = document.QuerySelector(path);

            return new OkObjectResult(result?.InnerHtml);
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
    }
}