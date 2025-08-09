using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5;         // �ƶ��ٶ�
    public float sensitivity = 2f;   // ���������

    private Vector3 moveDirection;   // �ƶ���������

    [SerializeField]
    public Camera cameraCamera;

    void Update()
    {
        // ---------------- �ƶ� ----------------
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D -> -1 / 1
        float vertical = Input.GetAxisRaw("Vertical");     // W/S -> 1 / -1

        Vector3 moveForward = transform.forward * vertical; // ���������������ǰ�� + ����
        Vector3 moveRight = transform.right * horizontal;

        moveDirection = (moveForward + moveRight).normalized; // �����ϳ��ƶ���������һ������ֹ�Խ��߸��죩

        transform.position += moveDirection * speed * Time.deltaTime; // Ӧ���ƶ�

        // ---------------- ��ת��ˮƽ�� ----------------
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        transform.Rotate(Vector3.up * mouseX);

        // �����һ��
        if (Input.GetKeyDown(KeyCode.C))
        {
            /*Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(cameraCamera);
            foreach(var plane in frustumPlanes)
            {
                GameObject planeCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                planeCube.transform.localScale = new Vector3(10f, 10f, 0.01f);
                AlignCubeWithPlane(planeCube.transform, plane);
            }*/
            GenerateFrustumMesh(cameraCamera);
        }
    }


    Vector3[] GetFrustumCorners(Camera camera, Matrix4x4 transformMatrix)
    {
        float near = camera.nearClipPlane;
        float far = camera.farClipPlane;
        Vector3[] nearCorners = new Vector3[4];
        Vector3[] farCorners = new Vector3[4];
        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), near, Camera.MonoOrStereoscopicEye.Mono, nearCorners);
        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), far, Camera.MonoOrStereoscopicEye.Mono, farCorners);

        Vector3[] resultCorners = nearCorners.Concat(farCorners).ToArray();
        for (int i = 0; i < resultCorners.Length; i++)
        {
            resultCorners[i] = transformMatrix.MultiplyPoint(resultCorners[i]);
        }
        return resultCorners;
    }

    GameObject GenerateFrustumMesh(Camera camera)
    {
        Vector3[] frustumCorners = GetFrustumCorners(camera, camera.transform.localToWorldMatrix);
        Mesh mesh = new Mesh();
        mesh.vertices = frustumCorners;
        mesh.triangles = new int[]
        {
            0, 1, 2,  0, 2, 3,
            0, 3, 7,  0, 7, 4,
            1, 5, 6,  1, 6, 2,
            3, 2, 6,  3, 6, 7,
            0, 4, 5,  0, 5, 1,
            4, 6, 5,  4, 7, 6,
        };
        GameObject frustumObject = new GameObject("FrustumMesh");
        MeshFilter meshFilter = frustumObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = frustumObject.AddComponent<MeshRenderer>();
        meshFilter.mesh = mesh;
        meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        meshRenderer.material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        return frustumObject;
    }

    void AlignCubeWithPlane(Transform obj, Plane plane)
    {
        obj.position = -plane.normal * plane.distance;
        obj.rotation = Quaternion.FromToRotation(Vector3.forward, plane.normal);
    }
}
