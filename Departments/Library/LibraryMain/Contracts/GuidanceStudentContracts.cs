namespace LibraryMain.Contracts;

public record StudentSummaryDto(Guid Id, string StudentNumber, string FullName, string Email);
public record IncomingReferralDto(Guid StudentId, string ReferralReason, string OriginatingDeptCode);

