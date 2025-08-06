using MFiles.VAF.Configuration;
using MFiles.VAF.Configuration.Logging;
using MFiles.VAF.Configuration.Logging.NLog;
using MFiles.VAF.Extensions.Webhooks.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MFiles.VAF.Extensions.Configuration
{
	/// <summary>
	/// The type of webhook configuration needed.
	/// </summary>
	public enum WebhookConfigurationType
	{
		[JsonConfEditor(Label = "Common", HelpText = "All web hooks use the same configuration")]
		Common = 0,

		[JsonConfEditor(Label = "Individual", HelpText = "Web hooks are configured individually")]
		Individual = 1
	}

	/// <summary>
	/// A base class for configuration that implements <see cref="IConfigurationWithLoggingConfiguration"/>.
	/// </summary>
	[DataContract]
	[UsesConfigurationResources]
	[UsesLoggingResources]
	public abstract class ConfigurationBase
		: VersionedConfigurationBase,
			IConfigurationWithLoggingConfiguration,
			IConfigurationWithWebhookConfiguration
	{

		[DataMember]
		[Security(ChangeBy = SecurityAttribute.UserLevel.VaultAdmin, ViewBy = SecurityAttribute.UserLevel.VaultAdmin)]
		[JsonConfEditor(Label = "Webhook configuration type")]
		public WebhookConfigurationType WebhookConfigurationType { get; set; } = WebhookConfigurationType.Common;

		[DataMember]
		[Security(ChangeBy = SecurityAttribute.UserLevel.VaultAdmin, ViewBy = SecurityAttribute.UserLevel.VaultAdmin)]
		[JsonConfEditor
		(
			Label = "Webhook configuration",
			HelpText = "Configures webhooks (e.g. authentication)",
			ShowWhen = ".parent._children{.key == 'WebhookConfigurationType' && .value != 'Individual' }"
		)]
		public WebhookConfiguration CommonWebhookConfiguration { get; set; }
			= new WebhookConfiguration();

		[DataMember]
		[Security(ChangeBy = SecurityAttribute.UserLevel.VaultAdmin, ViewBy = SecurityAttribute.UserLevel.VaultAdmin)]
		[JsonConfEditor
		(
			Label = "Webhook configuration",
			HelpText = "Configures webhooks (e.g. authentication)",
			ShowWhen = ".parent._children{.key == 'WebhookConfigurationType' && .value == 'Individual' }"
		)]
		public IndividualWebhookConfigurationEditor IndividualWebhookConfiguration { get; set; }
			= new IndividualWebhookConfigurationEditor();

		[DataMember(EmitDefaultValue = false)]
		[JsonConfEditor
		(
			Label = ResourceMarker.Id + nameof(Resources.Configuration.LoggingConfiguration_Label),
			HelpText = ResourceMarker.Id + nameof(Resources.Configuration.LoggingConfiguration_HelpText)
		)]
		[Security(ChangeBy = SecurityAttribute.UserLevel.VaultAdmin, ViewBy = SecurityAttribute.UserLevel.VaultAdmin)]
		public NLogLoggingConfiguration Logging { get; set; }

		/// <inheritdoc />
		public ILoggingConfiguration GetLoggingConfiguration()
			=> this.Logging ?? new NLogLoggingConfiguration();
	}

	[DataContract]
	public class NLogLoggingConfiguration
		: MFiles.VAF.Configuration.Logging.NLog.Configuration.NLogLoggingConfiguration
	{
		/// <inheritdoc />
		public override IEnumerable<NLogLoggingExclusionRule> GetAllLoggingExclusionRules()
		{
			// Include any other exclusion rules.
			foreach (var r in base.GetAllLoggingExclusionRules() ?? Enumerable.Empty<NLogLoggingExclusionRule>())
				yield return r;

			// If we're set to exclude internal messages then also exclude the task manager ex (spammy).
			if (false == (this.Advanced?.RenderInternalLogMessages ?? false))
			{
				yield return new NLogLoggingExclusionRule()
				{
					LoggerName = "MFiles.VAF.Extensions.TaskManagerEx*",
					MinimumLogLevelOverride = LogLevel.Fatal
				};
			}
		}
	}
}
