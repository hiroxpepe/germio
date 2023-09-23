// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using System;
using UnityEngine;

namespace Germio {
    /// <summary>
    /// sound system
    /// <author>h.adachi (STUDIO MeowToon)</author>
    /// </summary>
    public class SoundSystem : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField] AudioClip _se_item_clip;

        [SerializeField] AudioClip _se_jump_clip;

        [SerializeField] AudioClip _se_climb_clip;

        [SerializeField] AudioClip _se_walk_clip;

        [SerializeField] AudioClip _se_run_clip;

        [SerializeField] AudioClip _se_grounded_clip;

        [SerializeField] AudioClip _bgm_beat_level_clip;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        SEClip _now_playing_clip_se1;

        AudioSource _audio_source_se1;

        AudioSource _audio_source_bgm;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

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

        public void Play(BGMClip type) {
            switch (type) {
                case BGMClip.BeatLevel:
                    _audio_source_bgm.Stop();
                    _audio_source_bgm.clip = _bgm_beat_level_clip;
                    _audio_source_bgm.Play();
                    break;
            }
        }

        public void StopSEClip() {
            if (_now_playing_clip_se1 != SEClip.Grounded && _now_playing_clip_se1 != SEClip.Item) {
                _audio_source_se1.Stop();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Start is called before the first frame update
        void Start() {
            _audio_source_se1 = GetComponents<AudioSource>()[0]; // SE
            _audio_source_bgm = GetComponents<AudioSource>()[1]; // BGM
        }
    }

    public enum SEClip {
        Item,
        Jump,
        Climb,
        Walk,
        Run,
        Grounded,
        Push
    }

    public enum BGMClip {
        BeatLevel
    }
}