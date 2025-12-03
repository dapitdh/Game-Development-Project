using UnityEngine;

public class ChestOpener : MonoBehaviour
{
    public Animator animator;
    public string openBoolName = "isOpen";
    public bool openOnce = true;
    private bool hasOpened = false;

    private void Awake()
    {
        if (!animator)
            animator = GetComponent<Animator>();
    }

    public void OpenChest()
    {
        if (openOnce && hasOpened) return;
        hasOpened = true;
        if (animator)
            animator.SetBool(openBoolName, true);
    }
}
