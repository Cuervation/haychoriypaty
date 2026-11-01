using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    public enum StreetBonusCard
    {
        ParrillaAFondo,
        OtroParrillero,
        CocaExpress,
        OtroCocacolero,
        MozoRelampago,
        OtroMozo,
        TodosAFondo,
        EntregaDorada,
        ManosRapidas,
        RefuerzoDeEmergencia
    }

    /// <summary>Attempt-scoped rules and state for Huracán's timed Bonus Track.</summary>
    [Serializable]
    public sealed class StreetBonusTrackState
    {
        public const float DurationSeconds = 120f;
        public const int MaximumChoices = 5;
        public const int AguanteChargeDeliveries = 10;
        public const int MaximumGoldenDeliveries = 10;
        public const int MaximumHandsFastStacks = 2;
        // Provisional tuning only; final star thresholds require the requested strategy simulations.
        public const int BasicStarScore = 3000;
        public const int GoodStarScore = 6500;
        public const int ExcellentStarScore = 10000;
        public const int MaxSpeedStacks = 3;
        public const int MaxRoleWorkers = 4;
        public const float GlobalBoostSeconds = 15f;
        public const float AguanteBoostSeconds = 8f;

        [SerializeField] private int seed;
        [SerializeField] private int choiceCount;
        [SerializeField] private bool selectionPending;
        [SerializeField] private bool emergencyRoleSelectionPending;
        [SerializeField] private bool superBonusPending;
        [SerializeField] private int score;
        [SerializeField] private int deliveries;
        [SerializeField] private int completedOrders;
        [SerializeField] private int parrillaStacks;
        [SerializeField] private int cocaStacks;
        [SerializeField] private int mozoStacks;
        [SerializeField] private int handsFastStacks;
        [SerializeField] private int goldenDeliveriesRemaining;
        [SerializeField] private int aguanteProgress;
        [SerializeField] private bool aguanteReady;
        [SerializeField] private float globalBoostRemaining;
        [SerializeField] private float aguanteBoostRemaining;
        [SerializeField] private StreetBonusCard[] currentCards = Array.Empty<StreetBonusCard>();
        [SerializeField] private StreetBonusCard[] chosenCards = Array.Empty<StreetBonusCard>();
        [SerializeField] private StreetWorkerRole[] emergencyRoles = Array.Empty<StreetWorkerRole>();
        [SerializeField] private List<string> choiceHistory = new List<string>();

        private System.Random random;
        public int Score => score;
        public int Deliveries => deliveries;
        public int CompletedOrders => completedOrders;
        public int ChoiceCount => choiceCount;
        public bool SelectionPending => selectionPending;
        public bool EmergencyRoleSelectionPending => emergencyRoleSelectionPending;
        public bool IsSuperBonusPending => superBonusPending;
        public bool AguanteReady => aguanteReady && aguanteBoostRemaining <= 0f;
        public bool AguanteActive => aguanteBoostRemaining > 0f;
        public float AguanteProgress => aguanteReady ? 1f : Mathf.Clamp01(aguanteProgress / (float)AguanteChargeDeliveries);
        public float GlobalBoostRemaining => globalBoostRemaining;
        public float AguanteBoostRemaining => aguanteBoostRemaining;
        public int GoldenDeliveriesRemaining => goldenDeliveriesRemaining;
        public int ParrillaStacks => parrillaStacks;
        public int CocaStacks => cocaStacks;
        public int MozoStacks => mozoStacks;
        public int HandsFastStacks => handsFastStacks;
        public IReadOnlyList<StreetBonusCard> CurrentCards => currentCards;
        public IReadOnlyList<StreetWorkerRole> EmergencyRoles => emergencyRoles;
        public IReadOnlyList<string> ChoiceHistory => choiceHistory;
        public int Stars => score >= ExcellentStarScore ? 3 : score >= GoodStarScore ? 2 : score >= BasicStarScore ? 1 : 0;
        public int TicketsAwardedThisRun => Stars > 0 ? 1 : 0;

        public StreetBonusTrackState(int randomSeed) { Begin(randomSeed); }

        public void Begin(int randomSeed)
        {
            seed = randomSeed;
            random = new System.Random(seed);
            choiceCount = score = deliveries = completedOrders = 0;
            selectionPending = emergencyRoleSelectionPending = superBonusPending = false;
            parrillaStacks = cocaStacks = mozoStacks = handsFastStacks = goldenDeliveriesRemaining = 0;
            aguanteProgress = 0;
            aguanteReady = false;
            globalBoostRemaining = aguanteBoostRemaining = 0f;
            currentCards = Array.Empty<StreetBonusCard>();
            chosenCards = Array.Empty<StreetBonusCard>();
            emergencyRoles = Array.Empty<StreetWorkerRole>();
            choiceHistory = new List<string>(MaximumChoices);
        }

        public void Advance(float delta)
        {
            if (delta <= 0f || selectionPending) return;
            globalBoostRemaining = Mathf.Max(0f, globalBoostRemaining - delta);
            if (aguanteBoostRemaining > 0f)
            {
                aguanteBoostRemaining = Mathf.Max(0f, aguanteBoostRemaining - delta);
                if (aguanteBoostRemaining <= 0f)
                {
                    aguanteProgress = 0;
                    aguanteReady = false;
                }
            }
        }

        public void ReachChoice(StreetSimulation sim)
        {
            if (selectionPending || choiceCount >= MaximumChoices || sim == null) return;
            superBonusPending = choiceCount == 2;
            BuildChoices(sim, superBonusPending);
            selectionPending = currentCards.Length == 3;
        }

        private void BuildChoices(StreetSimulation sim, bool superBonus)
        {
            var pool = new List<StreetBonusCard>(superBonus ? 4 : 10);
            if (!superBonus)
            {
                pool.Add(StreetBonusCard.ParrillaAFondo);
                pool.Add(StreetBonusCard.OtroParrillero);
                pool.Add(StreetBonusCard.CocaExpress);
                pool.Add(StreetBonusCard.OtroCocacolero);
                pool.Add(StreetBonusCard.MozoRelampago);
                pool.Add(StreetBonusCard.OtroMozo);
            }
            pool.Add(StreetBonusCard.TodosAFondo);
            pool.Add(StreetBonusCard.EntregaDorada);
            pool.Add(StreetBonusCard.ManosRapidas);
            pool.Add(StreetBonusCard.RefuerzoDeEmergencia);
            pool.RemoveAll(card => !CanApply(card, sim));
            currentCards = SelectThree(pool, sim, superBonus);
            emergencyRoles = Array.Empty<StreetWorkerRole>();
        }

        private StreetBonusCard[] SelectThree(List<StreetBonusCard> pool, StreetSimulation sim, bool superBonus)
        {
            var selected = new StreetBonusCard[3];
            var remaining = new List<StreetBonusCard>(pool);
            int count = 0;
            if (!superBonus)
            {
                count = PickCategory(remaining, selected, count, sim, 0); // production
                count = PickCategory(remaining, selected, count, sim, 1); // distribution
                count = PickCategory(remaining, selected, count, sim, 2); // strategic
            }
            while (count < selected.Length && remaining.Count > 0)
                count = PickWeighted(remaining, selected, count, sim);
            return count == selected.Length ? selected : Array.Empty<StreetBonusCard>();
        }

        private int PickCategory(List<StreetBonusCard> remaining, StreetBonusCard[] selected, int count,
            StreetSimulation sim, int category)
        {
            var choices = new List<StreetBonusCard>(remaining.Count);
            for (int i = 0; i < remaining.Count; i++)
                if (Category(remaining[i]) == category) choices.Add(remaining[i]);
            if (choices.Count == 0) return count;
            float total = 0f;
            for (int i = 0; i < choices.Count; i++) total += Weight(choices[i], sim);
            float roll = (float)random.NextDouble() * total;
            StreetBonusCard picked = choices[choices.Count - 1];
            for (int i = 0; i < choices.Count; i++)
            {
                roll -= Weight(choices[i], sim);
                if (roll <= 0f) { picked = choices[i]; break; }
            }
            selected[count++] = picked;
            remaining.Remove(picked);
            return count;
        }

        private int PickWeighted(List<StreetBonusCard> remaining, StreetBonusCard[] selected, int count,
            StreetSimulation sim)
        {
            float total = 0f;
            for (int i = 0; i < remaining.Count; i++) total += Weight(remaining[i], sim);
            float roll = (float)random.NextDouble() * total;
            int picked = remaining.Count - 1;
            for (int i = 0; i < remaining.Count; i++)
            {
                roll -= Weight(remaining[i], sim);
                if (roll <= 0f) { picked = i; break; }
            }
            selected[count++] = remaining[picked];
            remaining.RemoveAt(picked);
            return count;
        }

        private static int Category(StreetBonusCard card)
        {
            switch (card)
            {
                case StreetBonusCard.ParrillaAFondo:
                case StreetBonusCard.OtroParrillero:
                case StreetBonusCard.CocaExpress:
                case StreetBonusCard.OtroCocacolero: return 0;
                case StreetBonusCard.MozoRelampago:
                case StreetBonusCard.OtroMozo: return 1;
                default: return 2;
            }
        }

        private float Weight(StreetBonusCard card, StreetSimulation sim)
        {
            int sandwichTable = sim.Kitchen.TableCount(0) + sim.Kitchen.TableCount(1);
            int queue = sim.Customers.Count;
            int cocaTable = sim.Kitchen.TableCount(4);
            switch (card)
            {
                case StreetBonusCard.ParrillaAFondo: return Mathf.Clamp(1.25f - sandwichTable * .035f + queue * .015f, .45f, 1.65f);
                case StreetBonusCard.OtroParrillero: return Mathf.Clamp(1.2f - sandwichTable * .025f + queue * .012f, .55f, 1.7f);
                case StreetBonusCard.CocaExpress: return Mathf.Clamp(1.15f - cocaTable * .06f + queue * .01f, .45f, 1.55f);
                case StreetBonusCard.OtroCocacolero: return Mathf.Clamp(1.1f - cocaTable * .04f + queue * .008f, .5f, 1.5f);
                case StreetBonusCard.MozoRelampago: return Mathf.Clamp(.75f + sandwichTable * .04f + cocaTable * .03f, .6f, 1.8f);
                case StreetBonusCard.OtroMozo: return Mathf.Clamp(.8f + sandwichTable * .025f + cocaTable * .02f, .6f, 1.55f);
                case StreetBonusCard.TodosAFondo: return globalBoostRemaining > 0f ? .35f : 1f;
                case StreetBonusCard.EntregaDorada: return 1f;
                case StreetBonusCard.ManosRapidas: return Mathf.Max(.45f, 1.15f - handsFastStacks * .2f);
                case StreetBonusCard.RefuerzoDeEmergencia: return emergencyRoles.Length == 0 ? 1f : .8f;
                default: return 1f;
            }
        }

        private bool CanApply(StreetBonusCard card, StreetSimulation sim)
        {
            switch (card)
            {
                case StreetBonusCard.ParrillaAFondo: return parrillaStacks < MaxSpeedStacks;
                case StreetBonusCard.OtroParrillero: return sim.CanAddBonusWorker(StreetWorkerRole.Parrillero);
                case StreetBonusCard.CocaExpress: return cocaStacks < MaxSpeedStacks;
                case StreetBonusCard.OtroCocacolero: return sim.CanAddBonusWorker(StreetWorkerRole.Cocacolero);
                case StreetBonusCard.MozoRelampago: return mozoStacks < MaxSpeedStacks;
                case StreetBonusCard.OtroMozo: return sim.CanAddBonusWorker(StreetWorkerRole.Mozo);
                case StreetBonusCard.TodosAFondo: return globalBoostRemaining <= 0f;
                case StreetBonusCard.EntregaDorada: return goldenDeliveriesRemaining == 0;
                case StreetBonusCard.ManosRapidas: return handsFastStacks < MaximumHandsFastStacks;
                case StreetBonusCard.RefuerzoDeEmergencia: return sim.GetBonusWorkerChoices().Length > 0;
                default: return false;
            }
        }

        public bool ChooseCard(int index, StreetSimulation sim)
        {
            if (!selectionPending || emergencyRoleSelectionPending || index < 0 || index >= currentCards.Length || sim == null) return false;
            StreetBonusCard card = currentCards[index];
            if (!CanApply(card, sim)) return false;
            selectionPending = false;
            currentCards = Array.Empty<StreetBonusCard>();
            choiceCount++;
            Array.Resize(ref chosenCards, choiceCount);
            chosenCards[choiceCount - 1] = card;
            choiceHistory.Add(CardTitle(card));
            switch (card)
            {
                case StreetBonusCard.ParrillaAFondo: parrillaStacks++; break;
                case StreetBonusCard.OtroParrillero: sim.AddBonusWorker(StreetWorkerRole.Parrillero); break;
                case StreetBonusCard.CocaExpress: cocaStacks++; break;
                case StreetBonusCard.OtroCocacolero: sim.AddBonusWorker(StreetWorkerRole.Cocacolero); break;
                case StreetBonusCard.MozoRelampago: mozoStacks++; break;
                case StreetBonusCard.OtroMozo: sim.AddBonusWorker(StreetWorkerRole.Mozo); break;
                case StreetBonusCard.TodosAFondo: globalBoostRemaining = GlobalBoostSeconds; break;
                case StreetBonusCard.EntregaDorada: goldenDeliveriesRemaining = MaximumGoldenDeliveries; break;
                case StreetBonusCard.ManosRapidas: handsFastStacks++; break;
                case StreetBonusCard.RefuerzoDeEmergencia:
                    emergencyRoles = sim.GetBonusWorkerChoices();
                    emergencyRoleSelectionPending = emergencyRoles.Length > 0;
                    selectionPending = emergencyRoleSelectionPending;
                    break;
            }
            return true;
        }

        public bool ChooseEmergencyRole(StreetWorkerRole role, StreetSimulation sim)
        {
            if (!emergencyRoleSelectionPending || sim == null) return false;
            bool offered = false;
            for (int i = 0; i < emergencyRoles.Length; i++) if (emergencyRoles[i] == role) { offered = true; break; }
            if (!offered || !sim.AddBonusWorker(role)) return false;
            choiceHistory[choiceHistory.Count - 1] += " — " + RoleTitle(role);
            emergencyRoles = Array.Empty<StreetWorkerRole>();
            emergencyRoleSelectionPending = selectionPending = false;
            return true;
        }

        public void RecordDeliveredUnit()
        {
            deliveries++;
            int points = goldenDeliveriesRemaining > 0 ? 200 : 100;
            if (goldenDeliveriesRemaining > 0) goldenDeliveriesRemaining--;
            score = AddScore(score, points);
            if (!aguanteReady && aguanteBoostRemaining <= 0f)
            {
                aguanteProgress++;
                if (aguanteProgress >= AguanteChargeDeliveries) aguanteReady = true;
            }
        }

        public void RecordCompletedOrder()
        {
            completedOrders++;
            score = AddScore(score, 150);
        }

        public bool ActivateAguante()
        {
            if (!AguanteReady || selectionPending || aguanteBoostRemaining > 0f) return false;
            aguanteReady = false;
            aguanteProgress = 0;
            aguanteBoostRemaining = AguanteBoostSeconds;
            return true;
        }

        public float ProductionMultiplier(StreetWorkerRole role)
        {
            float value = 1f;
            if (role == StreetWorkerRole.Parrillero || role == StreetWorkerRole.ParrilleroPremium)
                value *= 1f + .30f * parrillaStacks;
            else if (role == StreetWorkerRole.Cocacolero || role == StreetWorkerRole.Fernetero)
                value *= 1f + .35f * cocaStacks;
            if (globalBoostRemaining > 0f || aguanteBoostRemaining > 0f) value *= 1.25f;
            return value;
        }

        public float MovementMultiplier(StreetWorkerRole role)
        {
            float value = (globalBoostRemaining > 0f || aguanteBoostRemaining > 0f) ? 1.25f : 1f;
            if (role == StreetWorkerRole.Mozo) value *= 1f + .40f * mozoStacks;
            return value;
        }

        public float PickupTimeMultiplier(StreetWorkerRole role)
        {
            return role == StreetWorkerRole.Mozo ? Mathf.Max(.4f, 1f - .30f * handsFastStacks) : 1f;
        }

        public static string CardTitle(StreetBonusCard card)
        {
            switch (card)
            {
                case StreetBonusCard.ParrillaAFondo: return "PARRILLA A FONDO";
                case StreetBonusCard.OtroParrillero: return "OTRO PARRILLERO";
                case StreetBonusCard.CocaExpress: return "COCA EXPRESS";
                case StreetBonusCard.OtroCocacolero: return "OTRO COCACOLERO";
                case StreetBonusCard.MozoRelampago: return "MOZO RELÁMPAGO";
                case StreetBonusCard.OtroMozo: return "OTRO MOZO";
                case StreetBonusCard.TodosAFondo: return "TODOS A FONDO";
                case StreetBonusCard.EntregaDorada: return "ENTREGA DORADA";
                case StreetBonusCard.ManosRapidas: return "MANOS RÁPIDAS";
                case StreetBonusCard.RefuerzoDeEmergencia: return "REFUERZO DE EMERGENCIA";
                default: return "MEJORA";
            }
        }

        public static string CardDescription(StreetBonusCard card)
        {
            switch (card)
            {
                case StreetBonusCard.ParrillaAFondo: return "+30% producción de Chori y Paty";
                case StreetBonusCard.OtroParrillero: return "Sumás un parrillero gratuito";
                case StreetBonusCard.CocaExpress: return "+35% preparación de Coca";
                case StreetBonusCard.OtroCocacolero: return "Sumás un cocacolero gratuito";
                case StreetBonusCard.MozoRelampago: return "+40% movimiento de mozos";
                case StreetBonusCard.OtroMozo: return "Sumás un mozo gratuito";
                case StreetBonusCard.TodosAFondo: return "+25% a todos durante 15 s";
                case StreetBonusCard.EntregaDorada: return "Próximas 10 entregas: 200 puntos";
                case StreetBonusCard.ManosRapidas: return "Retiro del mozo −30% por nivel (máx. −60%)";
                case StreetBonusCard.RefuerzoDeEmergencia: return "Elegís el rol del trabajador extra";
                default: return string.Empty;
            }
        }

        public static string RoleTitle(StreetWorkerRole role)
        {
            switch (role)
            {
                case StreetWorkerRole.Parrillero: return "PARRILLERO";
                case StreetWorkerRole.Cocacolero: return "COCACOLERO";
                case StreetWorkerRole.Mozo: return "MOZO";
                default: return role.ToString().ToUpperInvariant();
            }
        }

        private static int AddScore(int total, int amount) => total > int.MaxValue - amount ? int.MaxValue : total + amount;
    }

    [Serializable]
    public sealed class StreetBonusTrackProfile
    {
        public int bestScore, bestStars, bestDeliveries, bestOrders;
        public int lastScore, lastStars, lastDeliveries, lastOrders, lastTickets;
        public int previousBestScore;
        public long attemptsStarted, attemptsFinished;
        public bool threeStarTicketAwarded;
        public string lastAttemptId;

        public void Normalize()
        {
            bestScore = Math.Max(0, bestScore);
            bestStars = Mathf.Clamp(bestStars, 0, 3);
            bestDeliveries = Math.Max(0, bestDeliveries);
            bestOrders = Math.Max(0, bestOrders);
            lastScore = Math.Max(0, lastScore);
            lastStars = Mathf.Clamp(lastStars, 0, 3);
            lastDeliveries = Math.Max(0, lastDeliveries);
            lastOrders = Math.Max(0, lastOrders);
            lastTickets = Math.Max(0, lastTickets);
            previousBestScore = Math.Max(0, previousBestScore);
            attemptsStarted = Math.Max(0L, attemptsStarted);
            attemptsFinished = Math.Max(0L, attemptsFinished);
        }
    }
}
