using UnityEngine;

public class TestInteract : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        Debug.Log("Interaction Worked!!!");
    }

    public string InteractText()
    {
        return "Test Interact";
    }
}
