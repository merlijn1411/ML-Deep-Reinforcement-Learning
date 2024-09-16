using TMPro;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class SeekerAgent : Agent
{
    [SerializeField] private Transform target;
    [SerializeField] private float forceMultiplier = 10f;
    
    [SerializeField] private float t;
    [SerializeField] private TextMeshProUGUI timeCounter;
    
    private Rigidbody _rBody;
    
    private void Start()
    {
        _rBody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        t -= 1f * Time.deltaTime;
        var tCounter = Mathf.Ceil(t);
        timeCounter.text = $"{tCounter}";
    }

    public override void OnEpisodeBegin()
    {
        t = 30f;
        
        _rBody.angularVelocity = Vector3.zero;
        _rBody.velocity = Vector3.zero;
        transform.localPosition = new Vector3( -4, 1f, -1);
        
        target.localPosition = new Vector3(4,1f,-1);
    }

    //Deze method houd bij welke gegevens hij moet onthouden dat word gebruikt om een vedere beslissing te maken (dus action). 
    public override void CollectObservations(VectorSensor sensor)
    {
        //Target en agent position
        sensor.AddObservation(target.localPosition);
        sensor.AddObservation(this.transform.localPosition);
        
        //Agent velocity
        sensor.AddObservation(_rBody.velocity.x);
        sensor.AddObservation(_rBody.velocity.z);
    }
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var controlSignal = Vector3.zero;
        controlSignal.x = actionBuffers.ContinuousActions[0];
        controlSignal.z = actionBuffers.ContinuousActions[1];
        _rBody.AddForce(controlSignal * forceMultiplier);
        
        
        var distanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);
        
        if (t <= 0 )
        {
            var reward = (distanceToTarget / 10f) * -1f;
            print("Time ran out");
            Debug.Log(reward);
            SetReward(reward);
            EndEpisode();   
        }
        
        if (distanceToTarget < 1.42f)
        {
            print("+1");
            SetReward(1f);
            EndEpisode();
        }

        else if (transform.localPosition.y < 0)
        {
            print("-1");
            SetReward(-1f);
            EndEpisode();   
        }
    }   
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }
}
