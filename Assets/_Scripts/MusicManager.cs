using System.Collections.Generic;
using UnityEngine;

// Plays the music layers in sync (same loop length) and crossfades between them by mood.
// Calm: no threats. Tense: round alert or meteors alive. Outro: win/lose + credits.
public class MusicManager : MonoBehaviour
{
    private enum Mood { Calm, Tense, Outro }

    [Header("Layers (same length, played in sync)")]
    [SerializeField] private AudioClip calmClip;
    [SerializeField] private AudioClip tenseClip;
    [SerializeField] private AudioClip outroClip;

    [Header("Mix")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.7f;
    [SerializeField] private float fadeDuration = 1.5f;

    private AudioSource calmSource;
    private AudioSource tenseSource;
    private AudioSource outroSource;

    private readonly List<GameObject> threats = new List<GameObject>();
    private bool alertActive;
    private bool outro;

    private void Awake()
    {
        calmSource = CreateLayer(calmClip, musicVolume);
        tenseSource = CreateLayer(tenseClip, 0f);
        outroSource = CreateLayer(outroClip, 0f);

        // Start all layers on the same frame so they stay aligned while looping
        calmSource.Play();
        tenseSource.Play();
        outroSource.Play();
    }

    private void Update()
    {
        threats.RemoveAll(t => t == null || !t.CompareTag("Obstacle")); // Deflected (destroyed) or crashed (retagged)

        Mood mood = outro ? Mood.Outro : (alertActive || threats.Count > 0 ? Mood.Tense : Mood.Calm);

        float step = musicVolume / Mathf.Max(fadeDuration, 0.01f) * Time.unscaledDeltaTime;
        Fade(calmSource, mood == Mood.Calm, step);
        Fade(tenseSource, mood == Mood.Tense, step);
        Fade(outroSource, mood == Mood.Outro, step);
    }

    public void SetAlert(bool active) => alertActive = active;
    public void TrackThreat(GameObject threat) => threats.Add(threat);
    public void PlayOutro() => outro = true;

    private AudioSource CreateLayer(AudioClip clip, float volume)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = volume;
        return source;
    }

    private void Fade(AudioSource source, bool on, float step)
    {
        source.volume = Mathf.MoveTowards(source.volume, on ? musicVolume : 0f, step);
    }
}
