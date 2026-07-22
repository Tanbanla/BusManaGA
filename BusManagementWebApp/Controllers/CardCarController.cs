using BusManagement.Model;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;
using BusManagementWebApp.ViewModels.CardCar;
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
        private readonly ITmCardUsageService _tmCardUsageService;
        private readonly IWebHostEnvironment _env;

        public CardCarController(ILogger<CardCarController> logger, ITmCardService tmCardService, ISectionService sectionService,
            IWebHostEnvironment env, ITmEmployeeService tmEmployeeService, ITmCardBorrowService tmCardBorrowService, ITmCardUsageService tmCardUsageService)
        {
            _logger = logger;
            _tmCardService = tmCardService;
            _sectionService = sectionService;
            _tmEmployeeService = tmEmployeeService;
            _tmCardUsageService = tmCardUsageService;
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
        public async Task<IActionResult> UpdateCardTaxiUsage([FromBody] TM_CARD_USAGE request)
        {
            if (request == null)
            {
                return StatusCode(500, "Request cannot be null.");
            }

            try
            {
                // update user create
                var user = User?.Claims?.FirstOrDefault(c => c.Type == "adid")?.Value ?? "vietdo bad";
                request.CHR_USER_UPDATE = user;
                request.DTM_UPDATE = DateTime.Now;

                var result = await _tmCardUsageService.UpdateCardUsageByCardIdAsync(request);
                if (!result.Success)
                {
                    return StatusCode(result.Status ?? 500, result.Message ?? "An error occurred while updating card taxi usage.");
                }
                return Ok(result.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating card taxi usage.");
                return StatusCode(500, "An error: " + ex.Message);
            }
            ;
        }
        // Get Detail Card Usage
        [HttpGet]
        public async Task<IActionResult> GetDetailCardUsage(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid card ID.");
            }
            try
            {
                var result = await _tmCardUsageService.GetByIdAsync(id);
                if (!result.Success)
                {
                    return StatusCode(result.Status ?? 500, result.Message ?? "An error occurred while processing your request.");
                }
                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving card usage details for card ID: {id}");
                return StatusCode(500, "An error: " + ex.Message);
            }
        }

        // Add card borrow taxi
        [HttpPost]
        public async Task<IActionResult> AddCardBorrowTaxi([FromBody] TM_CARD_BORROW request)
        {
            if (request == null)
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
        [HttpGet]
        public async Task<IActionResult> CardUsingCar(
            string? employeeId,
            string? department,
            int page = 1,
            int pageSize = 10)
        {
            var sectionsResp = await _sectionService.GetAllAsync();
            ViewBag.Sections = sectionsResp?.Success == true ? sectionsResp.Data : null;

            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var cardResp = await _tmCardService.GetCardUsing(employeeId,department,page,pageSize);

            var vm = new UsingCardViewModel
            {
                EmployeeId = employeeId,
                Department = department,

                PageNumber = page,
                PageSize = pageSize,

                Cards = cardResp?.Data?.Data?.ToList() ?? new List<dynamic>(),

                TotalCount = (int)(cardResp?.Data?.TotalCount ?? 0),
                TotalPages = (int)(cardResp?.Data?.TotalPages ?? 0)
            };

            if (vm.TotalPages <= 0)
            {
                vm.TotalPages = vm.TotalCount > 0
                    ? (int)Math.Ceiling(vm.TotalCount / (double)vm.PageSize)
                    : 1;
            }

            return View(vm);
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
        // lấy dữ liệu các thể đang cho mượn
        [HttpGet]

        public async Task<IActionResult> GetListCardUsingTaxi()
        {
            try
            {
                var result = await _tmCardBorrowService.GetListCardUsingTaxiAsync();
                if (!result.Success)
                {
                    return StatusCode(result.Status ?? 500, result.Message ?? "An error occurred while processing your request.");
                }
                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving using taxi cards.");
                return StatusCode(500, "An error: " + ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> AddCardUsage([FromBody] TM_CARD_USAGE request)
        {
            if (request == null)
            {
                return BadRequest("Request cannot be null.");
            }
            try
            {
                var user = User?.Claims?.FirstOrDefault(c => c.Type == "adid")?.Value ?? "vietdo bad";
                request.CHR_USER_CREATE = user;
                var result = await _tmCardUsageService.AddAsync(request);
                if (!result.Success)
                {
                    return BadRequest("Lỗi khi thêm dữ liệu: " + result.Message);
                }
                return Ok(result.Success);
            }
            catch (Exception ex)
            {
                return BadRequest("An error occurred while adding card usage: " + ex.Message);
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
