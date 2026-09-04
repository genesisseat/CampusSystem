using FinanceMain.Contracts;

namespace FinanceMain.Services;

public interface ICounselorTriageService
{
    Task<IReadOnlyList<StudentRequestResponse>> ListAsync(TriageFilter filter, CancellationToken cancellationToken);
    Task<ServiceResult<StudentRequestResponse>> TransitionAsync(string actorId, TransitionRequest transition, CancellationToken cancellationToken);
}
