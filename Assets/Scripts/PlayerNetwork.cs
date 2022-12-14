using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private Transform spawnedObjectPrefab;
    private Transform _spawnedObjectTransform;

    /// A custom value type struct that can be serialized for sending data over the network. 
    private struct CustomDataType : INetworkSerializable
    {
        public int ID;
        public bool IsServerOwner;
        public FixedString128Bytes UserMessage;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ID);
            serializer.SerializeValue(ref IsServerOwner);
            serializer.SerializeValue(ref UserMessage);
        }
    }

    /// A custom network variable with set read and write permissions.
    /// The generic type must be a value type (struct, bool, int, enum...).
    private readonly NetworkVariable<CustomDataType> _customNetworkVariable = new(
        new CustomDataType { ID = -1, IsServerOwner = true, UserMessage = "" },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private void Update()
    {
        // Only the owner can modify the variable
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.C)) SendToClient();
        if (Input.GetKeyDown(KeyCode.X)) SendToServer(new CustomDataType());

        if (Input.GetKeyDown(KeyCode.N)) SpawnGameObjServerRpc();
        if (Input.GetKeyDown(KeyCode.K)) DestroyGameObjServerRpc();
        if (Input.GetKeyDown(KeyCode.Y)) DespawnGameObjServerRpc(true);


        var moveDir = new Vector3(0, 0, 0);

        if (Input.GetKey(KeyCode.W)) moveDir.z = +1f;
        if (Input.GetKey(KeyCode.S)) moveDir.z = -1f;
        if (Input.GetKey(KeyCode.A)) moveDir.x = -1f;
        if (Input.GetKey(KeyCode.D)) moveDir.x = +1f;

        const float moveSpeed = 3f;
        transform.position += moveDir * (moveSpeed * Time.deltaTime);
    }

    private void SendToServer(CustomDataType customDataType)
    {
        var serverRpcParams = new ServerRpcParams();
        TestServerRpc(customDataType, serverRpcParams);
    }

    private void SendToClient()
    {
        // Send to a specific client, in this case, send to client with ID 1
        var targetClientIds = new List<ulong> { 1 };
        var clientRpcSendParams = new ClientRpcSendParams { TargetClientIds = targetClientIds };
        var clientRpcParams = new ClientRpcParams { Send = clientRpcSendParams };
        TestClientRpc(clientRpcParams);
    }

    /// With a server RPC, all of the code runs on the server, not the client.
    /// If you don't want your game to allow client ownership, use this.
    /// The client calls a function, and then the server runs than function.
    /// <param name="customDataType"></param>
    /// <param name="serverRpcParams"></param>
    [ServerRpc] private void TestServerRpc(CustomDataType customDataType, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log("TestServerRpc" + customDataType);
        Debug.Log("TestServerRpc " + OwnerClientId + "; " + serverRpcParams.Receive.SenderClientId);
    }

    /// Client RPCs are meant to be called from the server and then run on the clients.
    /// Server calls the function, then all of the clients run that function.
    /// The client cannot call the client RPC.
    /// <param name="clientRpcParams"></param>
    [ClientRpc] private void TestClientRpc(ClientRpcParams clientRpcParams)
    {
        Debug.Log(clientRpcParams);

        if (clientRpcParams.Send.TargetClientIds.Count > 0)
        {
            foreach (var sendTargetClientId in clientRpcParams.Send.TargetClientIds)
            {
                Debug.Log("ClientRpcParams: " + sendTargetClientId);
            }
        }

        Debug.Log("TestClientRpc " + OwnerClientId);
    }

    /// Spawn the GameObject from the server and send it over to the client
    [ServerRpc] private void SpawnGameObjServerRpc()
    {
        Debug.Log("IsHost: " + IsHost);
        Debug.Log("IsServer: " + IsServer);
        Debug.Log("Server: IsOwner: " + IsOwner);

        _spawnedObjectTransform = Instantiate(spawnedObjectPrefab);
        _spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
    }

    /// Despawn the GameObject from the server
    /// <param name="destroyObj"></param>
    [ServerRpc] private void DespawnGameObjServerRpc(bool destroyObj)
    {
        _spawnedObjectTransform.GetComponent<NetworkObject>().Despawn(destroyObj);
    }

    /// Destroy a GameObject from the server
    [ServerRpc] private void DestroyGameObjServerRpc()
    {
        Destroy(_spawnedObjectTransform.gameObject);
    }

    /// Update the data of the customNetworkVariable with updated values
    public override void OnNetworkSpawn()
    {
        _customNetworkVariable.OnValueChanged += (prevValue, newValue) =>
        {
            Debug.LogFormat(
                "OwnerClientId: {0}; prevValue: {1}; newValue: {2}; {3}; userMessage: {4}",
                OwnerClientId,
                prevValue.ID,
                newValue.ID,
                newValue.IsServerOwner,
                newValue.UserMessage
            );
        };
    }
}