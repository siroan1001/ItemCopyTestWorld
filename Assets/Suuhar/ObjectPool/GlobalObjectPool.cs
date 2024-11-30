
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;
using UnityEditor;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
[CustomEditor(typeof(GlobalObjectPool))]

public class GlobalObjectPoolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("セットアップ"))
        {
            GlobalObjectPool admin = target as GlobalObjectPool;
            if (admin)
            {
                admin.SetUpObjectPool();
            }
        }
        base.OnInspectorGUI();
    }
}
#endif
public class GlobalObjectPool : UdonSharpBehaviour
{
    [Header("スポーンさせるオブジェクト")]
    [SerializeField] GameObject Object;
    [Header("同時スポーンの上限数")]
    [SerializeField] int ObjectCount;

    [SerializeField,HideInInspector]VRCObjectPool objectPool;
    [Header("スポーン位置")]
    [SerializeField] Transform SpawnPoint;
    public override void Interact()
    {
        base.Interact();

        SpawnObject();
    }
    public void SpawnObject()
    {
        if (!Networking.IsOwner(objectPool.gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, objectPool.gameObject);
        }

        var SpawnedObject = objectPool.TryToSpawn();

        if (!SpawnedObject)
        {
            //スポーン失敗
            return;
        }

        if (!Networking.IsOwner(SpawnedObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, SpawnedObject);
        }

        if (SpawnPoint)
        {
            SpawnedObject.transform.SetPositionAndRotation(SpawnPoint.position, SpawnedObject.transform.rotation);
        }
        else
        {
            PooledObject poolObject= SpawnedObject.GetComponent<PooledObject>();

            SpawnedObject.transform.SetPositionAndRotation(poolObject.initialPosition, poolObject.initialRotation);
        }
    }
    public void ReturnObject(GameObject obj)
    {
        if (!Networking.IsOwner(objectPool.gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, objectPool.gameObject);
        }

        objectPool.Return(obj);
    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    public void SetUpObjectPool()
    {
        if(objectPool == null)
        {
            objectPool = this.GetComponentInChildren<VRCObjectPool>();
            if (objectPool == null) 
            { 
                Debug.Log("ObjectPoolが見つかりません" +
                "新規作成します");
                GameObject NewGameObject = new GameObject("ObjectPool");
                NewGameObject.transform.parent = this.transform.parent;

                objectPool = NewGameObject.AddComponent<VRCObjectPool>();
            }
        }

        if(!SpawnPoint)
        {
            Debug.Log("SpawnPointが見つかりません。作成します");
            SpawnPoint = new GameObject("SpawnPoint").transform;
            SpawnPoint.parent = this.transform;
            SpawnPoint.SetPositionAndRotation(this.transform.position + this.transform.forward * 0.2f,
                this.transform.rotation);
        }

        //既存削除
        if (objectPool)
        {
            foreach (var item in objectPool.Pool)
            {
                DestroyImmediate(item);
            }
        }

        if(Object == null)
        {
            Debug.Log("オブジェクトが指定されていません。" +
                "生成を中止します");
            objectPool.Pool = new GameObject[0];
            return;
        }

        objectPool.Pool = new GameObject[ObjectCount];

        for (int i = 0; i < ObjectCount; i++)
        {
            objectPool.Pool[i] = (GameObject)PrefabUtility.InstantiatePrefab(Object,objectPool.transform);
            if (objectPool.Pool[i] == null)
            {
                //プレハブ以外
                objectPool.Pool[i] = Instantiate(Object,objectPool.transform);
            }

            objectPool.Pool[i].SetActive(true);


            Rigidbody rigidbody = objectPool.Pool[i].GetComponent<Rigidbody>();
            if (!rigidbody)
            {
                rigidbody = objectPool.Pool[i].AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
            }

            VRCPickup pickup = objectPool.Pool[i].GetComponent<VRCPickup>();
            if(pickup == null)
            {
                pickup = objectPool.Pool[i].AddComponent<VRCPickup>();
                pickup.AutoHold = VRC_Pickup.AutoHoldMode.Yes;
            }

            VRCObjectSync sync = objectPool.Pool[i].GetComponent<VRCObjectSync>();
            if(sync == null)
            {
                sync = objectPool.Pool[i].AddComponent<VRCObjectSync>();
            }

            Collider collider = objectPool.Pool[i].GetComponent<Collider>();
            if(collider == null)
            {
                collider = objectPool.Pool[i].AddComponent<BoxCollider>();
            }


            PooledObject pooledObject = objectPool.Pool[i].GetComponent<PooledObject>();
            if(pooledObject == null)
            {
                pooledObject = objectPool.Pool[i].AddComponent<PooledObject>();
            }
            pooledObject.Pool = this;
        }
    }
#endif
}
