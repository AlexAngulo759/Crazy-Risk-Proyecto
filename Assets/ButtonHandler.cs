using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BotonHandler : MonoBehaviour
{
    [Header("Atributos del país")]
    public int dueño; // 1 = servidor, 2 = cliente, 3 = neutro
    public int ID;
    public List<int> adyacentes = new List<int>();
    public int tropas;
    public string continente;

    [Header("Control de fases")]
    public bool primerToque = true;

    private Button boton;
    private TextMeshProUGUI textoBoton;

    void Awake()
    {
        boton = GetComponent<Button>();
        boton.onClick.AddListener(OnClick);

        // Suponemos que hay un TextMeshProUGUI hijo en el botón
        textoBoton = GetComponentInChildren<TextMeshProUGUI>();

        // Hacer el botón transparente (solo fondo)
        ColorBlock cb = boton.colors;
        cb.normalColor = new Color(1, 1, 1, 0); // transparente
        cb.highlightedColor = new Color(1, 1, 1, 0.1f);
        cb.pressedColor = new Color(1, 1, 1, 0.2f);
        boton.colors = cb;
    }

    public void Configurar(int _dueño, int _ID, string _continente, int _tropasIniciales, List<int> _adyacentes)
    {
        dueño = _dueño;
        ID = _ID;
        continente = _continente;
        tropas = _tropasIniciales;
        adyacentes = new List<int>(_adyacentes);
    }

    void Update()
    {
        if (textoBoton == null || GameManager.Instance == null) return;

        // Cambiar color del texto según dueño
        switch (dueño)
        {
            case 1: // Servidor
                textoBoton.color = GetColorFromInt(GameManager.Instance.color1);
                break;
            case 2: // Cliente
                textoBoton.color = GetColorFromInt(GameManager.Instance.color2);
                break;
            case 3: // Neutro
                textoBoton.color = Color.white;
                break;
        }

        // Mostrar solo el número de tropas en el botón
        textoBoton.text = tropas.ToString();
    }

    void OnClick()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.fase == 1)
        {
            // Refuerzos
            if ((dueño == 1 && GameManager.Instance.turno == 1) ||
                (dueño == 2 && GameManager.Instance.turno == 2))
            {
                Refuerzos();
            }
        }
        else if (GameManager.Instance.fase == 2)
        {
            GameManager.Instance.SeleccionarPais(this);
        }
        else if (GameManager.Instance.fase == 3)
        {
            GameManager.Instance.SeleccionarMovimiento(this);
        }
    }
    public void EnviarEstado()
    {
        if (MyNetworkManager.Instance == null) return;

        // Formato: COUNTRY:ID,dueño,tropas,continente,fase
        string mensaje = $"COUNTRY:{ID},{dueño},{tropas},{continente},{GameManager.Instance.fase}";
        MyNetworkManager.Instance.EnviarMensaje(mensaje);
        Debug.Log("📤 Estado enviado: " + mensaje);
    }

    void Refuerzos()
    {
        if (GameManager.Instance.turno == 1)
        {
            tropas++;
            GameManager.Instance.tropasReforzar1--;
            Debug.Log($"[Servidor] País {ID} reforzado. Tropas = {tropas}, refuerzos restantes = {GameManager.Instance.tropasReforzar1}");

            if (GameManager.Instance.tropasReforzar1 == 0)
            {
                GameManager.Instance.fase = 2;
                Debug.Log("Servidor terminó refuerzos → Fase = 2");
            }
            EnviarEstado();
        }
        else if (GameManager.Instance.turno == 2)
        {
            tropas++;
            GameManager.Instance.tropasReforzar2--;
            Debug.Log($"[Cliente] País {ID} reforzado. Tropas = {tropas}, refuerzos restantes = {GameManager.Instance.tropasReforzar2}");

            if (GameManager.Instance.tropasReforzar2 == 0)
            {
                GameManager.Instance.fase = 2;
                Debug.Log("Cliente terminó refuerzos → Fase = 2");
            }
            EnviarEstado();
        }
    }

    // Convierte los valores 1=Rojo, 2=Verde, 3=Azul a Color
    Color GetColorFromInt(int colorInt)
    {
        switch (colorInt)
        {
            case 1: return Color.red;
            case 2: return Color.green;
            case 3: return Color.blue;
            default: return Color.white;
        }
    }
}