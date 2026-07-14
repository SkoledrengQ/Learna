using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface ITermRepository
{
    Task<IEnumerable<Term>> GetBySchoolYearIdAsync(int schoolYearId);
    Task<Term?> GetByIdAsync(int id);
    Task<Term> CreateAsync(Term term);
    Task<Term> UpdateAsync(Term term);
    Task<bool> DeleteAsync(int id);
}
