using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    #region Public
    public float xSens = 30f;
    public float ySens = 30f;
    #endregion

    #region Private
    private Camera _cam;
    private float xRotation = 0f;
    #endregion

    #region Unity Events
    void Start()
    {
        _cam = transform.GetChild(0).GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        CalculateLook(InputReader.Instance.LookValue);
    }
    #endregion

    #region Functions
    private void CalculateLook(Vector2 input)
    {
        float mouseX = input.x;
        float mouseY = input.y;

        // Y Movement
        xRotation -= (mouseY * Time.deltaTime) * ySens;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        _cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);

        // X Movement
        transform.Rotate(Vector3.up * (mouseX * Time.deltaTime) * xSens);
    }
    #endregion
}