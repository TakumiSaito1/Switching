using System.Collections.Generic;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float fallY = -10f;
    [SerializeField] private string boxTag = "Box";

    private Rigidbody playerRigidbody;
    private readonly List<BoxRespawnState> boxes = new List<BoxRespawnState>();

    private sealed class BoxRespawnState
    {
        public GameObject Box;
        public Rigidbody Rigidbody;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    private void Awake()
    {
        if (player != null)
        {
            playerRigidbody = player.GetComponent<Rigidbody>();
        }
    }

    private void Start()
    {
        foreach (GameObject box in GameObject.FindGameObjectsWithTag(boxTag))
        {
            boxes.Add(new BoxRespawnState
            {
                Box = box,
                Rigidbody = box.GetComponent<Rigidbody>(),
                Position = box.transform.position,
                Rotation = box.transform.rotation
            });
        }
    }

    private void Update()
    {
        if (player != null && respawnPoint != null && player.position.y < fallY)
        {
            Respawn();
        }

        foreach (BoxRespawnState boxState in boxes)
        {
            if (boxState.Box != null && boxState.Box.transform.position.y < fallY)
            {
                RespawnBox(boxState);
            }
        }
    }

    private void Respawn()
    {
        player.position = respawnPoint.position;

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
    }

    public void RespawnPlayer()
    {
        if (player == null || respawnPoint == null)
        {
            return;
        }

        Respawn();
    }

    private void RespawnBox(BoxRespawnState boxState)
    {
        Transform boxTransform = boxState.Box.transform;
        boxTransform.SetParent(null, true);
        boxTransform.SetPositionAndRotation(boxState.Position, boxState.Rotation);

        if (boxState.Rigidbody != null)
        {
            boxState.Rigidbody.linearVelocity = Vector3.zero;
            boxState.Rigidbody.angularVelocity = Vector3.zero;
            boxState.Rigidbody.Sleep();
        }

        Physics.SyncTransforms();
        Debug.Log("A box returned to its starting position.", boxState.Box);
    }

}
