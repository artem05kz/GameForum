using Microsoft.AspNetCore.Mvc;

namespace GameForum.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DrawerController : ControllerBase
    {
        [HttpGet("{id:int}")]
        public IActionResult GetSvg(int id)
        {
            try
            {
                // Декодируем параметры с помощью битовых операций
                var shapeType = (id >> 29) & 0x7;    // 3 бита для типа фигуры (0-7)
                var colorCode = (id >> 26) & 0x7;    // 3 бита для цвета (0-7)
                var param1 = (id >> 18) & 0xFF;      // 8 бит для первого параметра
                var param2 = (id >> 10) & 0xFF;      // 8 бит для второго параметра
                var param3 = (id >> 2) & 0xFF;       // 8 бит для третьего параметра

                // Преобразуем параметры в реальные значения
                var size1 = param1 * 5 + 20;
                var size2 = param2 * 5 + 20;
                var size3 = param3 * 5 + 20;

                var svg = shapeType switch
                {
                    0 => GenerateCircle(colorCode, size1),           // Круг: param1 = радиус
                    1 => GenerateRectangle(colorCode, size1, size2), // Прямоугольник: param1 = ширина, param2 = высота
                    2 => GenerateStar(colorCode, size1),             // Звезда: param1 = размер
                    3 => GenerateTriangle(colorCode, size1),         // Треугольник: param1 = размер
                    4 => GenerateEllipse(colorCode, size1, size2),   // Эллипс: param1 = радиус X, param2 = радиус Y
                    _ => GenerateCircle(colorCode, 50)               // По умолчанию круг
                };

                return Content(svg, "image/svg+xml");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка декодирования: {ex.Message}");
            }
        }

        private string GenerateCircle(int colorCode, int radius)
        {
            var color = GetColor(colorCode);
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                <svg width=""300"" height=""300"" xmlns=""http://www.w3.org/2000/svg"">
                    <rect width=""100%"" height=""100%"" fill=""#f8f9fa""/>
                    <circle cx=""150"" cy=""150"" r=""{radius}"" fill=""{color}"" stroke=""#333"" stroke-width=""2""/>
                    <text x=""150"" y=""280"" text-anchor=""middle"" font-family=""Arial"" font-size=""12"" fill=""#333"">
                        Circle: R={radius}, Color={colorCode}
                    </text>
                </svg>";
        }

        private string GenerateRectangle(int colorCode, int width, int height)
        {
            var color = GetColor(colorCode);
            var x = 150 - width / 2;
            var y = 150 - height / 2;

            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                    <svg width=""300"" height=""300"" xmlns=""http://www.w3.org/2000/svg"">
                        <rect width=""100%"" height=""100%"" fill=""#f8f9fa""/>
                        <rect x=""{x}"" y=""{y}"" width=""{width}"" height=""{height}"" fill=""{color}"" stroke=""#333"" stroke-width=""2""/>
                        <text x=""150"" y=""280"" text-anchor=""middle"" font-family=""Arial"" font-size=""12"" fill=""#333"">
                            Rectangle: {width}×{height}, Color={colorCode}
                        </text>
                    </svg>";
        }

        private string GenerateStar(int colorCode, int size)
        {
            var color = GetColor(colorCode);
            var points = CalculateStarPoints(150, 150, size, 5);

            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                    <svg width=""300"" height=""300"" xmlns=""http://www.w3.org/2000/svg"">
                        <rect width=""100%"" height=""100%"" fill=""#f8f9fa""/>
                        <polygon points=""{points}"" fill=""{color}"" stroke=""#333"" stroke-width=""2""/>
                        <text x=""150"" y=""280"" text-anchor=""middle"" font-family=""Arial"" font-size=""12"" fill=""#333"">
                            Star: Size={size}, Color={colorCode}
                        </text>
                    </svg>";
        }

        private string GenerateTriangle(int colorCode, int size)
        {
            var color = GetColor(colorCode);
            var halfSize = size / 2;

            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                <svg width=""300"" height=""300"" xmlns=""http://www.w3.org/2000/svg"">
                    <rect width=""100%"" height=""100%"" fill=""#f8f9fa""/>
                    <polygon points=""150,{150 - halfSize} {150 - halfSize},{150 + halfSize} {150 + halfSize},{150 + halfSize}"" 
                             fill=""{color}"" stroke=""#333"" stroke-width=""2""/>
                    <text x=""150"" y=""280"" text-anchor=""middle"" font-family=""Arial"" font-size=""12"" fill=""#333"">
                        Triangle: Size={size}, Color={colorCode}
                    </text>
                </svg>";
        }

        private string GenerateEllipse(int colorCode, int rx, int ry)
        {
            var color = GetColor(colorCode);

            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                    <svg width=""300"" height=""300"" xmlns=""http://www.w3.org/2000/svg"">
                        <rect width=""100%"" height=""100%"" fill=""#f8f9fa""/>
                        <ellipse cx=""150"" cy=""150"" rx=""{rx}"" ry=""{ry}"" fill=""{color}"" stroke=""#333"" stroke-width=""2""/>
                        <text x=""150"" y=""280"" text-anchor=""middle"" font-family=""Arial"" font-size=""12"" fill=""#333"">
                            Ellipse: RX={rx}, RY={ry}, Color={colorCode}
                        </text>
                    </svg>";
        }

        private string CalculateStarPoints(int cx, int cy, int outerRadius, int points)
        {
            var result = "";
            var innerRadius = outerRadius / 2;

            for (int i = 0; i < points * 2; i++)
            {
                var angle = Math.PI * i / points;
                var radius = i % 2 == 0 ? outerRadius : innerRadius;
                var x = cx + radius * Math.Sin(angle);
                var y = cy - radius * Math.Cos(angle);
                result += $"{x:F1},{y:F1} ";
            }

            return result.Trim();
        }

        private string GetColor(int colorCode)
        {
            return colorCode switch
            {
                0 => "red",
                1 => "green",
                2 => "blue",
                3 => "yellow",
                4 => "purple",
                5 => "orange",
                6 => "gray",
                7 => "black",
                _ => "blue"
            };
        }
    }
}
