// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using UnityEngine;

namespace Germio {
    /// <summary>
    /// Manages sound effects and background music.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class SoundSystem : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        /// <summary>
        /// Gets the sound effect clip for item interaction.
        /// </summary>
        [SerializeField] AudioClip _se_item_clip;

        /// <summary>
        /// Gets the sound effect clip for jumping.
        /// </summary>
        [SerializeField] AudioClip _se_jump_clip;

        /// <summary>
        /// Gets the sound effect clip for climbing.
        /// </summary>
        [SerializeField] AudioClip _se_climb_clip;

        /// <summary>
        /// Gets the sound effect clip for walking.
        /// </summary>
        [SerializeField] AudioClip _se_walk_clip;

        /// <summary>
        /// Gets the sound effect clip for running.
        /// </summary>
        [SerializeField] AudioClip _se_run_clip;

        /// <summary>
        /// Gets the sound effect clip for grounding.
        /// </summary>
        [SerializeField] AudioClip _se_grounded_clip;

        /// <summary>
        /// Gets the background music clip for level beat.
        /// </summary>
        [SerializeField] AudioClip _bgm_beat_level_clip;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        /// <summary>
        /// Gets the currently playing sound effect type.
        /// </summary>
        SEClip _now_playing_clip_se1;

        /// <summary>
        /// Gets the audio source for sound effects.
        /// </summary>
        AudioSource _audio_source_se1;

        /// <summary>
        /// Gets the audio source for background music.
        /// </summary>
        AudioSource _audio_source_bgm;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>
        /// Plays the specified sound effect clip.
        /// </summary>
        /// <param name="type">Type of sound effect to play.</param>
        public void Play(SEClip type) {
            switch (type) {
                case SEClip.Item:
                    if (_audio_source_se1.isPlaying) {
                        _audio_source_se1.Stop();
                    }
                    _audio_source_se1.PlayOneShot(clip: _se_item_clip, volumeScale: 2.5f);
                    _now_playing_clip_se1 = SEClip.Item;
                    break;
                case SEClip.Jump:
                    if (_audio_source_se1.isPlaying) {
                        _audio_source_se1.Stop();
                    }
                    _audio_source_se1.PlayOneShot(clip: _se_jump_clip);
                    _now_playing_clip_se1 = SEClip.Jump;
                    break;
                case SEClip.Climb:
                    if (_now_playing_clip_se1 != SEClip.Climb) {
                        _audio_source_se1.Stop();
                    }
                    if (!_audio_source_se1.isPlaying) {
                        _audio_source_se1.clip = _se_climb_clip;
                        _audio_source_se1.Play();
                        _now_playing_clip_se1 = SEClip.Climb;
                    }
                    break;
                case SEClip.Walk:
                    if (_now_playing_clip_se1 != SEClip.Walk && 
                        _now_playing_clip_se1 != SEClip.Grounded && 
                        _now_playing_clip_se1 != SEClip.Item) {
                        _audio_source_se1.Stop();
                    }
                    if (!_audio_source_se1.isPlaying) {
                        _audio_source_se1.clip = _se_walk_clip;
                        _audio_source_se1.Play();
                        _now_playing_clip_se1 = SEClip.Walk;
                    }
                    break;
                case SEClip.Run:
                    if (_now_playing_clip_se1 != SEClip.Run && 
                        _now_playing_clip_se1 != SEClip.Grounded && 
                        _now_playing_clip_se1 != SEClip.Item) {
                        _audio_source_se1.Stop();
                    }
                    if (!_audio_source_se1.isPlaying) {
                        _audio_source_se1.clip = _se_run_clip;
                        _audio_source_se1.Play();
                        _now_playing_clip_se1 = SEClip.Run;
                    }
                    break;
                case SEClip.Grounded:
                    if (_audio_source_se1.isPlaying) {
                        _audio_source_se1.Stop();
                    }
                    _audio_source_se1.PlayOneShot(clip: _se_grounded_clip, volumeScale: 0.65f);
                    _now_playing_clip_se1 = SEClip.Grounded;
                    break;
            }
        }

        /// <summary>
        /// Plays the specified background music clip.
        /// </summary>
        /// <param name="type">Type of background music to play.</param>
        public void Play(BGMClip type) {
            switch (type) {
                case BGMClip.BeatLevel:
                    _audio_source_bgm.Stop();
                    _audio_source_bgm.clip = _bgm_beat_level_clip;
                    _audio_source_bgm.Play();
                    break;
            }
        }

        /// <summary>
        /// Stops the currently playing sound effect clip.
        /// </summary>
        public void StopSEClip() {
            if (_now_playing_clip_se1 != SEClip.Grounded && _now_playing_clip_se1 != SEClip.Item) {
                _audio_source_se1.Stop();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Start is called before the first frame update.
        void Start() {
            _audio_source_se1 = GetComponents<AudioSource>()[0]; // SE
            _audio_source_bgm = GetComponents<AudioSource>()[1]; // BGM
        }
    }

    /// <summary>
    /// Specifies the types of sound effects.
    /// </summary>
    public enum SEClip {
        /// <summary>
        /// Represents the sound effect for item interaction.
        /// </summary>
        Item,

        /// <summary>
        /// Represents the sound effect for jumping.
        /// </summary>
        Jump,

        /// <summary>
        /// Represents the sound effect for climbing.
        /// </summary>
        Climb,

        /// <summary>
        /// Represents the sound effect for walking.
        /// </summary>
        Walk,

        /// <summary>
        /// Represents the sound effect for running.
        /// </summary>
        Run,

        /// <summary>
        /// Represents the sound effect for grounding.
        /// </summary>
        Grounded,

        /// <summary>
        /// Represents the sound effect for pushing.
        /// </summary>
        Push
    }

    /// <summary>
    /// Specifies the types of background music.
    /// </summary>
    public enum BGMClip {
        /// <summary>
        /// Represents the background music for level beat.
        /// </summary>
        BeatLevel
    }
}