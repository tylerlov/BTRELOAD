// If you're using a namespace, uncomment and adjust the following line:
// namespace YourProjectNamespace.Enemies
// {

public interface ILimbDamageReceiver
{
    void DamageFromLimb(string limbName, float amount);
    bool IsAlive();
}

// If you're using a namespace, uncomment the following line:
// }
