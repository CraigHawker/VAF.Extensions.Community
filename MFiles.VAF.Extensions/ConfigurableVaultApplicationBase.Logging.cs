using MFiles.VAF.Configuration;
using MFiles.VAF.Configuration.Logging;
using MFiles.VAF.Configuration.Logging.NLog;
using MFiles.VAF.Extensions.Configuration;
using MFiles.VAF.Extensions.Logging;
using MFiles.VAF.Extensions.Webhooks.Configuration;
using MFilesAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MFiles.VAF.Extensions
{
	public partial class ConfigurableVaultApplicationBase<TSecureConfiguration>
	{
		/// <summary>
		/// The logger for the vault application class.
		/// </summary>
		public ILogger Logger { get; private set; }

		/// <summary>
		/// The default logging namespace; everything from here will
		/// be put into the "application" log category.
		/// </summary>
		/// <returns></returns>
		protected virtual string GetDefaultLoggingNamespace()
			=> $"{this.GetType().Namespace}.*";

		/// <inheritdoc />
		protected override void InitializeApplication(Vault vault)
		{
			base.InitializeApplication(vault);

			// If we have logging configuration then initialize with that.
			var loggingConfiguration = this.GetLoggingConfiguration();
			LogManager.Initialize(vault, loggingConfiguration);
			this.Logger?.Debug($"Logging started");
		}

		/// <summary>
		/// Retrieves the current logging configuration, if available.
		/// </summary>
		/// <returns>The current logging configuration, or null.</returns>
		protected virtual ILoggingConfiguration GetLoggingConfiguration()
		{
			if (this.Configuration is Configuration.IConfigurationWithLoggingConfiguration configurationWithLogging)
			{
				return configurationWithLogging?.GetLoggingConfiguration();
			}

			return null;
		}



		/// <summary>
		/// Retriees the current logging configuration, if available.
		/// </summary>
		/// <returns>The current logging configuration, or null.</returns>
		protected virtual IndividualWebhookConfigurationEditor GetIndividualWebhookConfiguration(TSecureConfiguration config = null)
		{
			var c = config ?? this.Configuration;
			if (c is IConfigurationWithWebhookConfiguration configurationWithWebhook)
			{
				return configurationWithWebhook?.IndividualWebhookConfiguration;
			}

			return null;
		}
		
		/// <inheritdoc />
		protected override void UninitializeApplication(Vault vault)
		{
			// If we have a logger then write out that we're stopping.
			this.Logger?.Debug($"Logging stopping");
			LogManager.Shutdown();
			base.UninitializeApplication(vault);
		}

		/// <inheritdoc />
		protected override IEnumerable<ValidationFinding> CustomValidation(Vault vault, TSecureConfiguration config)
		{
			foreach (var finding in base.CustomValidation(vault, config) ?? new ValidationFinding[0])
				yield return finding;

			// If we have logging configuration then use that.
			var loggingConfiguration = this.GetLoggingConfiguration();
			if (loggingConfiguration != null)
			{
				foreach (var finding in loggingConfiguration.GetValidationFindings() ?? new ValidationFinding[0])
					yield return finding;
			}

			// Validate the webhook stuff.
			if(config is IConfigurationWithWebhookConfiguration webhookConfig
				&& (this.Webhooks?.Any() ?? false))
			{
				switch(webhookConfig.WebhookConfigurationType)
				{
					// If we have common auth then check that.
					case WebhookConfigurationType.Common:
						{
							var commonConfiguration = webhookConfig.CommonWebhookConfiguration;
							if(null == commonConfiguration || null == commonConfiguration?.GetWebhookAuthenticator())
							{
								// No config, but no webhooks.
								yield return new ValidationFinding
								(
									ValidationFindingType.Warning,
									"WebhookConfiguration",
									"No webhook configuration was found, but webhooks are available.  Webhooks may not be able to be called."
								);
							}
						}
						break;

					// Individual config is harder to validate.
					case WebhookConfigurationType.Individual:
						{
							var individualWebhookConfiguration = this.GetIndividualWebhookConfiguration(config);
							if (null == individualWebhookConfiguration)
							{
								// No config, but no webhooks.
								yield return new ValidationFinding
								(
									ValidationFindingType.Warning,
									"WebhookConfiguration",
									"No webhook configuration was found, but webhooks are available.  Webhooks may not be able to be called."
								);
							}
							if (null != individualWebhookConfiguration)
							{
								foreach (var webhook in this.Webhooks)
								{
									// If none whatsoever then return a warning.
									if (!individualWebhookConfiguration.TryGetWebhookAuthenticator(webhook.WebhookName, out var authenticator))
									{
										yield return new ValidationFinding
										(
											ValidationFindingType.Warning,
											"WebhookConfiguration",
											$"No webhook configuration was found for webhook {webhook.WebhookName}.  This webhook may not be able to be called."
										);
										continue;
									}

									// Allow each authenticator type to validate.
									foreach (var finding in authenticator.CustomValidation(vault, webhook.WebhookName))
									{
										yield return finding;
									}
								}
							}
							break;
					}
					default:
						break;
				}
			}
			
		}
	}
}
