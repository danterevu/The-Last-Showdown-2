using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AudioDatabase", menuName = "Audio/AudioDatabase")]
public class AudioDatabase : ScriptableObject
{
    [System.Serializable] //Muestra en el inspector
    public class AudioEntry // Agrupa toda la informacion de un sonido
    {
        public SoundID id;   // Nombre del ID que lo pones en el SoundID.cs (ej: PointAdd)   
        public AudioClip clip;  //Archivo del audio real
        [Range(0f, 1f)] public float volume = 1f; //Really?
        public bool loop = false; // x2
    }

    [SerializeField] private AudioEntry[] entries;

    private Dictionary<SoundID, AudioEntry> lookup;

    public AudioEntry Get(SoundID id) //Metodo publico llama al audio manager para pedirle el sonido
    {
        if (lookup == null) BuildLookup(); //Si no existe el diccionario crealo
        return lookup.TryGetValue(id, out var e) ? e : null; //intenta buscar el nombre clave y si no existe devuelve null
    }

    private void BuildLookup() //Convierte el array en un diccionario 
    {
        lookup = new Dictionary<SoundID, AudioEntry>();
        foreach (var e in entries)
            lookup[e.id] = e; //Guarda el entry bajo esta clave 
    }

    // Todo esto hace que puedas buscar cualquier sonido instantanamente por su id
}