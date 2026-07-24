using BusManagement.Model;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using DocumentFormat.OpenXml.Spreadsheet;

namespace BusManagement.Service.Services.Interfaces;

public interface ITmCardPrintQueueService : IBaseService<TM_CARD_PRINT_QUEUE, int, TM_CARD_PRINT_QUEUEDTO>
{   
    // search card print queue with pagination
    Task<GenericResponse<NotCardViewModel>> SearchCardPrintQueueAsync(SearchCard searchCard);
    // Delete card print queue 
    Task<GenericResponse<bool>> DeleteCardPrintQueuesAsync(List<int> ids);
    // Print card queue 
    Task<GenericResponse<bool>> PrintCardQueuesAsync(List<int> ids,string user);
}
