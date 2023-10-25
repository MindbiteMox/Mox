using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.UI
{
    public static class InlineStaticFiles
    {
        private readonly static ConcurrentDictionary<string, Configuration.StaticIncludes.StaticFile> _scriptCache = new();
        private readonly static ConcurrentDictionary<string, Configuration.StaticIncludes.StaticFile> _styleCache = new();

        public static void IncludeScript(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper html, string path)
        {
            var script = _scriptCache.GetOrAdd(path, _ => Configuration.StaticIncludes.StaticFile.Script(path));

            if (!html.ViewContext.HttpContext.Items.TryGetValue("MoxInlineStaticFiles", out var _inlineScripts) || _inlineScripts is not List<Configuration.StaticIncludes.StaticFile> inlineScripts)
            {
                inlineScripts = new List<Configuration.StaticIncludes.StaticFile>();
                html.ViewContext.HttpContext.Items["MoxInlineStaticFiles"] = inlineScripts;
            }

            if (!inlineScripts.Contains(script))
            {
                inlineScripts.Add(script);
            }
        }

        public static void IncludeStyle(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper html, string path)
        {
            var style = _styleCache.GetOrAdd(path, _ => Configuration.StaticIncludes.StaticFile.Style(path));

            if (!html.ViewContext.HttpContext.Items.TryGetValue("MoxInlineStaticFiles", out var _inlineStyles) || _inlineStyles is not List<Configuration.StaticIncludes.StaticFile> inlineStyles)
            {
                inlineStyles = new List<Configuration.StaticIncludes.StaticFile>();
                html.ViewContext.HttpContext.Items["MoxInlineStaticFiles"] = inlineStyles;
            }

            if (!inlineStyles.Contains(style))
            {
                inlineStyles.Add(style);
            }
        }

        public static async Task<Microsoft.AspNetCore.Html.IHtmlContent> InlineStaticFilesAsync(this MoxHtmlExtensionCollection mox, bool head)
        {
            if (mox.HtmlHelper.ViewContext.HttpContext.Items.TryGetValue("MoxInlineStaticFiles", out var _inlineStyles) && _inlineStyles is List<Configuration.StaticIncludes.StaticFile> inlineStyles)
            {
                var config = mox.HtmlHelper.ViewContext.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<Configuration.StaticIncludes.IncludeConfig>>();
                var staticFileProviderOptions = mox.HtmlHelper.ViewContext.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<Configuration.StaticIncludes.StaticFileProviderOptions>>();

                var builder = new Microsoft.AspNetCore.Html.HtmlContentBuilder();
                foreach(var style in inlineStyles.Where(x => x.RenderInHead == head))
                {
                    builder.AppendHtml(style.Render(config.Value.StaticRoot, staticFileProviderOptions.Value.FileProviders));
                }
                return builder;
            }

            return new Microsoft.AspNetCore.Html.HtmlString("");
        }
    }
}
