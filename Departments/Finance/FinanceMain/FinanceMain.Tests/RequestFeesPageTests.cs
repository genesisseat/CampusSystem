using FinanceMain.Contracts;
using FinanceMain.Contracts.Dtos;
using FinanceMain.Services;

namespace FinanceMain.Tests;

public class RequestFeesPageTests
{
    [Fact]
    public async Task NoOpRequestService_ShouldProvideDemoDocumentTypesAndCurrentStudentRequests()
    {
        var service = new NoOpRequestService();

        var documentTypes = await service.GetDocumentFeeTypesAsync();
        var requests = await service.GetRequestsForStudentAsync(Guid.Parse("8c2f7bc9-31f2-4c36-9a4f-3f61d5da8b12"));

        Assert.NotNull(documentTypes);
        Assert.NotEmpty(documentTypes);
        Assert.Contains(documentTypes, dt => dt.Name == "Transcript of Records" && dt.Amount == 250m);
        Assert.NotNull(requests);
        Assert.NotEmpty(requests);
        Assert.Contains(requests, req => req.DocumentType == "Transcript of Records");
    }
}
