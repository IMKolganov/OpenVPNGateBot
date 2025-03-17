using DataGateVPNBot.DataBase.Repositories.Queries.Interfaces;
using DataGateVPNBot.DataBase.UnitOfWork;
using DataGateVPNBot.Models;
using DataGateVPNBot.Services.DataServices.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataGateVPNBot.Services.DataServices;

public class TelegramUsersService : ITelegramUsersService
{
    private readonly IUnitOfWork _unitOfWork;
    public TelegramUsersService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task RegisterUserAsync(long telegramId, string? username, string? firstName, string? lastName,
        CancellationToken cancellationToken)
    {
        //for example best way if you don't need change data in DB
        // var existingUser = await _unitOfWork.GetQuery<TelegramUser>()
        //     .AsQueryable().FirstOrDefaultAsync(x => x.TelegramId == telegramId);
        
        var telegramUserRepository = _unitOfWork.GetRepository<TelegramUser>();
        var existingUser = await telegramUserRepository.Query
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId, cancellationToken: cancellationToken);

        if (existingUser == null)
        {
            var user = new TelegramUser
            {
                TelegramId = telegramId,
                Username = username,
                FirstName = firstName,
                LastName = lastName
            };

            await telegramUserRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<TelegramUser>?> GetAdminsAsync(CancellationToken cancellationToken)
    {
        var existingUser = await _unitOfWork.GetQuery<TelegramUser>().AsQueryable()
            .Where(u => u.TelegramId == 5767006971).ToListAsync(cancellationToken: cancellationToken);
        // var existingUser = await _telegramUserQuery.GetAdmins().ToListAsync();
        // var telegramUserRepository = _unitOfWork.GetRepository<TelegramUser>();
        // var existingUser = await telegramUserRepository.Query
        //     .Where(u => u.TelegramId == 5767006971).ToListAsync();
        if (existingUser == null)
        {
            throw new Exception("User not found");
        }
        
        return existingUser;
    }
}
