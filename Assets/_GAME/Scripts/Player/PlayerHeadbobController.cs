using System;
using System.ComponentModel.Design;
using Unity.Netcode;
using UnityEngine;

public class PlayerHeadbobController : NetworkBehaviour
{
    #region Public
    [Header("Headbob Settings")]
    public bool Enabled = true;
    [Range(0, 0.1f)]
    public float walkAmp = 0.001f;
    [Range(0, 0.1f)]
    public float sprintAmp = 0.015f;
    [SerializeField, Range(0, 30f)]
    private float _frequency = 10.0f; // How oftern it bobs/step amount
    #endregion

    #region Private
    private float _amplitude; // Current Intensity

    [SerializeField]
    private Transform _camera = null;

    private Vector3 _startPos;
    #endregion

    #region Unity Events
    void Start()
    {
        _startPos = _camera.localPosition;
    }

    void Update()
    {
        if (!Enabled)
            return;

        _amplitude = InputReader.Instance.IsSprinting ? sprintAmp : walkAmp;

        if (InputReader.Instance.MovementValue.magnitude > 0)
            PlayMotion(FootStepMotion());

        ResetPosition();
    }
    #endregion

    #region Functions
    private void PlayMotion(Vector3 motion)
    {
        _camera.localPosition += motion;
    }

    private void ResetPosition()
    {
        if (_camera.localPosition == _startPos)
            return;
        _camera.localPosition = Vector3.Lerp(_camera.localPosition, _startPos, 1 * Time.deltaTime);
    }

    private Vector3 FootStepMotion()
    {
        Vector3 pos = Vector3.zero;
        pos.y += Mathf.Sin(Time.time * _frequency) * _amplitude; // Smooth Y motion
        pos.x += Mathf.Cos(Time.time * _frequency / 2) * _amplitude * 2; // X motion
        return pos;
    }
    #endregion

    // If client doesn't own this, disable me
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) enabled = false;
    }
}
