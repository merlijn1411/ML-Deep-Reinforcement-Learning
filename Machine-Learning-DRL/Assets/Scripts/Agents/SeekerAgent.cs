using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Events;
public class SeekerAgent : Agent
{
    [SerializeField] private GameObject target;
    [SerializeField] private Transform obstacleFence;
    
    [SerializeField] private float walkSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float rotationSpeed;
    
    [SerializeField] private Timer countDown;
    
    private Rigidbody _rBody;
    private Rigidbody _targetRbody;
    private bool _isGrounded;
    
    private float _previousDistance;
    private float _distanceToTarget;
    
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


    //Deze method houd bij welke gegevens hij moet onthouden dat word gebruikt om een vedere beslissing te maken (dus action). 
    public override void CollectObservations(VectorSensor sensor)
    {
        //Agent y axis rotation(1)
        sensor.AddObservation(transform.localRotation.y);
        
        //Vector van target naar ball (direction naar target)(3)
        var toTarget = new Vector3((target.transform.localPosition.x - transform.localPosition.x) * rotationSpeed,
            (target.transform.localPosition.y - transform.localPosition.y),(target.transform.localPosition.z - transform.localPosition.z)*rotationSpeed);
        
        sensor.AddObservation(toTarget.magnitude);
        
        //Aftsand van de target(1)
        sensor.AddObservation(toTarget.normalized);
            
        //Agent velocity(3)
        sensor.AddObservation(_rBody.velocity);
        
        //Wall localPosition(3)
        sensor.AddObservation(obstacleFence.localPosition);
        
        // target velocity (3 floats)
        sensor.AddObservation(_targetRbody.velocity.y);
        sensor.AddObservation(_targetRbody.velocity.z * rotationSpeed);
        sensor.AddObservation(_targetRbody.velocity.x * rotationSpeed);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers.DiscreteActions);
        AddReward(-0.001f);
        
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
        
        var obstacleDetected = CheckForObstacle();

        if (jumpAction == 1 && _isGrounded)
        {
            if (obstacleDetected)
            {
                Jump();
                AddReward(0.1f);
            }
        }

        if (jumpAction == 1 && !obstacleDetected) 
        {
            AddReward(-0.01f); // Straf voor onnodig springen
        }
        
        if (!_isGrounded)
        {
            _rBody.AddForce(Vector3.down * 150f, ForceMode.Acceleration);
        }
        
        DistanceToTarget();
    }
    
    private void Jump()
    {
        _rBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    
    private void DistanceToTarget()
    {
        _distanceToTarget = Vector3.Distance(transform.localPosition, target.transform.localPosition);
        // Reward for reducing distance to the cube
        if (_previousDistance > _distanceToTarget)
        {
            AddReward(0.02f); // Give a small positive reward for getting closer
        }
        _previousDistance = _distanceToTarget;
    }
    

    public void TimerReachedZeroReward()
    {
        var reward = (_distanceToTarget / 20f) * -1f; //de reward word op de hand van hoe dichtbij hij bij de target komt berekend.
        SetReward(reward);
        EndEpisode();
        
    }
    
    private bool CheckForObstacle() {
        RaycastHit hit;
        // Stel de afstand en layer in voor de raycast (pas aan naar je situatie)
        var raycastDistance = 10f;
        LayerMask obstacleLayer = LayerMask.GetMask("Obstacles");
        if (Physics.Raycast(transform.position, transform.forward, out hit, raycastDistance, obstacleLayer)) {
            return true; // Obstakel gedetecteerd
        }
        return false; // Geen obstakel gedetecteerd
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
        if (other.gameObject.CompareTag($"Runner"))
        {
            SetReward(1f);
            onNewEpisode.Invoke();
            EndEpisode();
        }        
        if (other.gameObject.CompareTag($"Wall"))
        {
            AddReward(-0.01f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag($"RewardPoint"))
        {
            SetReward(0.3f);
            Destroy(other.gameObject);
        }
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