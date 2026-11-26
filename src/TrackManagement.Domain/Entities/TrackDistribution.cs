using TrackManagement.Domain.Enums;

namespace TrackManagement.Domain.Entities;

public class TrackDistribution
{
    public int Id { get; set; }

    public int TrackId { get; set; }
    public Track Track { get; set; } = null!;

    public int DspId { get; set; }
    public Dsp Dsp { get; set; } = null!;

    /// <summary>UTC timestamp of when the track was submitted to the DSP.</summary>
    public DateTime SubmittedAt { get; set; }
    public DistributionStatus Status { get; set; } = DistributionStatus.Pending;
}
