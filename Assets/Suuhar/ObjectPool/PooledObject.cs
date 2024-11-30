
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PooledObject : UdonSharpBehaviour
{
    [HideInInspector] public GlobalObjectPool Pool;
    [HideInInspector] public Vector3 initialPosition;
    [HideInInspector] public Quaternion initialRotation;
    private void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

    }
    public override void OnPickupUseDown()
    {
        base.OnPickupUseDown();
        VRC_Pickup pickup = GetComponent<VRC_Pickup>();
        if (pickup) { pickup.Drop(); }

        //if (Pool) { Pool.ReturnObject(this.gameObject); }
    }
}
