using System;
using System.Collections.Generic;


namespace BattleServer.Models;

/// <summary>Distinct <c>damage_type</c> / <c>category</c> values for weapons admin UI.</summary>
public sealed class BattleWeaponMetaDto
{
    public IReadOnlyList<string> DamageTypes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Categories { get; set; } = Array.Empty<string>();
}
