using UnityEngine;

public class Letter_sc : MonoBehaviour
{
    [Header("Panel UI khusus surat ini (drag dari Canvas)")]
    public GameObject LetterUI;

    public bool IsOpen => LetterUI && LetterUI.activeSelf;

    public void Open()
    {
        if (!LetterUI) { Debug.LogError($"[{name}] LetterUI belum di-assign."); return; }
        LetterUI.SetActive(true);
    }

    public void Close()
    {
        if (!LetterUI) { Debug.LogError($"[{name}] LetterUI belum di-assign."); return; }
        LetterUI.SetActive(false);
    }
}
