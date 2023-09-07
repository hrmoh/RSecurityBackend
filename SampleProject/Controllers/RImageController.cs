using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using RSecurityBackend.Controllers;
using RSecurityBackend.Services;

namespace SampleProject.Controllers
{
    /// <summary>
    /// Generic Image Provider
    /// </summary>
    [Produces("application/json")]
    [Route("api/rimages")]
    public class RImageController : RImageControllerBase
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="pictureFileService"></param>
        /// <param name="memoryCache"></param>
        public RImageController(IImageFileService pictureFileService, IMemoryCache memoryCache) : base(pictureFileService, memoryCache)
        {

        }
    }
}
