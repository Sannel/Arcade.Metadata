﻿namespace Sannel.Arcade.Metadata.Scan.v1.Services;

public interface IScanService
{
	Task ScanAsync(CancellationToken cancellationToken);
	Task ScanAsync(bool forceRebuild, CancellationToken cancellationToken);
}
