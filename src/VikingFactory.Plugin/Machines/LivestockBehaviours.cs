using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VikingFactory.Core.Husbandry;
using VikingFactory.Core.Items;

namespace VikingFactory.Machines
{
    internal static class Herd
    {
        public static List<Character> TameNear(Vector3 position, float radius)
        {
            var list = new List<Character>();
            foreach (var character in Character.GetAllCharacters())
            {
                if (character == null || character.IsPlayer() || character.IsDead() || !character.IsTamed())
                    continue;
                if (Vector3.Distance(character.transform.position, position) <= radius)
                    list.Add(character);
            }

            return list;
        }

        public static string Species(Character character)
        {
            return character.gameObject.name.Replace("(Clone)", "").Trim();
        }
    }

    /// <summary>
    /// Feed gate. Releases one real food item onto its pad while a tame animal nearby is hungry and the pad is
    /// empty. The animal eats it through its own AI; taming, breeding, and growth stay native.
    /// </summary>
    public sealed class FeedGateBehaviour : MachineBehaviour
    {
        private StackStore _food = new StackStore(2, 50);
        private float _wait;
        private string _reason = "";

        public FeedGateBehaviour(WorkshopMachine machine) : base(machine) { }

        protected override void Load() { _food = new StackStore(2, 50, ItemCodec.Parse(M.GetString("vf_food"))); }
        protected override void Save() { M.Set("vf_food", ItemCodec.Format(_food.Stacks)); }

        private Vector3 Pad => M.transform.position + M.transform.forward * 1.2f + Vector3.up * 0.2f;

        private HashSet<string> Edible()
        {
            var set = new HashSet<string>();
            foreach (var animal in Herd.TameNear(M.transform.position, 15f))
            {
                var ai = animal.GetComponent<MonsterAI>();
                if (ai == null || ai.m_consumeItems == null)
                    continue;
                foreach (var item in ai.m_consumeItems)
                {
                    if (item != null)
                        set.Add(item.gameObject.name);
                }
            }

            return set;
        }

        public override void Tick(float dt)
        {
            if (!M.Producing)
                return;
            _wait -= dt;
            if (_wait > 0f)
                return;
            _wait = 4f;
            var edible = Edible();
            var hungry = Herd.TameNear(M.transform.position, 10f).Count(c =>
            {
                var tame = c.GetComponent<Tameable>();
                return tame != null && tame.IsHungry();
            });
            var onPad = 0;
            foreach (var hit in Physics.OverlapSphere(Pad, 1.5f, LayerMask.GetMask("item")))
            {
                var drop = hit.GetComponentInParent<ItemDrop>();
                if (drop != null && edible.Contains(drop.gameObject.name.Replace("(Clone)", "").Trim()))
                    onPad++;
            }

            if (!FeedPolicy.ShouldRelease(hungry, onPad))
            {
                _reason = hungry == 0 ? "No hungry tame animal nearby." : "Food is already on the pad.";
                return;
            }

            var food = _food.TakeOne(s => edible.Contains(s.Prefab));
            if (food == null)
            {
                _reason = "Out of food these animals eat.";
                return;
            }

            SaveNow();
            ItemBridge.Drop(food, Pad);
            _reason = "Released one " + food.Prefab + ".";
        }

        public override IItemEndpoint InputAt(Transform port)
        {
            return new StoreEndpoint(M, "feed", () => _food, SaveNow, s => Edible().Contains(s.Prefab), false);
        }

        public override List<ItemStack> Drain() { return _food.TakeAll(); }

        public override string Status() { return _reason + "\nFood: " + Describe(_food.Stacks); }
    }

    /// <summary>
    /// Culling gate. Off by default. Reviewed species only (boars), tamed unnamed adults above a breeding
    /// reserve. The kill uses native damage and native drops with no attacker, so no skill is raised.
    /// </summary>
    public sealed class CullingGateBehaviour : MachineBehaviour
    {
        private static readonly int[] Reserves = { 4, 2, 6, 8 };
        private readonly CullingPolicy _policy = new CullingPolicy();
        private int _reserveIndex;
        private StackStore _store = new StackStore(4, 50);
        private float _wait;
        private Vector3 _lastKill;
        private float _lastKillTime = -100f;
        private HashSet<string> _drops = new HashSet<string>();
        private string _reason = "";

