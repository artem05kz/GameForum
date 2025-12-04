using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameForum.Data;
using GameForum.Model;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace GameForum.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    private readonly AppDbContext _context;

    public TestController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetTestData()
    {
        // 1. Р’С‹Р±РѕСЂРєР° РїРѕР»СЊР·РѕРІР°С‚РµР»РµР№ (AuthUsers) РёР· Р±Р°Р·С‹ РґР°РЅРЅС‹С…
        var users = await _context.AuthUsers
            .Select(u => new { u.Id, u.Username, u.DisplayName, u.IsAdmin })
            .ToListAsync();

        // 2. РРЅС„РѕСЂРјР°С†РёСЏ Рѕ РІРµСЂСЃРёРё .NET Рё СЃРёСЃС‚РµРјРµ
        var runtimeInfo = new
        {
            RuntimeVersion = RuntimeInformation.FrameworkDescription,
            OSDescription = RuntimeInformation.OSDescription,
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            
        };

        // 3. Р¤РѕСЂРјРёСЂСѓРµРј РѕС‚РІРµС‚
        var response = new
        {
            Users = users,
            RuntimeInfo = runtimeInfo
        };

        return Ok(response);
    }
}
