using System;
using System.Collections.Generic;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.CollabNet
{
    /// <summary>
    /// CollabNet TeamForge issue tracker provider.
    /// </summary>
    [ProviderProperties(
      "CollabNet TeamForge",
      "Supports CollabNet TeamForge 5.3 and later.")]
    [CustomEditor(typeof(CollabNetTrackerProviderEditor))]
    public sealed class CollabNetTrackerProvider : IssueTrackingProviderBase, ICategoryFilterable, IUpdatingProvider
    {
        private const string CollabNetServicePath = "/ce-soap50/services/CollabNet";
        private const string TrackerAppServicePath = "/ce-soap50/services/TrackerApp";
        private const string FrsAppServicePath = "/ce-soap50/services/FrsApp";
        private const string CollabNetUrlFormatString = "/sf/go/{0}";

        private CollabNetService.CollabNetSoapService collabNetService;
        private CollabNetTrackerService.TrackerAppSoapService trackerAppService;
        private CollabNetFrsAppService.FrsAppSoapService frsAppService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollabNetTrackerProvider"/> class.
        /// </summary>
        public CollabNetTrackerProvider()
        {
            this.CategoryIdFilter = new string[0];
        }

        /// <summary>
        /// Gets or sets the URL of the CollabNet server.
        /// </summary>
        [Persistent]
        public string BaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the user name to use when connecting to the CollabNet server.
        /// </summary>
        [Persistent]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the password which corresponds to the UserName property.
        /// </summary>
        [Persistent]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the field on artifacts that specifies the release number.
        /// </summary>
        [Persistent]
        public string ReleaseField { get; set; }

        /// <summary>
        /// Gets or sets the category ID filter.
        /// </summary>
        public string[] CategoryIdFilter { get; set; }

        /// <summary>
        /// Gets an inheritor-defined array of category types.
        /// </summary>
        /// <example>
        /// In FogBugz, this could either be simply "Project" or "Client", "Project", "Area", depending on
        /// the implementer.
        /// </example>
        public string[] CategoryTypeNames
        {
            get { return new[] { "Project", "Tracker" }; }
        }

        /// <summary>
        /// Determines whether an issue's description can be appended to.
        /// </summary>
        public bool CanAppendIssueDescriptions
        {
            get { return true; }
        }

        /// <summary>
        /// Determines whether an issue's status can be changed.
        /// </summary>
        public bool CanChangeIssueStatuses
        {
            get { return true; }
        }

        /// <summary>
        /// Determines whether an issue can be closed.
        /// </summary>
        public bool CanCloseIssues
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the CollabNet web service proxy.
        /// </summary>
        private CollabNetService.CollabNetSoapService CollabNet
        {
            get { return this.collabNetService ?? (this.collabNetService = new CollabNetService.CollabNetSoapService() { Url = CombinePaths(this.BaseUrl, CollabNetServicePath) }); }
        }

        /// <summary>
        /// Gets the TrackerApp web service proxy.
        /// </summary>
        private CollabNetTrackerService.TrackerAppSoapService TrackerApp
        {
            get { return this.trackerAppService ?? (this.trackerAppService = new CollabNetTrackerService.TrackerAppSoapService() { Url = CombinePaths(this.BaseUrl, TrackerAppServicePath) }); }
        }

        /// <summary>
        /// Gets the FrsApp web service proxy.
        /// </summary>
        private CollabNetFrsAppService.FrsAppSoapService FrsApp
        {
            get { return this.frsAppService ?? (this.frsAppService = new CollabNetFrsAppService.FrsAppSoapService() { Url = CombinePaths(this.BaseUrl, FrsAppServicePath) }); }
        }

        /// <summary>
        /// Gets a URL to the specified issue.
        /// </summary>
        /// <param name="issue">The issue whose URL is returned.</param>
        /// <returns>
        /// The URL of the specified issue if applicable; otherwise null.
        /// </returns>
        public override string GetIssueUrl(IssueTrackerIssue issue)
        {
            if (issue == null)
                throw new ArgumentNullException("issue");

            return CombinePaths(this.BaseUrl, string.Format(CollabNetUrlFormatString, issue.IssueId));
        }

        /// <summary>
        /// Gets an array of <see cref="Issue"/> objects that are for the current
        /// release.
        /// </summary>
        /// <param name="releaseNumber"></param>
        /// <returns></returns>
        public override IssueTrackerIssue[] GetIssues(string releaseNumber)
        {
            if (this.CategoryIdFilter == null || this.CategoryIdFilter.Length < 2 || string.IsNullOrEmpty(this.CategoryIdFilter[1]))
                throw new InvalidOperationException("CollabNet issue tracker has not been specified.");

            var trackerId = this.CategoryIdFilter[1];

            var sessionId = this.CollabNet.login(this.UserName, this.Password);
            if (string.IsNullOrEmpty(sessionId))
                throw new NotAvailableException();

            try
            {
                var issueList = this.TrackerApp.getArtifactList(sessionId, trackerId, new CollabNetTrackerService.SoapFilter[0]);

                var issues = new List<TrackerIssue>();
                foreach (var issue in issueList.dataRows)
                {
                    bool add = true;

                    if (!string.IsNullOrEmpty(this.ReleaseField))
                    {
                        var data = this.TrackerApp.getArtifactData(sessionId, issue.id);
                        var issueReleaseId = GetFieldValue(data, this.ReleaseField);
                        var issueReleaseName = GetReleaseName(sessionId, issueReleaseId);
                        
                        add = releaseNumber == issueReleaseName;
                    }

                    if(add)
                        issues.Add(new TrackerIssue(issue, releaseNumber));
                }

                return issues.ToArray();
            }
            finally
            {
                this.CollabNet.logoff(this.UserName, sessionId);
            }
        }

        /// <summary>
        /// Determines if the specified issue is closed.
        /// </summary>
        /// <param name="issue"></param>
        /// <returns></returns>
        public override bool IsIssueClosed(IssueTrackerIssue issue)
        {
            return ((TrackerIssue)issue).IsClosed;
        }

        /// <summary>
        /// When implemented in a derived class, indicates whether the provider
        /// is installed and available for use in the current execution context.
        /// </summary>
        /// <returns></returns>
        public override bool IsAvailable()
        {
            return true;
        }

        /// <summary>
        /// When implemented in a derived class, attempts to connect with the
        /// current configuration and, if not successful, throws a
        /// <see cref="ConnectionException"/>
        /// </summary>
        public override void ValidateConnection()
        {
            var sessionId = string.Empty;

            try
            {
                sessionId = this.CollabNet.login(this.UserName, this.Password);
            }
            catch (Exception ex)
            {
                throw new NotAvailableException(ex.Message, ex);
            }

            if (string.IsNullOrEmpty(sessionId))
                throw new NotAvailableException();

            this.CollabNet.logoff(this.UserName, sessionId);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "Connects to the issue tracking system of CollabNet TeamForge.";
        }

        /// <summary>
        /// Returns an array of all appropriate categories defined within the provider
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// The nesting level (i.e. <see cref="CategoryBase.SubCategories"/>) can never be less than
        /// the length of <see cref="CategoryTypeNames"/>
        /// </remarks>
        public IssueTrackerCategory[] GetCategories()
        {
            var sessionId = this.CollabNet.login(this.UserName, this.Password);
            if (string.IsNullOrEmpty(sessionId))
                throw new NotAvailableException();

            try
            {
                var categories = new List<TrackerCategory>();

                var projectList = this.CollabNet.getProjectList(sessionId);
                foreach (var project in projectList.dataRows)
                {
                    var trackers = new List<TrackerCategory>();

                    var trackerList = this.TrackerApp.getTrackerList(sessionId, project.id);
                    foreach (var tracker in trackerList.dataRows)
                        trackers.Add(new TrackerCategory(tracker.id, tracker.title));

                    categories.Add(new TrackerCategory(project.id, project.title, trackers));
                }

                return categories.ToArray();
            }
            finally
            {
                this.CollabNet.logoff(this.UserName, sessionId);
            }
        }

        /// <summary>
        /// Appends the specified text to the specified issue.
        /// </summary>
        /// <param name="issueId"></param>
        /// <param name="textToAppend"></param>
        public void AppendIssueDescription(string issueId, string textToAppend)
        {
            if (string.IsNullOrEmpty(issueId))
                throw new ArgumentNullException("issueId");
            if (string.IsNullOrEmpty(textToAppend))
                return;

            var sessionId = this.CollabNet.login(this.UserName, this.Password);
            if (string.IsNullOrEmpty(sessionId))
                throw new NotAvailableException();

            try
            {
                var issueData = this.TrackerApp.getArtifactData(sessionId, issueId);
                issueData.description = (issueData.description ?? string.Empty) + textToAppend;
                this.TrackerApp.setArtifactData(sessionId, issueData, string.Empty, null, null, null);
            }
            finally
            {
                this.CollabNet.logoff(this.UserName, sessionId);
            }
        }

        /// <summary>
        /// Changes the specified issue's status.
        /// </summary>
        /// <param name="issueId"></param>
        /// <param name="newStatus"></param>
        public void ChangeIssueStatus(string issueId, string newStatus)
        {
            if (string.IsNullOrEmpty(issueId))
                throw new ArgumentNullException("issueId");

            if (this.CategoryIdFilter == null || this.CategoryIdFilter.Length < 2 || string.IsNullOrEmpty(this.CategoryIdFilter[1]))
                throw new InvalidOperationException("CollabNet issue tracker has not been specified.");

            var trackerId = this.CategoryIdFilter[1];

            var sessionId = this.CollabNet.login(this.UserName, this.Password);
            if (string.IsNullOrEmpty(sessionId))
                throw new NotAvailableException();

            try
            {
                var statuses = new List<IssueStatus>(GetAvailableStatuses(sessionId, trackerId));
                var match = statuses.Find(i => i.Name == newStatus);
                if (match == null)
                    throw new ArgumentException("Invalid issue status.");

                var issueData = this.TrackerApp.getArtifactData(sessionId, issueId);
                issueData.status = match.Name;
                issueData.statusClass = match.Class;
                this.TrackerApp.setArtifactData(sessionId, issueData, string.Empty, null, null, null);
            }
            finally
            {
                this.CollabNet.logoff(this.UserName, sessionId);
            }
        }

        /// <summary>
        /// Closes the specified issue.
        /// </summary>
        /// <param name="issueId"></param>
        public void CloseIssue(string issueId)
        {
            if (string.IsNullOrEmpty(issueId))
                throw new ArgumentNullException("issueId");

            if (this.CategoryIdFilter == null || this.CategoryIdFilter.Length < 2 || string.IsNullOrEmpty(this.CategoryIdFilter[1]))
                throw new InvalidOperationException("CollabNet issue tracker has not been specified.");

            var trackerId = this.CategoryIdFilter[1];

            var sessionId = this.CollabNet.login(this.UserName, this.Password);
            if (string.IsNullOrEmpty(sessionId))
                throw new NotAvailableException();

            try
            {
                var closedStatus = GetClosedStatus(sessionId, trackerId);
                if (closedStatus == null)
                    throw new InvalidOperationException("The issue tracker does not have a closed status defined.");

                var issueData = this.TrackerApp.getArtifactData(sessionId, issueId);
                issueData.status = closedStatus.Name;
                issueData.statusClass = closedStatus.Class;
                this.TrackerApp.setArtifactData(sessionId, issueData, string.Empty, null, null, null);
            }
            finally
            {
                this.CollabNet.logoff(this.UserName, sessionId);
            }
        }

        /// <summary>
        /// Combines two URL's.
        /// </summary>
        /// <param name="baseUrl">Root URL element.</param>
        /// <param name="relativeUrl">Relative URL element.</param>
        /// <returns>Combined URL.</returns>
        private static string CombinePaths(string baseUrl, string relativeUrl)
        {
            if (baseUrl.EndsWith("/"))
            {
                return relativeUrl.StartsWith("/")
                    ? baseUrl + relativeUrl.Substring(1, relativeUrl.Length - 1)
                    : baseUrl + relativeUrl;
            }
            else
            {
                return relativeUrl.StartsWith("/")
                    ? baseUrl + relativeUrl
                    : baseUrl + "/" + relativeUrl;
            }
        }

        /// <summary>
        /// Returns the value of an arbitrary artifact field.
        /// </summary>
        /// <param name="artifact">Artifact whose value is returned.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>Value of the field if it is defined; otherwise null.</returns>
        private static string GetFieldValue(CollabNetTrackerService.ArtifactSoapDO artifact, string fieldName)
        {
            var prop = typeof(CollabNetTrackerService.ArtifactSoapDO).GetProperty(fieldName);
            if (prop == null)
            {
                if (artifact.flexFields == null || artifact.flexFields.names == null || artifact.flexFields.values == null)
                    return null;

                for (int i = 0; i < artifact.flexFields.names.Length; i++)
                {
                    if (artifact.flexFields.names[i] == fieldName)
                        return (artifact.flexFields.values[i] ?? string.Empty).ToString();
                }

                return null;
            }

            return (prop.GetValue(artifact, null) ?? string.Empty).ToString();
        }

        /// <summary>
        /// Returns the name of a TeamForge release.
        /// </summary>
        /// <param name="sessionId">Current session ID.</param>
        /// <param name="releaseId">TeamForge release ID.</param>
        /// <returns>Name of the release if found; otherwise null.</returns>
        private string GetReleaseName(string sessionId, string releaseId)
        {
            if (string.IsNullOrEmpty(releaseId))
                return null;

            try
            {
                var release = this.FrsApp.getReleaseData(sessionId, releaseId);
                if (release == null)
                    return null;

                return release.title;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a collection of the available statuses for a tracker.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        /// <param name="trackerId">The tracker ID.</param>
        /// <returns>Collection of the available statuses.</returns>
        private IEnumerable<IssueStatus> GetAvailableStatuses(string sessionId, string trackerId)
        {
            var fields = this.TrackerApp.getFields(sessionId, trackerId);
            foreach (var field in fields)
            {
                if (field.name == "Status")
                {
                    foreach (var value in field.fieldValues)
                        yield return new IssueStatus(value.value, value.valueClass);

                    yield break;
                }
            }
        }

        /// <summary>
        /// Gets the closed default status.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        /// <param name="trackerId">The tracker ID.</param>
        /// <returns>The default closed status if one is defined for the tracker; otherwise null.</returns>
        private IssueStatus GetClosedStatus(string sessionId, string trackerId)
        {
            var statuses = new List<IssueStatus>(GetAvailableStatuses(sessionId, trackerId));
            var defaultClosed = statuses.Find(i => i.Class == "Closed" && i.Name == "Closed");
            if (defaultClosed != null)
                return defaultClosed;

            return statuses.Find(i => i.Class == "Closed");
        }
    }
}
