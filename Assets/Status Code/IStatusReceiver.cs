// Anything that can build up status effects. Sources (hazards, enemy attacks,
// player items) talk to this, so they work the same on the player and enemies:
//
//     IStatusReceiver target = other.GetComponentInParent<IStatusReceiver>();
//     target?.AddBuildup(StatusType.Poison, 5f);
public interface IStatusReceiver
{
    void AddBuildup(StatusType type, float amount);
}
