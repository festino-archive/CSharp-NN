using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab.Controllers
{
    [ApiController]
    [Route("[controller]")]
    internal class RecognisionController : ControllerBase
    {
        private IRecognisionStorage db;

        public RecognisionController(IRecognisionStorage storage)
        {
            db = storage;

            
        }
    }
}
