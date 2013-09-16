using System;
using System.Collections.Generic;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;

namespace Inedo.BuildMasterExtensions.CollabNet
{
    /// <summary>
    /// Represents an issue tracker category for CollabNet.
    /// </summary>
    [Serializable]
    public sealed class TrackerCategory : CategoryBase
    {
        private static readonly TrackerCategory[] EmptyArray = new TrackerCategory[0];

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackerCategory"/> class.
        /// </summary>
        /// <param name="categoryId">The category ID.</param>
        /// <param name="categoryName">Name of the category.</param>
        /// <param name="subcategories">The subcategories.</param>
        public TrackerCategory(string categoryId, string categoryName, IEnumerable<TrackerCategory> subcategories)
            : base(categoryId, categoryName, new List<TrackerCategory>(subcategories).ToArray())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackerCategory"/> class.
        /// </summary>
        /// <param name="categoryId">The category ID.</param>
        /// <param name="categoryName">Name of the category.</param>
        public TrackerCategory(string categoryId, string categoryName)
            : base(categoryId, categoryName, EmptyArray)
        {
        }
    }
}