        public CullingGateBehaviour(WorkshopMachine machine) : base(machine) { }

        protected override void Load()
        {
            _policy.Enabled = M.GetInt("vf_cull") == 1;
            _reserveIndex = Mathf.Clamp(M.GetInt("vf_reserve"), 0, Reserves.Length - 1);
            _policy.KeepAbove = Reserves[_reserveIndex];
            _store = new StackStore(4, 50, ItemCodec.Parse(M.GetString("vf_store")));
        }

        protected override void Save()
        {
            M.Set("vf_cull", _policy.Enabled ? 1 : 0);
            M.Set("vf_reserve", _reserveIndex);
            M.Set("vf_store", ItemCodec.Format(_store.Stacks));
        }

        public override void Tick(float dt)
        {
            if (Time.time - _lastKillTime < 15f && WorldDrops.Collect(_lastKill, 3f, _drops, _store) > 0)
                SaveNow();
            if (!M.Producing)
                return;
            _wait -= dt;
            if (_wait > 0f)
                return;
            _wait = 10f;

            var nearby = Herd.TameNear(M.transform.position, 8f);
            var animals = new List<Animal>();
            var byId = new Dictionary<string, Character>();
            foreach (var character in nearby)
            {
                var view = character.GetComponent<ZNetView>();
                var tame = character.GetComponent<Tameable>();
                if (view == null || !view.IsValid())
                    continue;
                var id = view.GetZDO().m_uid.ToString();
                byId[id] = character;
                animals.Add(new Animal
                {
                    Id = id,
                    Species = Herd.Species(character),
                    Tamed = true,
                    Adult = character.GetComponent<Growup>() == null,
                    Named = tame != null && !string.IsNullOrEmpty(tame.GetText()),
                    Level = character.GetLevel()
                });
            }

            var chosen = _policy.SelectSurplus(animals, out _reason);
            if (chosen == null)
                return;
            var target = byId[chosen.Id];
            var drop = target.GetComponent<CharacterDrop>();
            _drops = new HashSet<string>();
            if (drop != null)
            {
                foreach (var d in drop.m_drops)
                {
                    if (d.m_prefab != null)
                        _drops.Add(d.m_prefab.name);
                }
            }

            var hit = new HitData();
            hit.m_damage.m_damage = 1000000f;
            hit.m_point = target.transform.position;
            _lastKill = target.transform.position;
            _lastKillTime = Time.time;
            target.Damage(hit);
            _reason = "Culled one surplus " + chosen.Species + ".";
        }

        public override bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;
            if (alt)
            {
                _reserveIndex = (_reserveIndex + 1) % Reserves.Length;
                _policy.KeepAbove = Reserves[_reserveIndex];
                WorkshopMachine.Message(user, "Keeps at least " + Reserves[_reserveIndex] + " adults of each species.");
            }
            else
            {
                _policy.Enabled = !_policy.Enabled;
                WorkshopMachine.Message(user, _policy.Enabled ? "Culling ON for surplus tame boars." : "Culling off.");
            }

            SaveNow();
            return true;
        }

        public override IItemEndpoint Body => new StoreEndpoint(M, "culling gate", () => _store, SaveNow, s => false);
        public override List<ItemStack> Drain() { return _store.TakeAll(); }

        public override string Status()
        {
            return (_policy.Enabled ? "ON" : "OFF") + ". Keeps " + Reserves[_reserveIndex] + " adults. " + _reason
                + "\nNamed, young, and untamed animals are never taken. Species: " + string.Join(", ", CullingPolicy.ReviewedSpecies.ToArray())
                + "\nHeld: " + Describe(_store.Stacks) + "\nInteract: on/off. Shift+interact: reserve.";
        }
    }
}
