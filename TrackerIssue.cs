using System;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;

namespace Inedo.BuildMasterExtensions.CollabNet
{
    /// <summary>
    /// Represents an issue in CollabNet's tracker.
    /// </summary>
    [Serializable]
    public sealed class TrackerIssue : Issue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrackerIssue"/> class.
        /// </summary>
        /// <param name="row">The artifact row to read.</param>
        /// <param name="release">The release.</param>
        public TrackerIssue(CollabNetTrackerService.ArtifactSoapRow row, string release)
            : base(row.id, row.status, row.title, row.description, release)
        {
            this.IsClosed = row.statusClass == "Closed";
        }

        /// <summary>
        /// Gets or sets a value indicating whether the issue is closed.
        /// </summary>
        public bool IsClosed { get; private set; }
    }
}
