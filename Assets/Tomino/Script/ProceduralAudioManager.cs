using UnityEngine;

public class ProceduralAudioManager : MonoBehaviour
{
    public static ProceduralAudioManager Instance;
    private float frecuenciaMuestreo;

    // --- VARIABLES PARA SFX (Efectos) ---
    private float faseSFX = 0f;
    private float frecuenciaSFX = 0f;
    private float volumenMaximoSFX = 0f;
    private float volumenActualSFX = 0f;
    private float tiempoRestanteSFX = 0f;
    private float tiempoTotalSFX = 0f;
    private float velocidadCaidaTono = 0f;
    
    private float[] frecuenciasArpegio;
    private int notaActualArpegio = 0;
    private float tiempoPorNotaArpegio = 0f;
    private float tiempoNotaRestanteArpegio = 0f;

    // --- VARIABLES PARA MÚSICA DE FONDO ---
    private bool musicaActiva = true; // Empieza encendida
    private float faseMusica = 0f;
    private float volumenMusica = 0.05f; // MUY BAJITO para no tapar los efectos
    // Un patrón de notas clásico y misterioso (La menor: A3, C4, E4, C4...)
    private float[] melodiaMusica = { 220.00f, 261.63f, 329.63f, 261.63f, 220.00f, 261.63f, 329.63f, 392.00f }; 
    private int indiceNotaMusica = 0;
    private float tiempoPorNotaMusica = 0.25f; // 4 notas por segundo
    private float tiempoNotaMusicaRestante = 0f;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } 
        else { Destroy(gameObject); }
    }

    void Start()
    {
        frecuenciaMuestreo = AudioSettings.outputSampleRate;
        tiempoNotaMusicaRestante = tiempoPorNotaMusica;
    }

    // ================= FUNCIONES DE EVENTOS =================

    public void PlayMoverPieza()
    {
        frecuenciaSFX = 220f; volumenMaximoSFX = 0.3f; tiempoTotalSFX = 0.05f;
        tiempoRestanteSFX = tiempoTotalSFX; velocidadCaidaTono = 0f; frecuenciasArpegio = null;
    }

    public void PlayRotarPieza()
    {
        frecuenciaSFX = 440f; volumenMaximoSFX = 0.3f; tiempoTotalSFX = 0.08f;
        tiempoRestanteSFX = tiempoTotalSFX; velocidadCaidaTono = 0f; frecuenciasArpegio = null;
    }

    public void PlayCaidaPieza()
    {
        frecuenciaSFX = 150f; volumenMaximoSFX = 0.5f; tiempoTotalSFX = 0.15f;
        tiempoRestanteSFX = tiempoTotalSFX; velocidadCaidaTono = 800f; frecuenciasArpegio = null;
    }

    public void PlayEliminarLinea()
    {
        frecuenciasArpegio = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }; 
        notaActualArpegio = 0; frecuenciaSFX = frecuenciasArpegio[0];
        volumenMaximoSFX = 0.4f; tiempoPorNotaArpegio = 0.1f; 
        tiempoNotaRestanteArpegio = tiempoPorNotaArpegio;
        tiempoTotalSFX = tiempoPorNotaArpegio * frecuenciasArpegio.Length; 
        tiempoRestanteSFX = tiempoTotalSFX; velocidadCaidaTono = 0f;
    }

    public void PlayGameOver()
    {
        musicaActiva = false; // ¡APAGAMOS LA MÚSICA AL PERDER!
        frecuenciaSFX = 200f; volumenMaximoSFX = 0.6f; tiempoTotalSFX = 2.0f;
        tiempoRestanteSFX = tiempoTotalSFX; velocidadCaidaTono = 100f; frecuenciasArpegio = null;
    }

    public void ReiniciarMusica()
    {
        musicaActiva = true;
        indiceNotaMusica = 0;
    }

    // ================= EL CEREBRO SINTETIZADOR =================
    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int i = 0; i < data.Length; i += channels)
        {
            float muestraFinal = 0f;

            // 1. GENERADOR DE MÚSICA DE FONDO
            if (musicaActiva)
            {
                tiempoNotaMusicaRestante -= 1f / frecuenciaMuestreo;
                if (tiempoNotaMusicaRestante <= 0)
                {
                    indiceNotaMusica++;
                    if (indiceNotaMusica >= melodiaMusica.Length) indiceNotaMusica = 0; // Loop infinito
                    tiempoNotaMusicaRestante = tiempoPorNotaMusica;
                }

                float freqMusicaActual = melodiaMusica[indiceNotaMusica];
                faseMusica += freqMusicaActual / frecuenciaMuestreo;
                if (faseMusica > 1f) faseMusica -= 1f;

                // Generamos onda triangular para la música (suena más a juego retro de 8 bits)
                float ondaMusica = Mathf.PingPong(faseMusica * 2f, 1f) * 2f - 1f;
                muestraFinal += ondaMusica * volumenMusica;
            }

            // 2. GENERADOR DE EFECTOS (SFX)
            if (tiempoRestanteSFX > 0)
            {
                if (frecuenciasArpegio != null && frecuenciasArpegio.Length > 0)
                {
                    tiempoNotaRestanteArpegio -= 1f / frecuenciaMuestreo;
                    if (tiempoNotaRestanteArpegio <= 0)
                    {
                        notaActualArpegio++;
                        if (notaActualArpegio < frecuenciasArpegio.Length)
                        {
                            frecuenciaSFX = frecuenciasArpegio[notaActualArpegio];
                            tiempoNotaRestanteArpegio = tiempoPorNotaArpegio;
                        }
                    }
                }

                if (velocidadCaidaTono > 0) {
                    frecuenciaSFX -= velocidadCaidaTono / frecuenciaMuestreo;
                    if (frecuenciaSFX < 20f) frecuenciaSFX = 20f;
                }

                faseSFX += frecuenciaSFX / frecuenciaMuestreo;
                if (faseSFX > 1f) faseSFX -= 1f;

                float proporcionTiempo = tiempoRestanteSFX / tiempoTotalSFX;
                volumenActualSFX = volumenMaximoSFX * proporcionTiempo;

                float ondaSFX = Mathf.Sin(faseSFX * Mathf.PI * 2f); // Onda seno para los efectos
                
                // Sumamos el SFX a la mezcla final
                muestraFinal += ondaSFX * volumenActualSFX;

                tiempoRestanteSFX -= 1f / frecuenciaMuestreo;
            }

            // 3. ENVIAR A LOS ALTAVOCES
            for (int c = 0; c < channels; c++)
            {
                data[i + c] = muestraFinal;
            }
        }
    }
}