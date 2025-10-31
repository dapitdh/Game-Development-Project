using UnityEngine;

public interface IStunnable
{
    bool IsStunned { get; }
    void Stun(float duration, Vector3 hitPoint, Vector3 hitNormal, GameObject instigator, float force);
}
