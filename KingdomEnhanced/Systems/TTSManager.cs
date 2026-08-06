#pragma warning disable CA1416
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace KingdomEnhanced.Systems
{
    /// <summary>
    /// TTS via SAPI.SpVoice COM on a dedicated STA background thread.
    /// All COM calls happen off the game's main thread — zero stutter.
    /// </summary>
    public static class TTSManager
    {
        // The SpVoice COM object lives entirely on the background STA thread
        private static Thread _speakThread;
        private static volatile bool _threadRunning = false;
        private static bool _initialized = false;

        // Thread-safe queue: main thread writes, speak thread reads
        private static readonly Queue<string> _pendingQueue = new Queue<string>();
        private static readonly object        _queueLock   = new object();

        // Hard cap on queued messages; the oldest is dropped when exceeded
        private const int MaxQueueLength = 32;

        /// <summary>Last text actually spoken; exact duplicates within the dedupe window are dropped to prevent infinite loops.</summary>
        private static string _lastSpokenText = string.Empty;

        /// <summary>Timestamp (Time.unscaledTime) of the last actual speech.</summary>
        private static float _lastSpokenTime = float.MinValue;

        /// <summary>Dedupe window in seconds: identical text within the window is dropped.</summary>
        private const float DedupeWindowSeconds = 2.0f;

        // History of recently spoken messages for RepeatLast and ReadPreviousMessage
        private static readonly List<string> _messageLog = new List<string>();
        // Current position in the message history
        private static int _historyIndex = -1;
        // Maximum number of history entries retained
        private const  int MaxHistory   = 10;

        // Strips HTML-like tags (e.g. rich text color codes) from speech text
        private static readonly Regex _htmlTagRegex = new Regex(@"<.*?>", RegexOptions.Compiled);

        // Called once at startup from the main thread
        public static void Initialize()
        {
            if (_initialized) return;

            _threadRunning = true;
            _speakThread   = new Thread(SpeakThreadLoop)
            {
                Name         = "KE_TTS_Thread",
                IsBackground = true   // Killed automatically when game exits
            };
            // SAPI.SpVoice is an STA COM object — must run on an STA thread
            _speakThread.SetApartmentState(ApartmentState.STA);
            _speakThread.Start();

            _initialized = true;
            KingdomEnhanced.Core.Plugin.Instance.LogSource.LogInfo("[TTSManager] Background STA speak thread started.");
        }

        // ── Background thread ────────────────────────────────────────────────
        /// <summary>Background STA thread loop that creates the SAPI COM object and speaks queued text.</summary>
        private static void SpeakThreadLoop()
        {
            // Create the SAPI COM object on THIS thread (STA requirement)
            object   spVoice     = null;
            Type     spVoiceType = null;
            bool     comReady    = false;

            try
            {
                spVoiceType = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (spVoiceType == null)
                {
                    KingdomEnhanced.Core.Plugin.Instance.LogSource.LogError("[TTSManager] SAPI.SpVoice ProgID not found.");
                    return;
                }

                spVoice = Activator.CreateInstance(spVoiceType);
                if (spVoice == null)
                {
                    KingdomEnhanced.Core.Plugin.Instance.LogSource.LogError("[TTSManager] SAPI.SpVoice instance was null.");
                    return;
                }

                comReady = true;
                KingdomEnhanced.Core.Plugin.Instance.LogSource.LogInfo("[TTSManager] SAPI.SpVoice COM ready on STA thread.");
            }
            catch (Exception ex)
            {
                KingdomEnhanced.Core.Plugin.Instance.LogSource.LogError($"[TTSManager] STA thread COM init failed: {ex.Message}");
                return;
            }

            // Main loop — blocks on COM Speak (synchronous on this thread = no game stutter)
            while (_threadRunning)
            {
                string text = null;

                lock (_queueLock)
                {
                    if (_pendingQueue.Count > 0)
                        text = _pendingQueue.Dequeue();
                }

                if (text != null && comReady)
                {
                    try
                    {
                        // SVSFDefault (0) = synchronous on this thread.
                        // This blocks the speak thread (not the game thread) until audio finishes.
                        spVoiceType.InvokeMember(
                            "Speak",
                            BindingFlags.InvokeMethod,
                            null, spVoice,
                            new object[] { text, 0 }
                        );

                        // Record the recently spoken text for main-thread dedupe
                        lock (_queueLock)
                        {
                            _lastSpokenText = text;
                            _lastSpokenTime = UnityEngine.Time.unscaledTime;
                        }
                    }
                    catch (Exception ex)
                    {
                        KingdomEnhanced.Core.Plugin.Instance.LogSource.LogWarning($"[TTSManager] Speak error: {ex.Message}");
                    }
                }
                else
                {
                    // Nothing to speak — sleep to keep CPU usage near zero
                    Thread.Sleep(50);
                }
            }

            // Clean up COM on exit
            if (spVoice != null)
            {
                try { System.Runtime.InteropServices.Marshal.ReleaseComObject(spVoice); }
                catch { /* ignore */ }
            }
        }

        // ── Public API (called from main thread) ─────────────────────────────

        /// <summary>Called every frame from AccessibilityFeature.Update() — now a no-op.</summary>
        public static void Update() { /* speak thread is self-managing */ }

        /// <summary>Queues text for speech after stripping tags and applying the dedupe window.</summary>
        public static void Speak(string text, bool interrupt = true)
        {
            if (string.IsNullOrEmpty(text) || !_initialized) return;

            string clean = _htmlTagRegex.Replace(text, string.Empty);

            lock (_queueLock)
            {
                // Dedupe: drop exact matches to the last spoken text within the window to prevent infinite loops from per-frame callers
                if (string.Equals(clean, _lastSpokenText, StringComparison.Ordinal) &&
                    UnityEngine.Time.unscaledTime - _lastSpokenTime < DedupeWindowSeconds)
                {
                    return;
                }

                if (interrupt) _pendingQueue.Clear(); // Drop queued phrases

                // Queue cap: drop the oldest message when full so the queue cannot grow unbounded
                if (_pendingQueue.Count >= MaxQueueLength)
                {
                    _pendingQueue.Dequeue();
                }

                _pendingQueue.Enqueue(clean);
            }

            AddToHistory(clean);
        }

        /// <summary>Replays the most recently spoken message.</summary>
        public static void RepeatLast()
        {
            if (_messageLog.Count == 0) return;
            lock (_queueLock)
            {
                _pendingQueue.Clear();
                _pendingQueue.Enqueue(_messageLog[_messageLog.Count - 1]);
            }
        }

        /// <summary>Speaks the previous message from history, or announces the beginning of history.</summary>
        public static void ReadPreviousMessage()
        {
            if (_messageLog.Count == 0) return;
            _historyIndex--;
            if (_historyIndex < 0)
            {
                _historyIndex = 0;
                Speak("Beginning of history", interrupt: true);
                return;
            }
            Speak($"{_historyIndex + 1} of {_messageLog.Count}: {_messageLog[_historyIndex]}", interrupt: true);
        }

        /// <summary>Records a message in history, skipping consecutive duplicates and trimming to MaxHistory.</summary>
        private static void AddToHistory(string text)
        {
            if (_messageLog.Count > 0 && _messageLog[_messageLog.Count - 1] == text) return;
            _messageLog.Add(text);
            if (_messageLog.Count > MaxHistory) _messageLog.RemoveAt(0);
            _historyIndex = _messageLog.Count;
        }
    }
}
