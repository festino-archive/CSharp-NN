using Lab.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Lab.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    internal class RecognisionController : ControllerBase
    {
        private IRecognisionStorage db;
        private IRecogniser recogniser;

        public RecognisionController(IRecognisionStorage storage, IRecogniser recogniser)
        {
            db = storage;
            this.recogniser = recogniser;
            System.Console.WriteLine("4");
        }

        [HttpPut]
        public ActionResult<RecognisionResult> PutImage(SingleImage image)
        {
            System.Console.WriteLine("image");
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
            System.Console.WriteLine("id " + id);
            RecognisionData? b = db.Load(id);
            if (b != null)
                return b;
            else
                return StatusCode(404, "RecognisionData with given id is not found");
        }

        /*[HttpGet]
        [Route("all")]
        public ActionResult<int[]> GetIds()
        {
            System.Console.WriteLine("all");
            return db.LoadIds();
        }*/

        [HttpDelete]
        public ActionResult DeleteClear()
        {
            System.Console.WriteLine("clear");
            db.Clear();
            return Ok();
        }
    }
}
