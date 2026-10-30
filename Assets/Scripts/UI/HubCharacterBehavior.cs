using System;
using UnityEngine;

namespace HayChoriYPaty
{
    public enum HubCharacterId { Seller, PoliceLeft, PoliceCenter, PoliceRight, Store, Boy, Girl }
    public enum HubCharacterAction { Idle, Look, Wave, Approve, Shift, Point, Interact, Step, Walk, Cheer, Disappointed }
    public enum HubCharacterEvent { Victory, Defeat }

    public struct HubCharacterState
    {
        public HubCharacterAction Action { get; internal set; }
        public float Progress01 { get; internal set; }
        public float Envelope { get; internal set; }
        public float Elapsed { get; internal set; }
    }

    public sealed class HubCharacterBehaviorSettings
    {
        public float SellerGapMin = 2.8f;
        public float SellerGapMax = 5f;
        public float PoliceGapMin = 2.5f;
        public float PoliceGapMax = 5.5f;
        public float StoreGapMin = 2.4f;
        public float StoreGapMax = 4.8f;
        public float KidsGapMin = 1.8f;
        public float KidsGapMax = 4f;
        public float GestureDurationMin = 1.2f;
        public float GestureDurationMax = 2f;
        public float EventDuration = 2.4f;
    }

    /// <summary>Seedable, presentation-only scheduler. It owns no Unity objects and never advances while hidden.</summary>
    public sealed class HubCharacterBehavior
    {
        private sealed class Slot
        {
            public HubCharacterAction action;
            public float elapsed, duration, rest;
        }

        private static readonly HubCharacterAction[] SellerActions = { HubCharacterAction.Wave, HubCharacterAction.Approve, HubCharacterAction.Look };
        private static readonly HubCharacterAction[] PoliceActions = { HubCharacterAction.Look, HubCharacterAction.Shift };
        private static readonly HubCharacterAction[] StoreActions = { HubCharacterAction.Look, HubCharacterAction.Interact, HubCharacterAction.Step };
        private static readonly HubCharacterAction[] ChildActions = { HubCharacterAction.Walk, HubCharacterAction.Cheer, HubCharacterAction.Look, HubCharacterAction.Step };
        private static readonly int[] EventRecipients = { 0, 5, 6 };

        private readonly System.Random random;
        private readonly HubCharacterBehaviorSettings settings;
        private readonly Slot[] slots = new Slot[7];
        private HubCharacterEvent? queuedEvent;

        public HubCharacterBehavior(int seed, HubCharacterBehaviorSettings settings = null)
        {
            random = new System.Random(seed);
            this.settings = Sanitize(settings ?? new HubCharacterBehaviorSettings());
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new Slot { action = HubCharacterAction.Idle };
                ScheduleRest(i);
            }
        }

        public void Notify(HubCharacterEvent hubEvent)
        {
            if (hubEvent != HubCharacterEvent.Victory && hubEvent != HubCharacterEvent.Defeat)
                throw new ArgumentOutOfRangeException(nameof(hubEvent));
            if (!queuedEvent.HasValue || Priority(hubEvent) >= Priority(queuedEvent.Value)) queuedEvent = hubEvent;
        }

