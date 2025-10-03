using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

public class MyNetworkManager : MonoBehaviour
{
    public static MyNetworkManager Instance;

    private TcpListener server;
    private TcpClient client;
    private StreamReader reader;
    private StreamWriter writer;

    // Puerto de comunicación
    public int port = 5000;

    // Saber si esta máquina es servidor
    public bool esServidor = true;

    // IP del servidor (para clientes)
    public string serverIP = "172.20.10.14"; // 🚀 Tu IP local fija

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persistir entre escenas
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (esServidor)
        {
            IniciarServidor();
        }
        else
        {
            ConectarCliente(serverIP);
        }
    }

    // ---------------- SERVIDOR ----------------
    public void IniciarServidor()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Debug.Log("✅ Servidor iniciado en puerto " + port);

            // Mostrar la IP del servidor
            string ipLocal = ObtenerIPLocal();
            Debug.Log("💻 La IP de este servidor es: " + ipLocal);

            // Aceptar cliente de forma asíncrona
            server.AcceptTcpClientAsync().ContinueWith(t =>
            {
                client = t.Result;
                reader = new StreamReader(client.GetStream());
                writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
                Debug.Log("Cliente conectado al servidor.");
                EscucharMensajes();
            });
        }
        catch (System.Exception ex)
        {
            Debug.LogError("❌ Error al iniciar servidor: " + ex.Message);
        }
    }

    // ---------------- CLIENTE ----------------
    public void ConectarCliente(string ip)
    {
        try
        {
            client = new TcpClient(ip, port);
            reader = new StreamReader(client.GetStream());
            writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
            Debug.Log("✅ Cliente conectado a " + ip + ":" + port);
            EscucharMensajes();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("❌ Error al conectar cliente: " + ex.Message);
        }
    }

    // ---------------- ESCUCHAR MENSAJES ----------------
    private void EscucharMensajes()
    {
        new Thread(() =>
        {
            while (true)
            {
                try
                {
                    string mensaje = reader.ReadLine();
                    if (mensaje == null) continue;

                    if (mensaje.StartsWith("INFO"))
                    {
                        // INFO:jugador1,color1,jugador2,color2
                        string data = mensaje.Substring(5);
                        string[] partes = data.Split(',');

                        GameManager.Instance.jugador1 = partes[0];
                        GameManager.Instance.color1 = int.Parse(partes[1]);
                        GameManager.Instance.jugador2 = partes[2];
                        GameManager.Instance.color2 = int.Parse(partes[3]);

                        Debug.Log($"📩 Recibidos datos de registro → J1:{GameManager.Instance.jugador1} (Color {GameManager.Instance.color1}), " +
                                  $"J2:{GameManager.Instance.jugador2} (Color {GameManager.Instance.color2})");
                    }
                    else if (mensaje.StartsWith("TURN"))
                    {
                        int nuevoTurno = int.Parse(mensaje.Split(':')[1]);
                        GameManager.Instance.turno = nuevoTurno;
                        Debug.Log("📩 Recibido turno: " + nuevoTurno);

                        // Reinicia la fase al recibir turno
                        GameManager.Instance.fase = 1;

                        // Si este turno es mío, calculo refuerzos con mis propias cartas
                        if ((GameManager.Instance.esServidor && nuevoTurno == 1) ||
                            (!GameManager.Instance.esServidor && nuevoTurno == 2))
                        {
                            GameManager.Instance.IniciarRefuerzos();
                        }
                    }
                    else if (mensaje.StartsWith("FASE"))
                    {
                        int nuevaFase = int.Parse(mensaje.Split(':')[1]);
                        GameManager.Instance.fase = nuevaFase;
                        Debug.Log("📩 Recibida fase: " + nuevaFase);
                    }
                    else if (mensaje == "DERROTA")
                    {
                        GameManager.Instance.RecibirMensajeRed(mensaje);
                    }

                    else if (mensaje.StartsWith("COUNTRY"))
                    {
                        // COUNTRY:ID,dueño,tropas,continente,fase
                        string data = mensaje.Substring(8);
                        string[] partes = data.Split(',');

                        int id = int.Parse(partes[0]);
                        int nuevoDueño = int.Parse(partes[1]);
                        int nuevasTropas = int.Parse(partes[2]);
                        string continente = partes[3];
                        int fase = int.Parse(partes[4]);

                        BotonHandler[] paises = GameObject.FindObjectsOfType<BotonHandler>();
                        BotonHandler pais = System.Array.Find(paises, p => p.ID == id);

                        if (pais != null)
                        {
                            pais.dueño = nuevoDueño;
                            pais.tropas = nuevasTropas;
                            pais.continente = continente;
                            GameManager.Instance.fase = fase; // Actualiza fase global

                            Debug.Log($"📩 País {id} actualizado → Dueño={nuevoDueño}, Tropas={nuevasTropas}, Continente={continente}, Fase={fase}");
                        }
                    }
                }
                catch
                {
                    Debug.Log("⚠️ Conexión cerrada.");
                    break;
                }
            }
        }).Start();
    }

    // ---------------- ENVIAR MENSAJES ----------------
    public void EnviarMensaje(string mensaje)
    {
        if (writer != null)
        {
            writer.WriteLine(mensaje);
            Debug.Log("📤 Enviado: " + mensaje);
        }
    }

    public void EnviarEstadoPais(BotonHandler pais)
    {
        string mensaje = $"COUNTRY:{pais.ID},{pais.dueño},{pais.tropas},{pais.continente},{GameManager.Instance.fase}";
        EnviarMensaje(mensaje);
    }

    // ---------------- OBTENER IP LOCAL ----------------
    private string ObtenerIPLocal()
    {
        string ipLocal = "No encontrada";
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                ipLocal = ip.ToString();
                break;
            }
        }
        return ipLocal;
    }
}

