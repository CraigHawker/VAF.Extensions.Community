using MFiles.VAF.Extensions.Webhooks.Configuration;

namespace MFiles.VAF.Extensions.Configuration
{
	public interface IConfigurationWithWebhookConfiguration
	{
		WebhookConfigurationType WebhookConfigurationType { get; set; }
		WebhookConfiguration CommonWebhookConfiguration { get; set; }
		IndividualWebhookConfigurationEditor IndividualWebhookConfiguration { get; set; }
	}
}
