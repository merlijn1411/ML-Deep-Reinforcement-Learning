using UnityEngine;

public class Timer : MonoBehaviour
{
    private TextMesh _timeMesh;
    //public static Timer Instance;
    [HideInInspector]public float t;

    private void Start()
    {
        //Instance = this;
        _timeMesh = GetComponent<TextMesh>();
    }

    private void Update()
    {
        t -= 1f * Time.deltaTime;
        var tCounter = Mathf.Ceil(t);
        _timeMesh.text = $"{tCounter}";
    }
}
