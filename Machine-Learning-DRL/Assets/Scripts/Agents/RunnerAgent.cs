using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Events;

public class RunnerAgent : Agent
{
    [SerializeField] private GameObject target;
    
    [SerializeField] private float walkSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float rotationSpeed;
    
    [SerializeField] private Timer countDown;

    private Rigidbody _rBody;
    private Rigidbody _targetRbody;
    private bool _isGrounded;

    [SerializeField] private UnityEvent onNewEpisode;
    
    public override void Initialize()
    {
        _rBody = GetComponent<Rigidbody>();

        _targetRbody = target.GetComponent<Rigidbody>();
    }
    
    public override void OnEpisodeBegin()
    {
        _rBody.velocity = Vector3.zero;
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        //Agent y axis rotation(1)
        sensor.AddObservation(transform.rotation.y);
        
        //Vector van target naar ball (direction naar target)(3)
        var toTarget = new Vector3((target.transform.position.x - transform.position.x) * rotationSpeed,
            (target.transform.position.y - transform.position.y),(target.transform.position.z - transform.position.z)*rotationSpeed);
        
        sensor.AddObservation(toTarget.magnitude);
        
        //Aftsand van de target(1)
        sensor.AddObservation(toTarget.normalized);
            
        //Agent velocity(3)
        sensor.AddObservation(_rBody.velocity);
        
        // target velocity (3 floats)
        sensor.AddObservation(_targetRbody.velocity.y);
        sensor.AddObservation(_targetRbody.velocity.z * rotationSpeed);
        sensor.AddObservation(_targetRbody.velocity.x * rotationSpeed);
    }
    
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers.DiscreteActions);
        SetReward(0.001f);
        
        AgentFellOff();
    }
    
    private void MoveAgent(ActionSegment<int> act)
    {
        //Dit is inprencipe het zelfde als Input.GetAxis zodat de Machine kan leren bewegen.
        CheckIfGrounded();
        
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var movementAction = act[0];
        var rotationAction  = act[1];
        var jumpAction = act[2];
        
        dirToGo = movementAction switch
        {
            1 => (_isGrounded ? 1f : 0.5f) * transform.forward * 1f,
            2 => (_isGrounded ? 1f : 0.5f) * transform.forward * -1f,
            3 => (_isGrounded ? 1f : 0.5f) * transform.right  * -1f,
            4 => (_isGrounded ? 1f : 0.5f) * transform.right ,
            _ => dirToGo
        };

        rotateDir = rotationAction switch
        {
            1 => transform.up * -1f,
            2 => transform.up * 1f,
            _ => rotateDir
        };

        transform.Rotate(rotateDir, rotationSpeed);
        
        var horizontalVelocity = dirToGo.normalized * walkSpeed;
        _rBody.velocity = new Vector3(horizontalVelocity.x, _rBody.velocity.y, horizontalVelocity.z);
        
        if (jumpAction == 1 && _isGrounded)
        {
            Jump();
        }
        
        if (!_isGrounded)
        {
            _rBody.AddForce(Vector3.down * 150f, ForceMode.Acceleration);
        }
    }
    
    private void Jump()
    {
        _rBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    
    public void TimerReachedZeroReward()
    {
        SetReward(1f);
        EndEpisode();
    }
    
    private void CheckIfGrounded()
    {
        RaycastHit hit;
        const float distance = 1.1f;
        var dir = Vector3.down;

        if (Physics.Raycast(transform.position, dir, out hit, distance))
        {
            if (hit.collider.CompareTag("walkableSurface"))
            {
                _isGrounded = true;
                return;
            }
        }
        _isGrounded = false;
    }
    
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag($"Seeker"))
        {
            SetReward(-1f);
            onNewEpisode.Invoke();
            EndEpisode();
        }
        
        if (other.gameObject.CompareTag($"Wall"))
            SetReward(-0.01f);
    }

    private void AgentFellOff()
    {
        if (!(transform.localPosition.y < 0)) return;
        SetReward(-1f);
        onNewEpisode.Invoke();
        EndEpisode();
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.D))
        {
            // rotate right
            discreteActionsOut[1] = 2;
        }
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            // move forward
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            // rotate left
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            // move backward
            discreteActionsOut[0] = 2;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            // move left
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            // move right
            discreteActionsOut[0] = 2;
        }
        discreteActionsOut[2] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }
}
