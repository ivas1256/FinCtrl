using FinCtrl.Backend.Core.RestAPI.BL.Implementation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

namespace FinCtrl.Backend.Core.RestAPI.Controllers
{
    [Route("api/upload_payments")]
    [ApiController]
    public class UploadPaymentsController : ControllerBase
    {
        ExcelFileLoader loader;

        public UploadPaymentsController(ExcelFileLoader loader)
        {
            this.loader = loader;
        }

        [HttpPost]
        public void UploadFile(IFormFile file)
        {
            if (Path.GetExtension(file.FileName) != ".xlsx")
                throw new Exception("Accept only .xlsx files");

            loader.Upload(file.OpenReadStream());
        }
    }    
}
