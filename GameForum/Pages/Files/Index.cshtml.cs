using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace GameForum.Pages.Files;

public class IndexModel : PageModel
{
    public record FileItem(string Name, string Uploader, long Length, DateTime LastModified);

    public List<FileItem> Files { get; private set; } = new();

    private string EnsureStorage()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs");
        if (!Directory.Exists(root)) Directory.CreateDirectory(root);
        return root;
    }

    public void OnGet()
    {
        var root = EnsureStorage();
        Files = Directory.EnumerateFiles(root, "*.pdf")
            .Select(p => new FileInfo(p))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Select(f =>
            {
                var metaPath = Path.Combine(root, Path.GetFileNameWithoutExtension(f.Name) + ".json");
                var uploader = "Unknown";
                if (System.IO.File.Exists(metaPath))
                {
                    try
                    {
                        var json = System.IO.File.ReadAllText(metaPath);
                        var meta = JsonSerializer.Deserialize<UploadMeta>(json);
                        if (!string.IsNullOrWhiteSpace(meta?.Uploader)) uploader = meta.Uploader!;
                    }
                    catch { }
                }
                return new FileItem(f.Name, uploader, f.Length, f.LastWriteTimeUtc);
            })
            .ToList();
    }

    public IActionResult OnGetDownload(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest();
        var root = EnsureStorage();
        var safeName = Path.GetFileName(name);
        var path = Path.Combine(root, safeName);
        if (!System.IO.File.Exists(path)) return NotFound();
        var stream = System.IO.File.OpenRead(path);
        return File(stream, "application/pdf", safeName);
    }

    public IActionResult OnGetDelete(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest();
        var root = EnsureStorage();
        var safeName = Path.GetFileName(name);
        var path = Path.Combine(root, safeName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        var metaPath = Path.Combine(root, Path.GetFileNameWithoutExtension(safeName) + ".json");
        if (System.IO.File.Exists(metaPath)) System.IO.File.Delete(metaPath);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var file = Request.Form.Files["file"];
        if (file == null || file.Length == 0) { ModelState.AddModelError(string.Empty, "Файл не выбран"); OnGet(); return Page(); }
        if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Поддерживаются только PDF");
            OnGet();
            return Page();
        }
        var root = EnsureStorage();
        var safeName = Path.GetFileName(file.FileName);
        var outPath = Path.Combine(root, safeName);
        using (var fs = System.IO.File.Create(outPath))
        {
            await file.CopyToAsync(fs);
        }
        // write sidecar metadata with uploader display name
        var uploader = User?.FindFirst("displayName")?.Value ?? User?.Identity?.Name ?? "Anonymous";
        var meta = new UploadMeta { Uploader = uploader, UploadedAtUtc = DateTime.UtcNow };
        var metaPath = Path.Combine(root, Path.GetFileNameWithoutExtension(safeName) + ".json");
        await System.IO.File.WriteAllTextAsync(metaPath, JsonSerializer.Serialize(meta));
        return RedirectToPage();
    }

    private class UploadMeta
    {
        public string? Uploader { get; set; }
        public DateTime UploadedAtUtc { get; set; }
    }
}

