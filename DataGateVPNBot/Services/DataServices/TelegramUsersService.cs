using DataGateVPNBot.Models;
using DataGateVPNBot.Services.DataServices.Interfaces;

namespace DataGateVPNBot.Services.DataServices;

public class TelegramUsersService : ITelegramUsersService
{
    private readonly ILogger<TelegramUsersService> _logger;

    public TelegramUsersService(ILogger<TelegramUsersService> logger)
    {
        _logger = logger;
    }
    
    public async Task RegisterUserAsync(long telegramId, string? username, string? firstName, string? lastName,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        //for example best way if you don't need change data in DB
        // var existingUser = await _unitOfWork.GetQuery<TelegramUser>()
        //     .AsQueryable().FirstOrDefaultAsync(x => x.TelegramId == telegramId);
        
        // var telegramUserRepository = _unitOfWork.GetRepository<TelegramUser>();
        // var existingUser = await telegramUserRepository.Query
        //     .FirstOrDefaultAsync(u => u.TelegramId == telegramId, cancellationToken: cancellationToken);
        //
        // if (existingUser == null)
        // {
        //     var user = new TelegramUser
        //     {
        //         TelegramId = telegramId,
        //         Username = username,
        //         FirstName = firstName,
        //         LastName = lastName
        //     };
        //
        //     await telegramUserRepository.AddAsync(user, cancellationToken);
        //     await _unitOfWork.SaveChangesAsync(cancellationToken);
        // }
    }

    public async Task<List<TelegramUser>?> GetAdminsAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        
        // var existingUser = await _unitOfWork.GetQuery<TelegramUser>().AsQueryable()
        //     .Where(u => u.TelegramId == 5767006971).ToListAsync(cancellationToken: cancellationToken);
        // // var existingUser = await _telegramUserQuery.GetAdmins().ToListAsync();
        // // var telegramUserRepository = _unitOfWork.GetRepository<TelegramUser>();
        // // var existingUser = await telegramUserRepository.Query
        // //     .Where(u => u.TelegramId == 5767006971).ToListAsync();
        // if (existingUser is { Count: 0 })
        // {
        //     _logger.LogError("Admins for telegram bot not found, returning empty list");
        //     return new List<TelegramUser>();
        // }
        //
        // return existingUser;
    }
}
