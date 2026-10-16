using System.ComponentModel.DataAnnotations;

namespace ExpiryTrack.Api.DTO;

public class SubmitRenewalRequest
{
    [Required]
    public DateOnly ProposedExpirationDate { get; set; }

    [Range(0.01, 9999999.99)]
    public decimal? ProposedCost { get; set; }
    [Required]
    public string? Justification { get; set; }
}