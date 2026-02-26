using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SettlementTracker.Core.Models.Entities;

namespace SettlementTracker.Core.Services
{
    public interface ICitizenService
    {
        Task<IEnumerable<Citizen>> GetCitizensAsync();
        Task<Citizen?> GetCitizenAsync(Guid id);
        Task AddCitizenAsync(Citizen citizen);
        Task UpdateCitizenAsync(Citizen citizen);
        Task DeleteCitizenAsync(Guid id);
        Task SaveChangesAsync();
        Task LoadFromFileAsync();

        public event EventHandler? CitizensChange;
    }
}