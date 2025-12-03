using UnityEngine;
using TMPro;
using System.Collections;   // <-- tambahkan ini

public class Sc_pin : MonoBehaviour
{
    private string pin = "", pinShowed = "";
    [SerializeField] TextMeshProUGUI pinText;
    private const int MAX_DIGITS = 9;

    // tambahan untuk error state
    private bool isShowingError = false;
    private Coroutine errorCoroutine;
    [SerializeField] private GameObject pintu;
    [SerializeField] Sc_showUIPin script;

    void Start()
    {
        pintu.GetComponent<Sc_pintu>().enabled = false;
    }

    void Update()
    {
        // kalau lagi nunjukin "PIN SALAH", jangan update teks dari PIN
        if (isShowingError) return;

        pinShowed = pin;
        KeepLast9Digits();
        pinText.text = pinShowed;
    }

    public void InputPin(string key)
    {
        pin += key;
        Debug.Log(pin);
    }

    public void DeleteLastDigit()
    {
        if (!string.IsNullOrEmpty(pin))
        {
            // buang karakter paling belakang
            pin = pin.Substring(0, pin.Length - 1);
            Debug.Log("PIN setelah delete = " + pin);
        }
    }

    private void KeepLast9Digits()
    {
        if (!string.IsNullOrEmpty(pin) && pin.Length > MAX_DIGITS)
        {
            // ambil 9 karakter terakhir
            pinShowed = pin.Substring(pin.Length - MAX_DIGITS, MAX_DIGITS);
        }
    }

    public void KlikEnter()
    {
        if (pin == "1308")
        {
            Debug.Log("PIN benar");
            pintu.GetComponent<Sc_pintu>().enabled = true;

            pin = ""; // reset PIN internal
            if (errorCoroutine != null)
                StopCoroutine(errorCoroutine);

            errorCoroutine = StartCoroutine(ShowError(true));
        }
        else
        {
            Debug.Log("PIN salah");
            pin = ""; // reset PIN internal

            // mulai coroutine buat nunjukin pesan error 2 detik + kedip
            if (errorCoroutine != null)
                StopCoroutine(errorCoroutine);

            errorCoroutine = StartCoroutine(ShowError(false));

        }
    }

    // Coroutine untuk menampilkan "PIN SALAH" 2 detik dan berkedip
    private IEnumerator ShowError(bool solved)
    {
        isShowingError = true;

        float duration = 2f;        // total durasi error
        float elapsed = 0f;
        float blinkInterval = 0.2f; // seberapa cepat kedip

        while (elapsed < duration)
        {
            pinText.text = solved ? "PIN BENAR" : "PIN SALAH";
            yield return new WaitForSeconds(blinkInterval);

            pinText.text = "";
            yield return new WaitForSeconds(blinkInterval);

            elapsed += blinkInterval * 2f;
        }

        // setelah 2 detik, reset jadi kosong (null display)
        pinShowed = "";
        pinText.text = "";
        if (solved) script.ClosePinUI(solved);

        isShowingError = false;
        errorCoroutine = null;
    }
}