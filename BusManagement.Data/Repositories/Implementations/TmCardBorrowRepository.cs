using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Common;
using BusManagement.Model.Entity;
using BusManagement.Model.Models;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BusManagement.Data.Repositories.Implementations;

public class TmCardBorrowRepository : BaseRepository<TM_CARD_BORROW, int>, ITmCardBorrowRepository
{
    private readonly BusManagementDBContext _context;
    public TmCardBorrowRepository(BusManagementDBContext context, IOptions<ConnectionStringOptions> options, IConfiguration configuration) : base(context, options, configuration)
    {
        _context = context;
    }
    // list card taxi brorrowing
    public async Task<List<TM_CARD_BORROW>> GetListCardUsingTaxiAsync()
    {
        var sql = @"SELECT distinct c.*
                    FROM TM_CARD_BORROW as c
                    WHERE BIT_END = 0
					and INT_CARD_ID in (SELECT ID FROM TM_CARD where BIT_IS_ACTIVE = 1)
                    ";
        var result = (await _conn.QueryAsync<TM_CARD_BORROW>(sql)).ToList();
        return result;
    }
}
