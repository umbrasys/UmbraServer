using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MareSynchronosShared.Models;

public class EstablishmentEvent
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EstablishmentId { get; set; }

    [ForeignKey(nameof(EstablishmentId))]
    public Establishment Establishment { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; }

    public string? Description { get; set; }

    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public int Recurrence { get; set; }
    public DateTime CreatedUtc { get; set; }
}
