using UnityEngine;

public class GunController : MonoBehaviour
{
    [Header("Gun Reference")]

    [SerializeField]
    private Transform gun;

    [Header("Rotation Settings")]

    [SerializeField]
    private float hiddenRotationX = 90f;

    [SerializeField]
    private float drawnRotationX = 0f;

    public bool IsGunDrawn { get; private set; }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ToggleGun();
        }
    }

    // ganti status gun antara keluar dan sembunyi
    private void ToggleGun()
    {
        IsGunDrawn = !IsGunDrawn;

        if (gun == null)
        {
            return;
        }

        float targetRotationX =
            IsGunDrawn ? drawnRotationX : hiddenRotationX;

        Vector3 currentEuler = gun.localEulerAngles;

        gun.localEulerAngles =
            new Vector3(
                targetRotationX,
                currentEuler.y,
                currentEuler.z
            );
    }
}
