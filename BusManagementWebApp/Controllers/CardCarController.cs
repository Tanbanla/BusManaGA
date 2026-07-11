using BusManagement.Model;
using BusManagement.Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BusManagementWebApp.Controllers
{
    public class CardCarController : Controller
    {

        private readonly ILogger<CardCarController> _logger;
        private readonly ITmCardService _tmCardService;
        private readonly ISectionService _sectionService;
        private readonly IWebHostEnvironment _env;

        public CardCarController(ILogger<CardCarController> logger, ITmCardService tmCardService, ISectionService sectionService, IWebHostEnvironment env)
        {
            _logger = logger;
            _tmCardService = tmCardService;
            _sectionService = sectionService;
            _env = env;
        }

        // MARK: Screen: Card Taxi
        public async Task<IActionResult> CardTaxi()
        {
            var sectionsResp = await _sectionService.GetAllAsync();
            ViewBag.Sections = sectionsResp?.Success == true ? sectionsResp.Data : null;

            return View();
        }
        // Search Card Taxi
        [HttpPost]
        public async Task<IActionResult> SearchCardTaxi([FromBody] SearchCard serach)
        {
            if (serach == null) return BadRequest("Search parameters cannot be null.");

            try
            {
                var result = await _tmCardService.GetCardByTaxiAsync(serach);

                if (!result.Success)
                {
                    return StatusCode(result.Status ?? 500, result.Error ?? "An error occurred while processing your request.");
                }

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while searching for card taxi.");
                return StatusCode(500, "An error: "+ex.Message);

            }
        }
        // Download Excel Taxi 
        [HttpPost]
        public async Task<IActionResult> DownloadExcelTaxi([FromBody] SearchCard serach)
        {
            if (serach == null) return BadRequest("Search parameters cannot be null.");

            try
            {
                var result = await _tmCardService.DownloadExcelTaxiAsync(serach);
                if (!result.Success)
                {
                    return StatusCode(result.Status ?? 500, result.Message ?? "An error occurred while processing your request.");
                }

                if (result.Data == null || result.Data.Length == 0)
                {
                    return StatusCode(500, "Không có dữ liệu file để tải xuống.");
                }

                var fileName = $"FileTaxiCard_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                return File(result.Data, contentType, fileName);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occurred while downloading excel taxi file.");
                return StatusCode(500, "An error: " + ex.Message);
            }
        }
        // Update card taxi 
        [HttpPost]
        public async Task<IActionResult> UpdateCardTaxi()
        {
            return Ok();
        }
        // Add card taxi
        [HttpPost]
        public async Task<IActionResult> AddCardTaxi()
        {
            return Ok();
        }
        // Delete card taxi
        [HttpPost]
        public async Task<IActionResult> DeleteCardTaxi([FromBody] List<int> Ids)
        {
            if (Ids == null || !Ids.Any())
            {
                return BadRequest("No IDs provided for deletion.");
            }
            var result = await _tmCardService.DeleteCardByTaxiAsync(Ids);
            if (!result.Success)
            {
                return BadRequest("Lỗi khi xóa dữ liệu: " + result.Message);
            }
            return Ok();
        }

        // MARK: Screen: Card using for car
        public IActionResult CardUsingCar()
        {
            return View();
        }
        // MARK: Screen: Not Card
        public IActionResult NotCard()
        {
            return View();
        }
    }
}
