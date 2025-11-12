using UnityEngine;

public class GameUtilities : MonoBehaviour
{
    [Header("Slice Lock")]
    public float sliceZ = 0f;

    private Rigidbody rb;  

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    void FixedUpdate()
    {
    var p = rb.position;
    if (Mathf.Abs(p.z - sliceZ) > 0.0001f)
        rb.position = new Vector3(p.x, p.y, sliceZ);
    }
}
