using UnityEngine;
using TMPro;
public class Sc_pin : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private string pin, pinShowed;
    [SerializeField] TextMeshProUGUI pinText;
    private const int MAX_DIGITS = 9;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
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
        if (pin.Length > MAX_DIGITS)
        {
            // ambil 9 karakter terakhir
            pinShowed = pin.Substring(pin.Length - MAX_DIGITS, MAX_DIGITS);
        }
    }
}
