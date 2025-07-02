using MediatR;

using Sannel.Arcade.Metadata.Common.Settings;
using Sannel.Arcade.Metadata.Settings.v1.Services;

namespace Sannel.Arcade.Metadata.Settings.v1.Handlers;

public class GetSettingHandler : IRequestHandler<GetSettingRequest, string?>
{
	private readonly IRuntimeSettingsService _secureSetting;
	private readonly IInsecureSettings _insecureSetting;

	public GetSettingHandler(IRuntimeSettingsService secureSetting, IInsecureSettings insecureSetting)
	{
		_secureSetting = secureSetting ?? throw new ArgumentNullException(nameof(secureSetting));
		_insecureSetting = insecureSetting ?? throw new ArgumentNullException(nameof(insecureSetting));
	}

	public async Task<string?> Handle(GetSettingRequest request, CancellationToken cancellationToken)
	{
		var setting = _insecureSetting.GetValue(request.Key) ??
			await _secureSetting.GetSettingAsync(request.Key, null, cancellationToken);
		return setting;
	}
}
