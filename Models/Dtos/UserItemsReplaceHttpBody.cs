using System.Collections.Generic;


namespace BattleServer.Models;

public sealed class UserItemsReplaceHttpBody
{
    public List<UserItemReplaceDto> Items { get; set; } = new();
}
