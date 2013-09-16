using System;

namespace Inedo.BuildMasterExtensions.CollabNet
{
    /// <summary>
    /// Represents an issue tracker status for CollabNet.
    /// </summary>
    [Serializable]
    public sealed class IssueStatus
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IssueStatus"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="statusClass">The status class.</param>
        public IssueStatus(string name, string statusClass)
        {
            this.Name = name;
            this.Class = statusClass;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the issue class.
        /// </summary>
        public string Class { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
