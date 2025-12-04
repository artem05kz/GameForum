using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace GameForum.Pages
{
    public class SortModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string? Array { get; set; }

        public string OriginalArrayText { get; set; } = string.Empty;
        public string SortedArrayText { get; set; } = string.Empty;
        public bool HasArray => !string.IsNullOrEmpty(Array);

        public void OnGet()
        {
            if (HasArray)
            {
                try
                {
                    // Создаем HttpClient напрямую
                    using var client = new HttpClient();
                    var response = client.GetAsync($"http://localhost:8080/api/sort/{Array}").Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var json = response.Content.ReadAsStringAsync().Result;

                        // Парсим JSON вручную
                        var jsonDocument = JsonDocument.Parse(json);
                        var root = jsonDocument.RootElement;

                        if (root.TryGetProperty("original", out var original) &&
                            root.TryGetProperty("sorted", out var sorted))
                        {
                            OriginalArrayText = string.Join(", ", original.EnumerateArray().Select(x => x.GetInt32()));
                            SortedArrayText = string.Join(", ", sorted.EnumerateArray().Select(x => x.GetInt32()));
                        }
                    }
                }
                catch (Exception ex)
                {
                    OriginalArrayText = $"Ошибка: {ex.Message}";
                }
            }
        }
    }
}
