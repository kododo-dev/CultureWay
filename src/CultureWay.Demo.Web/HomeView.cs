using System.Globalization;
using System.Net;
using Microsoft.Extensions.Localization;

namespace Kododo.CultureWay.Demo.Web;

static class HomeView
{
    public static string Render(
        string basePath,
        IStringLocalizer localizer,
        string currentCulture,
        IReadOnlyList<string> supportedCultures)
    {
        string L(string key) => WebUtility.HtmlEncode(localizer[key].Value);

        var cultureOptions = string.Concat(supportedCultures.Select(code =>
        {
            var selected = string.Equals(code, currentCulture, StringComparison.OrdinalIgnoreCase) ? " selected" : "";
            return $"<option value='{WebUtility.HtmlEncode(code)}'{selected}>{WebUtility.HtmlEncode(NativeLabel(code))}</option>";
        }));

        var cultureSwitcher = $"""
            <select class="culture-select" onchange="location.href='?culture='+this.value">
              {cultureOptions}
            </select>
            """;

        static string Feature(string title, string desc, string icon) => $"""
            <div class="card">
              <div class="icon">{icon}</div>
              <h3>{title}</h3>
              <p>{desc}</p>
            </div>
            """;

        return $$"""
            <!doctype html>
            <html lang="{{currentCulture}}">
            <head>
              <meta charset="UTF-8"/>
              <meta name="viewport" content="width=device-width,initial-scale=1"/>
              <title>{{L("App.Title")}}</title>
              <style>
                *{box-sizing:border-box;margin:0;padding:0}
                body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;background:#f9fafb;color:#111827;min-height:100vh}
                .topbar{background:#fff;border-bottom:1px solid #e5e7eb;padding:10px 24px;display:flex;align-items:center;justify-content:space-between;gap:12px}
                .topbar-brand{font-weight:700;font-size:.95rem;color:#111827;text-decoration:none}
                .topbar-nav{display:flex;align-items:center;gap:8px;flex-wrap:wrap}
                .topbar-link{color:#6b7280;font-size:.875rem;text-decoration:none;padding:4px 2px}
                .topbar-link:hover{color:#111827}
                .culture-bar{background:#f3f4f6;border-bottom:1px solid #e5e7eb;padding:8px 24px;display:flex;align-items:center;gap:8px;flex-wrap:wrap}
                .culture-label{font-size:.8rem;color:#6b7280;margin-right:4px}
                .culture-select{padding:6px 12px;border-radius:6px;border:1px solid #d1d5db;background:#fff;color:#374151;font-size:.85rem;font-weight:500;cursor:pointer}
                .culture-select:focus{outline:2px solid #2563eb;outline-offset:1px}
                .hero{padding:72px 24px 64px;text-align:center;background:#fff;border-bottom:1px solid #e5e7eb}
                .hero h1{font-size:2.25rem;font-weight:800;letter-spacing:-.02em;margin-bottom:16px;line-height:1.2}
                .hero p{color:#6b7280;font-size:1.1rem;max-width:580px;margin:0 auto 32px;line-height:1.6}
                .btn{display:inline-flex;align-items:center;gap:8px;background:#2563eb;color:#fff;font-weight:600;font-size:1rem;padding:14px 30px;border-radius:8px;text-decoration:none;transition:background .15s}
                .btn:hover{background:#1d4ed8}
                .tagline{margin-top:16px;font-size:.875rem;color:#9ca3af}
                .features{display:grid;grid-template-columns:repeat(auto-fill,minmax(280px,1fr));gap:20px;padding:40px 24px;max-width:1000px;margin:0 auto}
                .card{background:#fff;border:1px solid #e5e7eb;border-radius:10px;padding:24px}
                .card .icon{font-size:1.75rem;margin-bottom:12px}
                .card h3{font-size:1rem;font-weight:700;margin-bottom:8px}
                .card p{font-size:.9rem;color:#6b7280;line-height:1.5}
                .footer{text-align:center;padding:24px;color:#9ca3af;font-size:.82rem;border-top:1px solid #e5e7eb}
              </style>
            </head>
            <body>
              <nav class="topbar">
                <a class="topbar-brand" href="?culture={{currentCulture}}">{{L("App.Title")}}</a>
                <div class="topbar-nav">
                  <a class="topbar-link" href="?culture={{currentCulture}}">{{L("Nav.Home")}}</a>
                  <a class="topbar-link" href="{{basePath}}/translations">{{L("Nav.Editor")}}</a>
                </div>
              </nav>

              <div class="culture-bar">
                <span class="culture-label">Culture:</span>
                {{cultureSwitcher}}
              </div>

              <div class="hero">
                <h1>{{L("Hero.Heading")}}</h1>
                <p>{{L("Hero.Subheading")}}</p>
                <a class="btn" href="{{basePath}}/translations">
                  <svg width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" d="m3 5h11M9 3v2m1.048 9.5A18.022 18.022 0 0 1 6.412 9m6.088 9h7M11 21l5-10 5 10M12.751 5C11.783 10.77 8.07 15.61 3 18.129"/>
                  </svg>
                  {{L("Hero.Button")}}
                </a>
                <p class="tagline">{{L("App.Tagline")}}</p>
              </div>

              <div class="features">
                {{Feature(L("Feature.1.Title"), L("Feature.1.Desc"), "⚡")}}
                {{Feature(L("Feature.2.Title"), L("Feature.2.Desc"), "🌍")}}
                {{Feature(L("Feature.3.Title"), L("Feature.3.Desc"), "🐘")}}
              </div>

              <div class="footer">{{L("Footer.Text")}}</div>
            </body>
            </html>
            """;
    }

    private static string NativeLabel(string code)
    {
        try
        {
            return CultureInfo.GetCultureInfo(code).NativeName;
        }
        catch (CultureNotFoundException)
        {
            return code;
        }
    }
}
