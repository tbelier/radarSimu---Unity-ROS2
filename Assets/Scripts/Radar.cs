using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using messages.msg;
//using rosPoint = geometry_msgs.msg.Point;
using sensor_msgs.msg;
using rosPoint = geometry_msgs.msg.Point32;
using rosPointCloud = sensor_msgs.msg.PointCloud;
namespace ROS2
{
public class Radar : MonoBehaviour
{
    [Header("Radar Field of View")]
    [Range(1f, 360f)] public float horizontalFOV = 360f;
    [Range(1f, 180f)] public float verticalFOV = 10f;

    [Header("Radar Resolution")]
    [Range(0.1f, 10f)] public float horizontalResolution = 1f;
    [Range(0.1f, 10f)] public float verticalResolution = 10f;

    [Header("Radar Physics")]
    public float maxDistance = 100f;
    public float I0 = 220f;

    [Header("Visualisation")]
    public ParticleSystem hitParticles;

    [Header("Timing")]
    public float refreshRate = 0.5f;
    private float lastScanTime = 0f;


    

    // Liste des markers actifs
    private List<GameObject> activeMarkers = new List<GameObject>();

    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private IPublisher<rosPointCloud> radar_pub;

    void Start()
    {
        ros2Unity = GetComponent<ROS2UnityComponent>();
    }

    void Update()
    {
        if (ros2Unity.Ok())
        {
            if (ros2Node == null)
            {
                ros2Node = ros2Unity.CreateNode("SimuRadar");
                radar_pub = ros2Node.CreatePublisher<rosPointCloud>("/simu/radar");
            }

            if (Time.time - lastScanTime >= refreshRate)
            {
                lastScanTime = Time.time;
                List<rosPoint> radar_points = Scan();

                rosPointCloud msg = new rosPointCloud();
                
                msg.Header = new std_msgs.msg.Header
                {
                    Frame_id = "map",
                };
                msg.Points = radar_points.ToArray();
                radar_pub.Publish(msg);
            }
        }
    }

    public List<rosPoint> Scan()
    {   
        List<rosPoint> radar_points = new List<rosPoint>();
        // 1. Supprimer tous les markers précédents
        foreach (GameObject marker in activeMarkers)
        {
            if (marker != null)
                Destroy(marker);
        }
        activeMarkers.Clear();

        // 2. Nouveau scan
        float hStart = -horizontalFOV / 2f;
        float vStart = -verticalFOV / 2f;

        for (float yaw = hStart; yaw <= hStart + horizontalFOV; yaw += horizontalResolution)
        {
            for (float pitch = vStart; pitch <= vStart + verticalFOV; pitch += verticalResolution)
            {
                Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
                Vector3 direction = rotation * transform.forward;

                if (Physics.Raycast(transform.position, direction, out RaycastHit hit, maxDistance))
                {
                    if (hit.collider.CompareTag("Intangible"))
                        continue;

                    float distance = hit.distance;
                    float intensity = I0 / (distance * distance);

                    Debug.DrawLine(transform.position, hit.point, Color.white, refreshRate);

                    if (hitParticles != null)
                    {
                        hitParticles.Play();
                        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
                        {
                            position = hit.point,
                            applyShapeToPosition = false
                        };
                        hitParticles.Emit(emitParams, 1);

                        radar_points.Add(new rosPoint { X = hit.point.z, Y = -hit.point.x, Z = hit.point.y });
                    }
                }
                else
                {
                    Debug.DrawRay(transform.position, direction * maxDistance, Color.gray, refreshRate);
                }
            }
        }
        return radar_points;
    }
}
}