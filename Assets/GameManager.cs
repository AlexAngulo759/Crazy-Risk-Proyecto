using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // Registrar callback para cuando se cargue una escena
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Este método se llama automáticamente cuando una escena termina de cargar
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene")
        {
            if (esServidor)
            {
                // Solo el servidor reparte
                DistribuirTerritorios();

                // Una vez terminado, enviar todos los países al cliente
                EnviarEstadoInicial();
            }
        }
    }
    public void EnviarEstadoInicial()
    {
        BotonHandler[] paises = FindObjectsOfType<BotonHandler>();
        foreach (var pais in paises)
        {
            MyNetworkManager.Instance.EnviarEstadoPais(pais);
        }

        Debug.Log("📤 Estado inicial de todos los países enviado al cliente.");
    }
    public static GameManager Instance;

    [Header("Alias y colores (menú)")]
    public string jugador1; // alias del servidor
    public string jugador2; // alias del cliente
    public int color1 = 0;  // 1=rojo, 2=verde, 3=azul
    public int color2 = 0;  // 1=rojo, 2=verde, 3=azul

    [Header("Turno y rol local")]
    public int turno = 1;
    public bool esServidor = true;

    [Header("Fase actual del juego")]
    public int fase = 1;

    public int tropasReforzar1 = 26; // servidor
    public int tropasReforzar2 = 26; // cliente

    public bool primerToque = true;
    public bool primerConquista = true;
    public BotonHandler paisSeleccionado = null;

    public bool movimientoPrimerToque = true;
    public BotonHandler movimientoOrigen = null;

    [Header("Grupos de países conectados con el mismo dueño")]
    public List<List<int>> gruposConectados = new List<List<int>>();

    // ================= CARTAS =================
    [Header("Cartas del jugador local")]
    public int infanteria = 0;
    public int caballeria = 0;
    public int artilleria = 0;

    private List<int> serieFibonacci = new List<int> { 2, 3, 5, 8, 13, 21 };
    private int indiceFibonacci = 0;

    // Control para refuerzos iniciales
    private bool primerCambioListo = false;


   

    // ================= ATAQUE =================
    public void SeleccionarPais(BotonHandler pais)
    {
        if (primerToque)
        {
            if ((turno == 1 && pais.dueño == 1) || (turno == 2 && pais.dueño == 2))
            {
                if (pais.tropas > 1)
                {
                    paisSeleccionado = pais;
                    primerToque = false;
                    Debug.Log($"✔ Primer país seleccionado para ataque: {pais.ID} con {pais.tropas} tropas");
                }
                else
                {
                    Debug.Log("❌ No puedes atacar con 1 tropa.");
                }
            }
            else
            {
                Debug.Log("❌ Debes seleccionar uno de tus países para iniciar ataque.");
            }
        }
        else
        {
            if (paisSeleccionado == null)
            {
                primerToque = true;
                return;
            }

            if (pais.dueño == paisSeleccionado.dueño || !paisSeleccionado.adyacentes.Contains(pais.ID))
            {
                Debug.Log("❌ Selección inválida. Reiniciando ataque.");
                paisSeleccionado = null;
                primerToque = true;
                return;
            }

            ResolverAtaque(paisSeleccionado, pais);

            paisSeleccionado = null;
            primerToque = true;
        }
    }

    void ResolverAtaque(BotonHandler atacante, BotonHandler defensor)
    {
        int dadosAtacante = Mathf.Min(atacante.tropas - 1, 3);
        int dadosDefensor = defensor.tropas >= 2 ? 2 : 1;

        List<int> tiradaAtacante = TirarDados(dadosAtacante);
        List<int> tiradaDefensor = TirarDados(dadosDefensor);

        tiradaAtacante.Sort((a, b) => b.CompareTo(a));
        tiradaDefensor.Sort((a, b) => b.CompareTo(a));

        Debug.Log($"🎲 Atacante: {string.Join(",", tiradaAtacante)} | Defensor: {string.Join(",", tiradaDefensor)}");

        int comparaciones = Mathf.Min(tiradaAtacante.Count, tiradaDefensor.Count);
        for (int i = 0; i < comparaciones; i++)
        {
            if (tiradaAtacante[i] > tiradaDefensor[i])
            {
                defensor.tropas--;
                Debug.Log($"⚔ País {defensor.ID} pierde una tropa. Tropas restantes = {defensor.tropas}");
            }
            else
            {
                atacante.tropas--;
                Debug.Log($"🛡 País {atacante.ID} pierde una tropa. Tropas restantes = {atacante.tropas}");
            }
        }

        if (defensor.tropas <= 0)
        {
            int mover = atacante.tropas - 1;
            defensor.tropas = mover;
            defensor.dueño = atacante.dueño;
            atacante.tropas = 1;

            Debug.Log($"🏆 País {defensor.ID} conquistado. Tropas movidas = {mover}");

            // Enviar estado al otro jugador
            defensor.EnviarEstado();

            // Dar carta solo si es la primera conquista de esta fase
            if (primerConquista)
            {
                OtorgarCartaPorConquista();
                primerConquista = false;
            }

            // ==== VERIFICAR VICTORIA MUNDIAL ====
            CheckVictoriaMundial(atacante.dueño);
        }


        atacante.EnviarEstado();
        defensor.EnviarEstado();
    }
    private void CheckVictoriaMundial(int atacanteId)
    {
    
        BotonHandler[] todosPaises = FindObjectsOfType<BotonHandler>();
        foreach (var pais in todosPaises)
        {
            if (pais.dueño != atacanteId)
                return; // Aún hay países de otros jugadores
        }

        // Todos los países son del atacante → Victoria mundial
        Debug.Log("🏅 ¡Victoria mundial! Cargando escena Victoria...");

        // Cargar escena local de Victoria
        UnityEngine.SceneManagement.SceneManager.LoadScene("Victoria");

        // Enviar mensaje al otro jugador para que cargue Derrota
        MyNetworkManager.Instance.EnviarMensaje("DERROTA");
    }
    public void RecibirMensajeRed(string mensaje)
    {
        if (mensaje == "DERROTA")
        {
            Debug.Log("💀 Has perdido. Cargando escena Derrota...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Derrota");
        }
    }



    List<int> TirarDados(int cantidad)
    {
        List<int> resultado = new List<int>();
        for (int i = 0; i < cantidad; i++)
        {
            resultado.Add(Random.Range(1, 7));
        }
        return resultado;
    }

    // ================= TURNOS =================
    public void CambiarTurno()
    {
        int turnoAnterior = turno;
        turno = (turno == 1) ? 2 : 1;
        Debug.Log("Turno cambiado. Ahora turno = " + turno);

        // Ignorar el primer 1→2 del menú
        if (!primerCambioListo && turnoAnterior == 1 && turno == 2)
        {
            primerCambioListo = true;
            Debug.Log("⏭ Primer cambio 1→2 ignorado (inicio del juego).");
        }
        else
        {
            // Refuerzos en cambios válidos
            if ((turnoAnterior == 2 && turno == 1) || (turnoAnterior == 1 && turno == 2))
            {
                IniciarRefuerzos();
            }
        }

        MyNetworkManager.Instance.EnviarMensaje("TURN:" + turno);

        // Reiniciar fase
        fase = 1;
    }

    // ================= REFUERZOS =================
    public void IniciarRefuerzos()
    {
        int tropasExtra = CalcularTropasRefuerzo();
        Debug.Log($"💂 Jugador {turno} recibe {tropasExtra} tropas de refuerzo.");
        if (turno == 1) tropasReforzar1 += tropasExtra;
        else tropasReforzar2 += tropasExtra;
    }

    private int CalcularTropasRefuerzo()
    {
        int territorios = ContarTerritoriosJugador(turno);
        int tropas = Mathf.Max(3, territorios / 3);

        tropas += BonificacionContinentes(turno);
        tropas += CanjearCartas();

        return tropas;
    }

    public void OtorgarCartaPorConquista()
    {
        int tipo = Random.Range(0, 3);
        if (tipo == 0) infanteria++;
        else if (tipo == 1) caballeria++;
        else artilleria++;

        Debug.Log($"🃏 Jugador {turno} recibe carta: {(tipo == 0 ? "Infantería" : tipo == 1 ? "Caballería" : "Artillería")}");
    }

    private int CanjearCartas()
    {
        int tropas = 0;

        if (infanteria >= 3 || caballeria >= 3 || artilleria >= 3)
        {
            tropas = serieFibonacci[indiceFibonacci];
            AvanzarSerie();
            if (infanteria >= 3) infanteria -= 3;
            else if (caballeria >= 3) caballeria -= 3;
            else if (artilleria >= 3) artilleria -= 3;
        }
        else if (infanteria >= 1 && caballeria >= 1 && artilleria >= 1)
        {
            tropas = serieFibonacci[indiceFibonacci];
            AvanzarSerie();
            infanteria--; caballeria--; artilleria--;
        }

        if (tropas > 0)
            Debug.Log($"♻️ Canje de cartas: +{tropas} tropas extra");

        return tropas;
    }

    private void AvanzarSerie()
    {
        if (indiceFibonacci < serieFibonacci.Count - 1)
            indiceFibonacci++;
    }

    private int ContarTerritoriosJugador(int jugador)
    {
        int total = 0;
        foreach (var boton in FindObjectsOfType<BotonHandler>())
        {
            if (boton.dueño == jugador)
                total++;
        }
        return total;
    }

    private int BonificacionContinentes(int jugador)
    {
        // Definir bonificación de cada continente
        Dictionary<string, int> bonusPorContinente = new Dictionary<string, int>
    {
        { "Asia", 7 },
        { "America del norte", 3 },
        { "Europa", 5 },
        { "Africa", 3 },
        { "America del sur", 2 },
        { "Oceania", 2 }
    };

        int bonusTotal = 0;

        // Revisar cada continente
        foreach (var kvp in bonusPorContinente)
        {
            string continente = kvp.Key;
            int bonus = kvp.Value;

            if (ControlaContinente(jugador, continente))
            {
                bonusTotal += bonus;
                Debug.Log($"Jugador {jugador} controla {continente} (+{bonus} tropas)");
            }
        }

        return bonusTotal;
    }

    private bool ControlaContinente(int jugador, string continente)
    {
        BotonHandler[] paises = FindObjectsOfType<BotonHandler>();

        foreach (var pais in paises)
        {
            if (pais.continente == continente && pais.dueño != jugador)
            {
                // Si hay un país en ese continente que no es del jugador, no lo controla
                return false;
            }
        }

        return true; // todos los países de ese continente son del jugador
    }


    public void ActualizarDatosRegistro()
    {
        string datos = $"INFO:{jugador1},{color1},{jugador2},{color2}";
        MyNetworkManager.Instance.EnviarMensaje(datos);
        Debug.Log("📤 Datos de registro enviados: " + datos);
    }

    public void DistribuirTerritorios()
    {
        List<int> ids = Enumerable.Range(1, 42).ToList();
        System.Random rng = new System.Random();
        ids = ids.OrderBy(x => rng.Next()).ToList();
       
        BotonHandler[] paises = FindObjectsOfType<BotonHandler>();
 

        for (int i = 0; i < 14; i++)
        {
            var pais = paises.FirstOrDefault(p => p.ID == ids[i]);
            if (pais != null) { pais.dueño = 1; pais.tropas = 1; }
        }

        for (int i = 14; i < 28; i++)
        {
            Debug.Log(i);
            var pais = paises.FirstOrDefault(p => p.ID == ids[i]);
            if (pais != null) { pais.dueño = 2; pais.tropas = 1; }
        }

        List<BotonHandler> neutros = new List<BotonHandler>();
        for (int i = 28; i < 42; i++)
        {
            var pais = paises.FirstOrDefault(p => p.ID == ids[i]);
            if (pais != null) { pais.dueño = 3; pais.tropas = 1; neutros.Add(pais); }
        }

        int tropasRestantes = 40 - neutros.Count;
        while (tropasRestantes > 0)
        {
            
            var paisAleatorio = neutros[rng.Next(neutros.Count)];
            paisAleatorio.tropas++;
            tropasRestantes--;
        }

        Debug.Log("Territorios distribuidos correctamente, incluyendo ejército neutro");
    }

    // ================= MOVIMIENTO (fase 3) =================
    public void SeleccionarMovimiento(BotonHandler paisTocado)
    {
        if (fase != 3)
        {
            Debug.Log("No estás en fase 3 (movilización).");
            return;
        }

        int jugadorActivo = turno;
        if (!(paisTocado.dueño == jugadorActivo))
        {
            Debug.Log($"No puedes seleccionar país {paisTocado.ID}: no te pertenece.");
            return;
        }

        if (movimientoPrimerToque)
        {
            if (paisTocado.tropas <= 1)
            {
                Debug.Log($"País {paisTocado.ID} no puede ser origen (solo tiene {paisTocado.tropas} tropa(s)).");
                return;
            }
            movimientoOrigen = paisTocado;
            movimientoPrimerToque = false;
            Debug.Log($"Origen seleccionado para movilización: {movimientoOrigen.ID}");
            return;
        }

        if (movimientoOrigen == null)
        {
            movimientoPrimerToque = true;
            return;
        }

        if (paisTocado.ID == movimientoOrigen.ID)
        {
            movimientoPrimerToque = true;
            movimientoOrigen = null;
            return;
        }

        if (paisTocado.dueño != movimientoOrigen.dueño)
        {
            Debug.Log($"Destino {paisTocado.ID} no pertenece al mismo jugador.");
            return;
        }

        bool conectados = EstanConectados(movimientoOrigen.ID, paisTocado.ID, movimientoOrigen.dueño);
        if (!conectados)
        {
            Debug.Log($"No hay camino válido entre {movimientoOrigen.ID} y {paisTocado.ID}.");
            return;
        }

        if (movimientoOrigen.tropas <= 1)
        {
            movimientoPrimerToque = true;
            movimientoOrigen = null;
            return;
        }

        movimientoOrigen.tropas -= 1;
        paisTocado.tropas += 1;

        if (MyNetworkManager.Instance != null)
        {
            MyNetworkManager.Instance.EnviarEstadoPais(movimientoOrigen);
            MyNetworkManager.Instance.EnviarEstadoPais(paisTocado);
        }

        if (movimientoOrigen.tropas <= 1)
        {
            movimientoPrimerToque = true;
            movimientoOrigen = null;
        }
    }

    public bool EstanConectados(int idFrom, int idTo, int owner)
    {
        BotonHandler[] todos = GameObject.FindObjectsOfType<BotonHandler>();
        Dictionary<int, BotonHandler> mapa = new Dictionary<int, BotonHandler>();
        foreach (var b in todos) mapa[b.ID] = b;

        if (!mapa.ContainsKey(idFrom) || !mapa.ContainsKey(idTo)) return false;

        Queue<int> q = new Queue<int>();
        HashSet<int> visitados = new HashSet<int>();

        q.Enqueue(idFrom);
        visitados.Add(idFrom);

        while (q.Count > 0)
        {
            int actual = q.Dequeue();
            if (actual == idTo) return true;

            BotonHandler bh = mapa[actual];
            foreach (int ady in bh.adyacentes)
            {
                if (visitados.Contains(ady)) continue;
                if (!mapa.ContainsKey(ady)) continue;
                BotonHandler vecino = mapa[ady];
                if (vecino.dueño != owner) continue;

                visitados.Add(ady);
                q.Enqueue(ady);
            }
        }
        return false;
    }
}

