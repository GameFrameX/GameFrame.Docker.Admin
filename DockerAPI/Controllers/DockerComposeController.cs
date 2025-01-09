using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DockerAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DockerComposeController : ControllerBase
    {
        private readonly DockerComposeService _dockerComposeService;

        public DockerComposeController(DockerComposeService dockerComposeService)
        {
            _dockerComposeService = dockerComposeService;
        }

        /// <summary>
        /// 上传并执行 Docker Compose 文件
        /// </summary>
        /// <param name="file">Docker Compose 文件</param>
        /// <returns>执行结果</returns>
        [HttpPost]
        public async Task<IActionResult> ExecuteDockerCompose(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            var filePath = Path.GetTempFileName();
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            await _dockerComposeService.ExecuteDockerComposeAsync(filePath);
            System.IO.File.Delete(filePath);

            return Ok();
        }
    }
}