using Spears.Content.Common;
using Terraria;
using Terraria.ModLoader;

namespace Spears.Content.Players;

public sealed class SpearPlayer : ModPlayer
{
	private int _monarchVolleyCount;

	internal bool NextMonarchUseIsFinal()
	{
		if (_monarchVolleyCount < 5) {
			_monarchVolleyCount++;
			return false;
		}

		_monarchVolleyCount = 0;
		return true;
	}

	public override void PostUpdate()
	{
		bool holdingMonarch = !Player.dead
			&& Player.HeldItem?.ModItem is ProgressionSpearItem spearItem
			&& spearItem.SpearKind == SpearKind.Monarch;

		if (!holdingMonarch)
			_monarchVolleyCount = 0;
	}

	public override void OnRespawn() => _monarchVolleyCount = 0;
}
