using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeckTracker.Domain.DTO
{
    public sealed class UploadedReplay
    {
        public int Id { get; set; }
        public string ExternalId { get; set; }
        public string GameType { get; set; }
        public string BlobName { get; set; }
        [Column(TypeName = "timestamptz")] public DateTime UploadTime { get; set; }
        [Column(TypeName = "timestamptz")] public DateTime? ProcessTime { get; set; }
        public string DeckTrackerVersion { get; set; }
        public string GameVersion { get; set; }
        [Required, DefaultValue(0)] public int ErrorCount { get; set; }
        public string ErrorMessage { get; set; }
        [Column(TypeName = "timestamptz")] public DateTime? ErrorTime { get; set; }
    }
}
