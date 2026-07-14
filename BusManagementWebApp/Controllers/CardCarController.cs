using BusManagement.Model;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BusManagementWebApp.Controllers
{
    public class CardCarController : Controller
    {

        private readonly ILogger<CardCarController> _logger;
        private readonly ITmCardService _tmCardService;
        private readonly ISectionService _sectionService;
        private readonly ITmEmployeeService _tmEmployeeService;
        private readonly ITmCardBorrowService _tmCardBorrowService;
        private readonly IWebHostEnvironment _env;

        public CardCarController(ILogger<CardCarController> logger, ITmCardService tmCardService, ISectionService sectionService,
            IWebHostEnvironment env, ITmEmployeeService tmEmployeeService, ITmCardBorrowService tmCardBorrowService )
        {
            _logger = logger;
            _tmCardService = tmCardService;
            _sectionService = sectionService;
            _tmEmployeeService = tmEmployeeService;
            _env = env;
            _tmCardBorrowService = tmCardBorrowService;
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
                return StatusCode(500, "An error: " + ex.Message);

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
            catch (Exception ex)
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
        // Add card borrow taxi
        [HttpPost]
        public async Task<IActionResult> AddCardBorrowTaxi([FromBody] TM_CARD_BORROW request)
        {
            if(request == null)
            {
                return BadRequest("Request cannot be null.");
            }
            var result = await _tmCardBorrowService.AddAsync(request);
            if (!result.Success)
            {
                return BadRequest("Lỗi khi thêm dữ liệu: " + result.Message);
            }

            return Ok(result.Success);
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

        // Hàm lấy dữ liệu chung 
        [HttpGet]
        public async Task<IActionResult> GetListCardTaxiUsable() // Lấy thông tin thẻ xe taxi
        {
            try
            {
                var result = await _tmCardService.GetListCardTaxiUsableAsync();
                if (!result.Success)
                {
                    return StatusCode(result.Status ?? 500, result.Message ?? "An error occurred while processing your request.");
                }
                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving usable taxi cards.");
                return StatusCode(500, "An error: " + ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetInforEmployeeByCode([FromBody] string employeeCode)
        {
            if (string.IsNullOrEmpty(employeeCode))
            {
                return StatusCode(500, "Employee code cannot be null or empty.");
            }
            try
            {
                // Assuming you have a service method to get employee info by code
                var result = await _tmEmployeeService.GetEmployeeByCodeAsync(employeeCode);
                if (!result.Success)
                {
                    return StatusCode(result.Status ?? 500, result.Message ?? "An error occurred while processing your request.");
                }
                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving employee info for code: {employeeCode}");
                return StatusCode(500, "An error: " + ex.Message);
            }
        }
    }
}
