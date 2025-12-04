using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GameForum.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServerInfoController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetServerInfo()
        {
            try
            {
                var info = new
                {
                    // Основная информация
                    machineName = Environment.MachineName,
                    osVersion = Environment.OSVersion.ToString(),
                    runtimeVersion = RuntimeInformation.FrameworkDescription,
                    processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    osArchitecture = RuntimeInformation.OSArchitecture.ToString(),

                    // Информация о процессе
                    processId = Environment.ProcessId,
                    currentDirectory = Environment.CurrentDirectory,
                    userDomainName = Environment.UserDomainName,
                    userName = Environment.UserName,

                    // Системная информация
                    processorCount = Environment.ProcessorCount,
                    systemPageSize = Environment.SystemPageSize,
                    tickCount = Environment.TickCount64,
                    workingSet = Environment.WorkingSet,
                    is64BitProcess = Environment.Is64BitProcess,
                    is64BitOs = Environment.Is64BitOperatingSystem,

                    // Информация о времени
                    systemStartup = DateTime.Now - TimeSpan.FromMilliseconds(Environment.TickCount64),
                    currentTime = DateTime.Now,

                    // Дополнительная информация
                    runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory(),
                    clrVersion = Environment.Version.ToString(),
                    environmentVariables = Environment.GetEnvironmentVariables(),

                    // Дисковая информация
                    logicalDrives = Environment.GetLogicalDrives(),
                    currentDriveInfo = GetDriveInfo(Environment.CurrentDirectory),

                    // Процессы (первые 10)
                    processes = Process.GetProcesses()
                        .Take(10)
                        .Select(p => new
                        {
                            id = p.Id,
                            name = p.ProcessName,
                            startTime = p.StartTime,
                            memory = p.WorkingSet64
                        })
                };

                return Ok(info);
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex.Message}");
            }
        }

        private object GetDriveInfo(string path)
        {
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(path)!);
                return new
                {
                    name = drive.Name,
                    driveType = drive.DriveType.ToString(),
                    totalSize = drive.TotalSize,
                    availableSpace = drive.AvailableFreeSpace,
                    totalFreeSpace = drive.TotalFreeSpace
                };
            }
            catch
            {
                return "Недоступно";
            }
        }
    }
}
