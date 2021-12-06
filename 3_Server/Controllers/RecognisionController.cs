using Lab.Contract;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    internal class RecognisionController : ControllerBase
    {
        private IRecognisionStorage db;
        private IRecogniser recogniser;

        public RecognisionController(IRecognisionStorage storage)
        {
            db = storage;
        }

        [HttpPut("{image}")]
        public ActionResult<RecognisionResult> PutImage(SingleImage image)
        {
            byte[] pixels = Convert.FromBase64String(image.ImageBase64);
            if (pixels.Length != image.Width * image.Height * 4) // TODO exclude magic constant
            {
                return StatusCode(412, $"Pixel count {pixels.Length} not equals to width * height * 4 = {image.Width * image.Height * 4}");
            }

            RecognisionResult recognised = recogniser.RecogniseAsync(image.Name, pixels, image.Width, image.Height).Result;
            if (recognised != null)
                return recognised;
            else
                return StatusCode(412, "Broken image \"" + image.Name + "\"");
        }

        [HttpGet("{id}")]
        public ActionResult<RecognisionData> GetData(int id)
        {
            RecognisionData? b = db.Load(id);
            if (b != null)
                return b;
            else
                return StatusCode(404, "RecognisionData with given id is not found");
        }

        [HttpGet("/all")]
        public ActionResult<int[]> GetIds()
        {
            return db.LoadIds();
        }

        [HttpDelete("/clear")]
        public ActionResult DeleteClear()
        {
            db.Clear();
            return StatusCode(200);
        }
    }
}
