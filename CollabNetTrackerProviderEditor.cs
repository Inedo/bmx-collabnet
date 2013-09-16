using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.CollabNet
{
    /// <summary>
    /// Custom editor for the CollabNet tracker provider.
    /// </summary>
    internal sealed class CollabNetTrackerProviderEditor : ProviderEditorBase
    {
        private ValidatingTextBox txtUserName;
        private ValidatingTextBox txtBaseUrl;
        private PasswordTextBox txtPassword;
        private TextBox txtReleaseField;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollabNetTrackerProviderEditor"/> class.
        /// </summary>
        public CollabNetTrackerProviderEditor()
        {
        }

        public override void BindToForm(ProviderBase extension)
        {
            EnsureChildControls();

            var provider = (CollabNetTrackerProvider)extension;
            this.txtUserName.Text = provider.UserName;
            this.txtPassword.Text = provider.Password;
            this.txtBaseUrl.Text = provider.BaseUrl;
            this.txtReleaseField.Text = provider.ReleaseField ?? string.Empty;
        }

        public override ProviderBase CreateFromForm()
        {
            EnsureChildControls();

            return new CollabNetTrackerProvider()
            {
                UserName = this.txtUserName.Text,
                Password = this.txtPassword.Text,
                BaseUrl = this.txtBaseUrl.Text,
                ReleaseField = this.txtReleaseField.Text
            };
        }

        protected override void CreateChildControls()
        {
            txtUserName = new ValidatingTextBox();
            txtBaseUrl = new ValidatingTextBox()
            {
                Width = 300
            };
            
            txtPassword = new PasswordTextBox();
            txtReleaseField = new TextBox()
            {
                Text = "resolvedReleaseId"
            };

            CUtil.Add(this,
                new FormFieldGroup("TeamForge Server URL",
                    "The URL of the CollabNet TeamForge server, for example: http://collabnet:8080",
                    false,
                    new StandardFormField(
                        "Server URL:",
                        txtBaseUrl)
                    ),
                new FormFieldGroup("Authentication",
                    "Provide a username and password to connect to the CollabNet TeamForge service.",
                    false,
                    new StandardFormField(
                        "User Name:",
                        txtUserName),
                    new StandardFormField(
                        "Password:",
                        txtPassword)
                    ),
                new FormFieldGroup("Configuration",
                    "Additional configuration options.",
                    false,
                    new StandardFormField(
                        "Artifact Release Field:",
                        txtReleaseField)
                    )
            );

            base.CreateChildControls();
        }
    }
}
