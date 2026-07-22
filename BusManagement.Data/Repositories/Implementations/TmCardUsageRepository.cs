using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Common;
using BusManagement.Model.Entity;
using BusManagement.Model.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BusManagement.Data.Repositories.Implementations;

public class TmCardUsageRepository : BaseRepository<TM_CARD_USAGE, int>, ITmCardUsageRepository
{
    private readonly BusManagementDBContext _context;
    public TmCardUsageRepository(BusManagementDBContext context, IOptions<ConnectionStringOptions> options, IConfiguration configuration) : base(context, options, configuration)
    {
        _context = context;
    }
    // update Card Usage by card id
    public async Task<bool> UpdateCardUsageByCardIdAsync(TM_CARD_USAGE cardUsage)
    {
        var existingCardUsage = await _context.TM_CARD_USAGEs.FindAsync(cardUsage.ID);
        if (existingCardUsage == null)
        {
            return false;
        }
        existingCardUsage.CHR_EMPLOYEE_ID = cardUsage.CHR_EMPLOYEE_ID;
        existingCardUsage.NVCHR_EMPLOYEE_NAME = cardUsage.NVCHR_EMPLOYEE_NAME;
        existingCardUsage.CHR_USER_UPDATE = cardUsage.CHR_USER_UPDATE;
        existingCardUsage.DTM_UPDATE = cardUsage.DTM_UPDATE;
        existingCardUsage.DTM_USE_DATE= cardUsage.DTM_USE_DATE;
        existingCardUsage.NVCHR_FROM_ADDRESS = cardUsage.NVCHR_FROM_ADDRESS;
        existingCardUsage.NVCHR_TO_ADDRESS = cardUsage.NVCHR_TO_ADDRESS;
        existingCardUsage.INT_AMOUNT = cardUsage.INT_AMOUNT;
        existingCardUsage.NVCHR_NOTE = cardUsage.NVCHR_NOTE;
        existingCardUsage.CHR_SEC_COST = cardUsage.CHR_SEC_COST;
        existingCardUsage.CHR_TRAN_CODE = cardUsage.CHR_TRAN_CODE;
        existingCardUsage.INT_NUMBER_MOVE = cardUsage.INT_NUMBER_MOVE;
        existingCardUsage.BIT_DELETE = cardUsage.BIT_DELETE;
        existingCardUsage.NVCHR_REASON = cardUsage.NVCHR_REASON;
        await _context.SaveChangesAsync();
        return true;
    }
}
