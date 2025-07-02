using MediatR;

namespace Sannel.Arcade.Metadata.Common.Settings;

public class GetSettingRequest : IRequest<string?>
{
	public string Key { get; set; }
}
