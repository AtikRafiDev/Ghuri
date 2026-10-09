using System.Net;

namespace Ghuri.Application.Common;

/// <summary>
/// The look every Ghuri email shares: the "Ghuri" name on top, a white card
/// with a heading, the message and an optional big button, and a small
/// footer - in the website's forest green.
/// </summary>
/// <remarks>
/// Email isn't a browser. Gmail and Outlook drop &lt;style&gt; blocks and
/// web fonts and ignore flexbox, so this is built the old way: tables for
/// layout and every style written inline, on the tag itself. The colours
/// are the frontend palette's (index.css): forest-700 for the button,
/// ink-50 for the background, ink-700 for text.
/// Text a person typed must be HtmlEncoded before it goes into the body;
/// <see cref="Button"/> encodes its own text and link.
/// </remarks>
internal static class EmailLayout
{
    private const string Font = "'Segoe UI', Roboto, Helvetica, Arial, sans-serif";

    /// <summary>The whole email around <paramref name="bodyHtml"/>.</summary>
    /// <param name="heading">The big title inside the card - plain text.</param>
    /// <param name="preview">The grey line inboxes show after the subject - plain text, never a link or token.</param>
    /// <param name="bodyHtml">Already-safe HTML, usually built from <see cref="Paragraph"/>, <see cref="Button"/> and <see cref="Note"/>.</param>
    public static string Page(string heading, string preview, string bodyHtml) =>
        $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>{WebUtility.HtmlEncode(heading)}</title>
        </head>
        <body style="margin:0;padding:0;background-color:#f4f6f2;">
          <div style="display:none;max-height:0;overflow:hidden;opacity:0;">{WebUtility.HtmlEncode(preview)}</div>
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" bgcolor="#f4f6f2">
            <tr>
              <td align="center" style="padding:32px 16px;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;">
                  <tr>
                    <td style="padding:0 4px 16px;font-family:{Font};font-size:24px;font-weight:700;color:#17583f;">Ghuri</td>
                  </tr>
                  <tr>
                    <td bgcolor="#ffffff" style="padding:32px;border:1px solid #dfe6e1;border-radius:12px;font-family:{Font};font-size:15px;line-height:24px;color:#33433c;">
                      <h1 style="margin:0 0 16px;font-size:22px;line-height:30px;font-weight:700;color:#0f1f18;">{WebUtility.HtmlEncode(heading)}</h1>
                      {bodyHtml}
                    </td>
                  </tr>
                  <tr>
                    <td align="center" style="padding:20px 4px 0;font-family:{Font};font-size:12px;line-height:18px;color:#8a9a91;">
                      Ghuri - Tours across Bangladesh and beyond
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    /// <summary>A paragraph. Inline margins, because each email app has its own default.</summary>
    public static string Paragraph(string html) =>
        $"""<p style="margin:0 0 16px;">{html}</p>""";

    /// <summary>Small grey text for the less important lines ("didn't ask for this?").</summary>
    public static string Note(string html) =>
        $"""<p style="margin:0 0 12px;font-size:13px;line-height:20px;color:#5f7168;">{html}</p>""";

    /// <summary>
    /// A big green button, followed by the same link written out in full -
    /// for the email apps that block buttons or links.
    /// </summary>
    /// <remarks>
    /// A "bulletproof" button: the colour sits on the table cell (bgcolor),
    /// so even Outlook, which ignores padding on links, still shows a green box.
    /// </remarks>
    public static string Button(string text, string url)
    {
        var href = WebUtility.HtmlEncode(url);
        return $"""
            <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:8px 0 24px;">
              <tr>
                <td bgcolor="#17583f" style="border-radius:8px;">
                  <a href="{href}" target="_blank" style="display:inline-block;padding:12px 28px;font-family:{Font};font-size:15px;font-weight:600;line-height:20px;color:#ffffff;text-decoration:none;border-radius:8px;">{WebUtility.HtmlEncode(text)}</a>
                </td>
              </tr>
            </table>
            <p style="margin:0 0 16px;font-size:13px;line-height:20px;color:#5f7168;">
              Button not working? Copy this link into your browser:<br>
              <a href="{href}" target="_blank" style="color:#1f6f51;word-break:break-all;">{href}</a>
            </p>
            """;
    }
}
