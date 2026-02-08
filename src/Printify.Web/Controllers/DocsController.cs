using Markdig;
using Microsoft.AspNetCore.Mvc;

namespace Printify.Web.Controllers;

[ApiController]
public class DocsController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly MarkdownPipeline _markdownPipeline;

    public DocsController(IWebHostEnvironment environment)
    {
        _environment = environment;
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    [HttpGet("/doc")]
    [HttpGet("/doc/")]
    [HttpGet("/docs")]
    [HttpGet("/docs/")]
    public IActionResult RedirectDocsRoot()
    {
        return Redirect("/docs/about");
    }

    [HttpGet("/docs/{page}")]
    public IActionResult GetDocPage(string page)
    {
        // Sanitize page name (only allow alphanumeric and hyphens)
        if (!IsValidPageName(page))
        {
            return NotFound();
        }

        var htmlRoot = Path.Combine(_environment.ContentRootPath, "html");
        var markdownPath = Path.Combine(htmlRoot, "docs", $"{page}.md");

        if (!System.IO.File.Exists(markdownPath))
        {
            return NotFound();
        }

        try
        {
            var markdownContent = System.IO.File.ReadAllText(markdownPath);
            var htmlContent = Markdown.ToHtml(markdownContent, _markdownPipeline);
            var fullHtml = GenerateDocumentPage(page, htmlContent);

            return Content(fullHtml, "text/html");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error rendering documentation: {ex.Message}");
        }
    }

    private static bool IsValidPageName(string page)
    {
        return !string.IsNullOrWhiteSpace(page) &&
               page.All(c => char.IsLetterOrDigit(c) || c == '-') &&
               page.Length <= 50;
    }

    private string GenerateDocumentPage(string page, string contentHtml)
    {
        var pageTitle = FormatPageTitle(page);

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{pageTitle} - Virtual Printer</title>
    <meta name=""description"" content=""Virtual Printer documentation - {pageTitle}"">
    <link rel=""stylesheet"" href=""/assets/css/style.css"">
    <link rel=""stylesheet"" href=""/assets/css/docs.css"">
</head>
<body>
    <div class=""docs-container"">
        <!-- BEGIN NAVIGATION -->
        <aside class=""docs-sidebar"" id=""docsSidebar"">
            <div class=""docs-sidebar-header"">
                <button class=""icon-btn"" onclick=""toggleDocsSidebar()"" title=""Toggle sidebar"">
                    <img src=""/assets/icons/panel-left.svg"" alt="""" width=""20"" height=""20"">
                </button>
            </div>

            <a href=""/"" class=""docs-back-link"">
                <img src=""/assets/icons/arrow-left.svg"" alt="""" width=""20"" height=""20"">
                <span>Go to Application</span>
            </a>

            <nav class=""docs-nav list"">
                <a href=""/docs/about"" class=""docs-nav-item list-item"">
                    <img class=""docs-nav-icon"" src=""/assets/icons/info.svg"" alt="""" width=""20"" height=""20"">
                    <span>About Virtual Printer</span>
                </a>
                <a href=""/docs/guide"" class=""docs-nav-item list-item"">
                    <img class=""docs-nav-icon"" src=""/assets/icons/book-open.svg"" alt="""" width=""20"" height=""20"">
                    <span>Getting Started</span>
                </a>
                <a href=""/docs/faq"" class=""docs-nav-item list-item"">
                    <img class=""docs-nav-icon"" src=""/assets/icons/help-circle.svg"" alt="""" width=""20"" height=""20"">
                    <span>FAQ & Troubleshooting</span>
                </a>
                <a href=""/docs/security"" class=""docs-nav-item list-item"">
                    <img class=""docs-nav-icon"" src=""/assets/icons/shield.svg"" alt="""" width=""20"" height=""20"">
                    <span>Security Guidelines</span>
                </a>
                <a href=""/docs/terms"" class=""docs-nav-item list-item"">
                    <img class=""docs-nav-icon"" src=""/assets/icons/file-text.svg"" alt="""" width=""20"" height=""20"">
                    <span>Terms of Service</span>
                </a>
                <a href=""/docs/privacy"" class=""docs-nav-item list-item"">
                    <img class=""docs-nav-icon"" src=""/assets/icons/lock.svg"" alt="""" width=""20"" height=""20"">
                    <span>Privacy Policy</span>
                </a>
                <a href=""/docs/licenses"" class=""docs-nav-item list-item"">
                    <img class=""docs-nav-icon"" src=""/assets/icons/file-minus.svg"" alt="""" width=""20"" height=""20"">
                    <span>Third-Party Licenses</span>
                </a>
            </nav>
        </aside>
        <!-- END NAVIGATION -->

        <!-- PAGE CONTENT -->
        <main class=""docs-content"">
            <button class=""icon-btn docs-theme-toggle"" onclick=""toggleTheme()"" title=""Toggle theme"">
                <svg id=""themeIconDark"" width=""18"" height=""18"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" style=""display: none;"">
                    <path d=""M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z""></path>
                </svg>
                <svg id=""themeIconLight"" width=""18"" height=""18"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" style=""display: block;"">
                    <circle cx=""12"" cy=""12"" r=""5""></circle>
                    <line x1=""12"" y1=""1"" x2=""12"" y2=""3""></line>
                    <line x1=""12"" y1=""21"" x2=""12"" y2=""23""></line>
                    <line x1=""4.22"" y1=""4.22"" x2=""5.64"" y2=""5.64""></line>
                    <line x1=""18.36"" y1=""18.36"" x2=""19.78"" y2=""19.78""></line>
                    <line x1=""1"" y1=""12"" x2=""3"" y2=""12""></line>
                    <line x1=""21"" y1=""12"" x2=""23"" y2=""12""></line>
                    <line x1=""4.22"" y1=""19.78"" x2=""5.64"" y2=""18.36""></line>
                    <line x1=""18.36"" y1=""5.64"" x2=""19.78"" y2=""4.22""></line>
                </svg>
            </button>
            <div class=""docs-content-inner"">
                {contentHtml}
            </div>
        </main>
        <!-- END PAGE CONTENT -->
    </div>

    <script src=""/assets/js/docs.js""></script>
</body>
</html>";
    }

    private static string FormatPageTitle(string page)
    {
        // Convert page name to title case
        return char.ToUpper(page[0]) + page.Substring(1).Replace('-', ' ');
    }
}
