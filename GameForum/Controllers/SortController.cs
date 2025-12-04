using Microsoft.AspNetCore.Mvc;

namespace GameForum.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SortController : ControllerBase
    {
        [HttpGet("{array}")]
        public IActionResult SortArray(string array)
        {
            try
            {
                // Разделяем строку по запятым и преобразуем в числа
                var numbers = array.Split(',')
                    .Where(num => !string.IsNullOrWhiteSpace(num))
                    .Select(num => int.Parse(num.Trim()))
                    .ToArray();

                var sorted = QuickSort(numbers);

                return Ok(new
                {
                    original = numbers,
                    sorted = sorted
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex.Message}. Убедитесь, что переданы числа, разделенные запятыми.");
            }
        }

        private int[] QuickSort(int[] array)
        {
            if (array.Length <= 1) return array;

            var pivot = array[array.Length / 2];
            var left = array.Where(x => x < pivot).ToArray();
            var middle = array.Where(x => x == pivot).ToArray();
            var right = array.Where(x => x > pivot).ToArray();

            return QuickSort(left).Concat(middle).Concat(QuickSort(right)).ToArray();
        }
    }
}
