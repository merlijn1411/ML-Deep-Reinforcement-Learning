using UnityEngine;
using Random = UnityEngine.Random;

public class PositionChanger : MonoBehaviour
{
    [SerializeField] private float minX = -10.0f;
    [SerializeField] private float maxX = 10.0f;
    [SerializeField] private float minZ = -10.0f;
    [SerializeField] private float maxZ = 10.0f;
    [SerializeField] private float minY = 1.0f;

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Seeker"))
            MoveTarget();
    }
    private void MoveTarget()
    {
        var randomX = Random.Range(minX, maxX);
        var randomZ = Random.Range(minZ, maxZ);
        
        // Verplaats het doel naar de nieuwe positie
        transform.position = new Vector3(randomX, minY, randomZ);
    }
}
