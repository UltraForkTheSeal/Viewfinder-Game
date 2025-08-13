using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EzySlice;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;         // �ƶ��ٶ�
    public float sensitivity = 2f;   // ���������

    [SerializeField] private Camera cameraCamera;
    [SerializeField] private Material cuttingMaterial;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private GameObject copiedObject;
    [SerializeField] private Material frustumDebugMaterial; // ���ڵ�����׵��

    private Vector3 moveDirection;
    private Bounds frustumBounds;
    private GameObject frustumDebugObject; // ��������׵�� Mesh

    void Update()
    {
        // ---------------- �ƶ� ----------------
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 moveForward = transform.forward * vertical;
        Vector3 moveRight = transform.right * horizontal;
        moveDirection = (moveForward + moveRight).normalized;
        transform.position += moveDirection * speed * Time.deltaTime;

        // ---------------- ��Cִ���и� ----------------
        if (Input.GetKeyDown(KeyCode.C))
        {
            copiedObject.transform.position = cameraCamera.transform.position;
            copiedObject.transform.rotation = cameraCamera.transform.rotation;

            Vector3[] frustumCorners = GetFrustumWorldCorners(cameraCamera);
            frustumBounds = GetWorldBounds(frustumCorners);

            // ��ʾ������׵��
            DrawFrustumDebugMesh(frustumCorners);

            LayerMask mask = ~((1 << 6) | (1 << 7));
            Collider[] hits = Physics.OverlapBox(frustumBounds.center, frustumBounds.extents, Quaternion.identity, mask);

            UnityEngine.Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(cameraCamera);

            foreach (var hit in hits)
            {
                if (!GeometryUtility.TestPlanesAABB(frustumPlanes, hit.bounds))
                {
                    continue; // ����������׵���ڵ�����
                }

                GameObject processingTarget = Instantiate(hit.gameObject);
                processingTarget.transform.position = hit.transform.position;
                processingTarget.transform.rotation = hit.transform.rotation;

                foreach (var plane in frustumPlanes)
                {
                    Vector3 planePointWorld = -plane.normal * plane.distance;
                    Vector3 planeNormalWorld = plane.normal;

                    Vector3 localPoint = processingTarget.transform.InverseTransformPoint(planePointWorld);
                    Vector3 localNormal = processingTarget.transform.InverseTransformDirection(planeNormalWorld);

                    SlicedHull slicedHull = processingTarget.Slice(localPoint, localNormal, cuttingMaterial);
                    if (slicedHull != null)
                    {
                        GameObject upperPart = slicedHull.CreateUpperHull(processingTarget, cuttingMaterial);
                        upperPart.name = "cut_" + processingTarget.name;
                        upperPart.transform.position = processingTarget.transform.position;
                        upperPart.transform.rotation = processingTarget.transform.rotation;

                        Destroy(processingTarget);
                        processingTarget = upperPart;
                    }
                }
                // ����ԭ����λ��
                Vector3 worldPos = processingTarget.transform.position;
                Quaternion worldRot = processingTarget.transform.rotation;

                processingTarget.transform.SetParent(copiedObject.transform);

                processingTarget.transform.position = worldPos;
                processingTarget.transform.rotation = worldRot;
            }

            cameraController.enabled = false;
            this.enabled = false;
        }

        // ---------------- ��ת��ˮƽ�� ----------------
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        transform.Rotate(Vector3.up * mouseX);
    }

    private void OnDrawGizmos()
    {
        if (frustumBounds.size != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(frustumBounds.center, frustumBounds.size);
        }
    }

    // ��ȡ�����׵��˸���������ǵ�
    Vector3[] GetFrustumWorldCorners(Camera cam)
    {
        Vector3[] nearCorners = new Vector3[4];
        Vector3[] farCorners = new Vector3[4];

        cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, nearCorners);
        cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, farCorners);

        for (int i = 0; i < 4; i++)
        {
            nearCorners[i] = cam.transform.TransformPoint(nearCorners[i]);
            farCorners[i] = cam.transform.TransformPoint(farCorners[i]);
        }

        return nearCorners.Concat(farCorners).ToArray();
    }

    // �ð˸�����������Χ��
    Bounds GetWorldBounds(Vector3[] points)
    {
        Bounds b = new Bounds(points[0], Vector3.zero);
        foreach (var p in points) b.Encapsulate(p);
        return b;
    }

    // ���Ƶ�������׵�� Mesh
    void DrawFrustumDebugMesh(Vector3[] corners)
    {
        if (frustumDebugObject != null) Destroy(frustumDebugObject);

        Mesh mesh = new Mesh();
        mesh.vertices = corners;

        // �����ζ���������near: 0-3, far: 4-7��
        mesh.triangles = new int[]
        {
            0,1,2, 0,2,3, // ��
            4,5,6, 4,6,7, // Զ
            0,4,5, 0,5,1, // ��
            1,5,6, 1,6,2, // ��
            2,6,7, 2,7,3, // ��
            3,7,4, 3,4,0  // ��
        };

        mesh.RecalculateNormals();

        frustumDebugObject = new GameObject("FrustumDebugMesh");
        MeshFilter mf = frustumDebugObject.AddComponent<MeshFilter>();
        MeshRenderer mr = frustumDebugObject.AddComponent<MeshRenderer>();

        mf.mesh = mesh;
        mr.material = frustumDebugMaterial != null ? frustumDebugMaterial : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mr.material.SetFloat("_Mode", 3); // ͸��
        mr.material.color = new Color(0, 1, 0, 0.01f); // ��͸����ɫ

        frustumDebugObject.layer = 7; // ���ڵ��Բ�
    }
}