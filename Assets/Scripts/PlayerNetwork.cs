using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    
    private NetworkVariable<int> _randomNumber = new(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        _randomNumber.OnValueChanged += (int prevValue, int newValue) =>
        {
            Debug.Log(OwnerClientId + "; randomNumber: " + _randomNumber.Value);
        };

    }

    // Update is called once per frame
    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.T))
        {
            _randomNumber.Value = Random.Range(0, 100);
        }

        Vector3 moveDir = new Vector3(0, 0, 0);

        if (Input.GetKey(KeyCode.W)) moveDir.z = +1f;
        if (Input.GetKey(KeyCode.S)) moveDir.z = -1f;
        if (Input.GetKey(KeyCode.A)) moveDir.x = -1f;
        if (Input.GetKey(KeyCode.D)) moveDir.x = +1f;

        const float moveSpeed = 3f;
        transform.position += moveDir * (moveSpeed * Time.deltaTime);
    }
}