        public void Tick(float dt, bool visible)
        {
            if (!visible || dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            int policeActive = -1;
            for (int i = 1; i <= 3; i++)
                if (slots[i].action != HubCharacterAction.Idle) { policeActive = i; break; }

            for (int i = 0; i < slots.Length; i++)
            {
                Slot s = slots[i];
                if (s.action == HubCharacterAction.Idle)
                {
                    s.rest -= dt;
                    if (s.rest <= 0f && !(i >= 1 && i <= 3 && policeActive >= 0))
                    {
                        StartGesture(i);
                        if (i >= 1 && i <= 3) policeActive = i;
                    }
                    continue;
                }
                s.elapsed += dt;
                if (s.elapsed < s.duration) continue;
                s.action = HubCharacterAction.Idle;
                s.elapsed = 0f;

                ScheduleRest(i);
            }

            // Start at the end of the tick after existing poses have reached their zero-envelope endpoint.
            if (queuedEvent.HasValue && EventRecipientsAreIdle())
            {
                StartEvent(queuedEvent.Value);
                queuedEvent = null;
            }
        }

        public HubCharacterState GetState(HubCharacterId id)
        {
            int index = (int)id;
            if (index < 0 || index >= slots.Length) throw new ArgumentOutOfRangeException(nameof(id));
            Slot s = slots[index];
            float p = s.duration <= 0f ? 0f : Mathf.Clamp01(s.elapsed / s.duration);
            // Smooth zero-ended envelope makes Idle handback continuous for every action.
            float sin = Mathf.Sin(Mathf.PI * p);
            float envelope = s.action == HubCharacterAction.Idle ? 0f : sin * sin;
            return new HubCharacterState { Action = s.action, Progress01 = p, Envelope = Mathf.Clamp01(envelope), Elapsed = s.elapsed };
        }

        private bool EventRecipientsAreIdle()
        {
            for (int i = 0; i < EventRecipients.Length; i++)
                if (slots[EventRecipients[i]].action != HubCharacterAction.Idle) return false;
            return true;
        }

        private void StartEvent(HubCharacterEvent e)
        {
            HubCharacterAction action = e == HubCharacterEvent.Victory ? HubCharacterAction.Cheer : HubCharacterAction.Disappointed;
            for (int i = 0; i < EventRecipients.Length; i++)
            {
                Slot s = slots[EventRecipients[i]];
                s.action = action; s.elapsed = 0f; s.duration = settings.EventDuration;
            }
        }

        private void StartGesture(int i)
        {
            HubCharacterAction[] choices = i == 0 ? SellerActions :
                i >= 1 && i <= 3 ? PoliceActions :
                i == 4 ? StoreActions : ChildActions;
            Slot s = slots[i]; s.action = choices[random.Next(choices.Length)]; s.elapsed = 0f;
            s.duration = Range(settings.GestureDurationMin, settings.GestureDurationMax);
        }

        private void ScheduleRest(int i)
        {
            Slot s = slots[i];
            if (i == 0) s.rest = Range(settings.SellerGapMin, settings.SellerGapMax);
            else if (i >= 1 && i <= 3) s.rest = Range(settings.PoliceGapMin, settings.PoliceGapMax);
            else if (i == 4) s.rest = Range(settings.StoreGapMin, settings.StoreGapMax);
            else s.rest = Range(settings.KidsGapMin, settings.KidsGapMax);
        }

        private float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);
        private static int Priority(HubCharacterEvent e) => e == HubCharacterEvent.Defeat ? 2 : 1;

        private static HubCharacterBehaviorSettings Sanitize(HubCharacterBehaviorSettings source)
        {
            var result = new HubCharacterBehaviorSettings();
            result.SellerGapMin = NonNegative(source.SellerGapMin, 8f);
            result.SellerGapMax = Math.Max(result.SellerGapMin, NonNegative(source.SellerGapMax, 5f));
            result.PoliceGapMin = NonNegative(source.PoliceGapMin, 8f);
            result.PoliceGapMax = Math.Max(result.PoliceGapMin, NonNegative(source.PoliceGapMax, 5.5f));
            result.StoreGapMin = NonNegative(source.StoreGapMin, 5f);
            result.StoreGapMax = Math.Max(result.StoreGapMin, NonNegative(source.StoreGapMax, 4.8f));
            result.KidsGapMin = NonNegative(source.KidsGapMin, 3f);
            result.KidsGapMax = Math.Max(result.KidsGapMin, NonNegative(source.KidsGapMax, 4f));
            result.GestureDurationMin = Math.Max(.01f, NonNegative(source.GestureDurationMin, 1.2f));
            result.GestureDurationMax = Math.Max(result.GestureDurationMin, NonNegative(source.GestureDurationMax, 2f));
            result.EventDuration = Math.Max(.01f, NonNegative(source.EventDuration, 2.4f));
            return result;
        }

        private static float NonNegative(float value, float fallback) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(0f, value);
    }
}
