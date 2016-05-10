using System;
using System.Linq;
using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace RoyalSongOfSona
{
    class Program
    {
        private static Spell.Active Q, W, E;
        private static Spell.Skillshot R;
        private static Menu menu;
        private static string LastCastSpell = "";
        //private static bool packets { get { return menu.Item("packets").GetValue<bool>(); } }
        private static List<BuffType> CcTypes = new List<BuffType> { BuffType.Fear, BuffType.Polymorph, BuffType.Snare, BuffType.Stun, BuffType.Taunt, BuffType.Charm };

        #region Menu
        private static CheckBox _ComboQ = new CheckBox("Use Q in combo");
        private static Slider _ComboQSlider = new Slider("Use Q on range", 850, 600, 850);
        private static CheckBox _ComboW = new CheckBox("Use W in combo");
        private static CheckBox _ComboE = new CheckBox("Use E in combo");
        private static CheckBox _ComboR = new CheckBox("Use R in combo");
        private static Slider _ComboRSlider = new Slider("Ultimate if hit № enemies", 2, 1, 5);

        private static CheckBox _HarassQ = new CheckBox("Use Q in harass");
        private static CheckBox _HarassQProof = new CheckBox("Use Q only if hit 2 enemies");
        private static CheckBox _HarassW = new CheckBox("Use W in harass");

        private static Slider _HealPercentage = new Slider("Heal if ally with hp < x%", 60, 5, 100);
        private static Slider _HealCount = new Slider("Heal if № of allies in range", 1, 0, 4);
        private static CheckBox _HealMyself = new CheckBox("Heal yourself anyway");
        private static CheckBox _HealMode = new CheckBox("<--- ON: Fill my HP | Same rules as for allies :OFF");

        private static CheckBox _MiscGapclose = new CheckBox("Auto E on enemy gapclose", false);
        private static CheckBox _MiscInterrupt = new CheckBox("Smart interrupter");
        private static CheckBox _MiscAA = new CheckBox("AA minions only when no allies nearby");
        private static CheckBox _MiscExhaust = new CheckBox("Exhaust if not possible to inperrupt");
        private static CheckBox _MiscCleanse = new CheckBox("Use Mikaels on allies (ADC prioritizing)");
        private static CheckBox _MiscMinionAA = new CheckBox("Don't CS if has passive");
        private static KeyBind _MiscUlti = new KeyBind("Panic ultimate", false, KeyBind.BindTypes.HoldActive, 'T');


        private static CheckBox _DrawingQ = new CheckBox("Draw Q range");
        private static CheckBox _DrawingW = new CheckBox("Draw W range");
        private static CheckBox _DrawingE = new CheckBox("Draw E range");
        private static CheckBox _DrawingR = new CheckBox("Draw R range");
        #endregion

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.Instance.ChampionName != "Sona") return;

            Q = new Spell.Active(SpellSlot.Q, 850);
            W = new Spell.Active(SpellSlot.W, 1000);
            E = new Spell.Active(SpellSlot.E, 350);
            R = new Spell.Skillshot(SpellSlot.R, 1000, EloBuddy.SDK.Enumerations.SkillShotType.Linear, 500, 125, 3000);
            R.AllowedCollisionCount = 999;
            LoadMenu();
            //Game.OnGameSendPacket += OnSendPacket;
            Game.OnUpdate += Game_OnGameUpdate;
            Gapcloser.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnInterruptableSpell += Interrupter_OnPossibleToInterrupt;
            Drawing.OnDraw += OnDraw;
            Orbwalker.OnPreAttack += BeforeAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Chat.Print("RoyalSongOfSona loaded!");
        }

        static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe) LastCastSpell = args.SData.Name;
        }

        static void OnDraw(EventArgs args)
        {
            if (_DrawingQ.CurrentValue)
                Circle.Draw(Color.Cyan, _ComboQSlider.CurrentValue, Player.Instance.Position);
            if (_DrawingW.CurrentValue)
                Circle.Draw(Color.ForestGreen, W.Range, Player.Instance.Position);
            if (_DrawingE.CurrentValue)
                Circle.Draw(Color.DeepPink, E.Range, Player.Instance.Position);
            if (_DrawingR.CurrentValue)
                Circle.Draw(Color.Gold, R.Range, Player.Instance.Position);
        }

        static void BeforeAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (args.Target.Type == GameObjectType.obj_AI_Minion && (_MiscAA.CurrentValue && AlliesInRange(1500) > 0 || _MiscMinionAA.CurrentValue && Player.HasBuff("sonapassiveattack")))
                args.Process = false;
            if (Player.HasBuff("sonapassiveattack") && (Q.IsReady(1500) || LastCastSpell != "SonaQ"))
                args.Process = false;
        }

        static void AntiGapcloser_OnEnemyGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (_MiscGapclose.CurrentValue && E.IsReady())
                E.Cast();
        }

        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (!sender.IsValid || sender.IsDead || !sender.IsTargetable || sender.IsStunned) return;
            if (R.IsReady() && R.IsInRange(sender.Position) && e.DangerLevel == EloBuddy.SDK.Enumerations.DangerLevel.High)
            {
                R.Cast(sender.Position);
                return;
            }
            else
            {
                if (!_MiscExhaust.CurrentValue) return;
                if (sender.Distance(Player.Instance.Position) > 600) return;
                if (Player.Instance.GetSpellSlotFromName("SummonerExhaust") != SpellSlot.Unknown && Player.Instance.Spellbook.CanUseSpell(Player.Instance.GetSpellSlotFromName("SummonerExhaust")) == SpellState.Ready)
                    Player.Instance.Spellbook.CastSpell(Player.Instance.GetSpellSlotFromName("SummonerExhaust"), sender);
                if ((W.IsReady() && GetPassiveCount() == 2) || (Player.Instance.HasBuff("sonapassiveattack") && LastCastSpell == "SonaW") || (Player.Instance.HasBuff("sonapassiveattack") && W.IsReady()))
                {
                    if (W.IsReady()) W.Cast();
                    Player.IssueOrder(GameObjectOrder.AttackUnit, sender);
                }
            }
        }

        static int GetPassiveCount()
        {
            foreach (BuffInstance buff in Player.Instance.Buffs)
                if (buff.Name == "sonapassivecount") return buff.Count;
            return 0;
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            if (_MiscUlti.CurrentValue)
                R.Cast(TargetSelector.GetTarget(400, DamageType.Magical).ServerPosition);

            if (_MiscCleanse.CurrentValue)
                CCRemove();

            // Combo
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
                Combo();

            // Harass
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass)
                Harass();
        }
        
        static void Combo()
        {
            bool useQ = Q.IsReady() && _ComboQ.CurrentValue;
            bool useW = W.IsReady() && _ComboW.CurrentValue;
            bool useE = E.IsReady() && _ComboE.CurrentValue;
            bool useR = R.IsReady() && _ComboR.CurrentValue;

            AIHeroClient targetQ = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            AIHeroClient targetR = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            
            if (targetR != null)
                foreach (var item in Player.Instance.InventoryItems)
                    if (item.Id == ItemId.Frost_Queens_Claim && Player.Instance.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                        Player.Instance.Spellbook.CastSpell(item.SpellSlot);

            if (useQ && targetQ != null && Vector3.Distance(Player.Instance.Position, targetQ.Position) < _ComboQSlider.CurrentValue)
                Q.Cast();

            if (useW)
                UseWSmart();

            if (useE)
                UseESmart(TargetSelector.GetTarget(1700, DamageType.Magical));
            if (useR && targetR != null)
            {
                var pred = R.GetPrediction(targetR);
                if (pred.GetCollisionObjects<AIHeroClient>().Count()+1 < _ComboRSlider.CurrentValue || pred.HitChance != EloBuddy.SDK.Enumerations.HitChance.High) return;
                R.Cast(pred.CastPosition);
            }
        }

        static void Harass()
        {
            bool useQ = Q.IsReady() && _HarassQ.CurrentValue;
            bool useW = W.IsReady() && _HarassW.CurrentValue;
            AIHeroClient targetQ = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (useQ && targetQ != null && (CountEnemiesInRange(Q.Range) > 1 || !_HarassQProof.CurrentValue))
                Q.Cast();

            if (useW)
                UseWSmart();
        }

        static void UseWSmart()
        {
            var count = _HealCount.CurrentValue;
            var percent = _HealPercentage.CurrentValue;

            double wHeal = (10 + 20 * W.Level + .2 * Player.Instance.FlatMagicDamageMod) * (1 + (Player.Instance.Health / Player.Instance.MaxHealth) / 2);

            AIHeroClient ally = MostWoundedAllyInRange(W.Range);
            int allies = AlliesInRange(W.Range);

            if (allies >= count && (ally.Health * 100 / ally.MaxHealth) <= percent)
                W.Cast();

            if (_HealMyself.CurrentValue)
            {
                if (_HealMode.CurrentValue && Player.Instance.MaxHealth - Player.Instance.Health > wHeal)
                    W.Cast();
                else if (allies >= count && (Player.Instance.Health / Player.Instance.MaxHealth) * 100 <= percent) 
                    W.Cast();
            }
        }

        //Ty DETUKS, copypasted as fuck :P
        public static void UseESmart(Obj_AI_Base target)
        {
            try
            {

                if (target.Path.Length == 0 || !target.IsMoving)
                    return;
                Vector2 nextEnemPath = target.Path[0].To2D();
                var dist = Player.Instance.Position.To2D().Distance(target.Position.To2D());
                var distToNext = nextEnemPath.Distance(Player.Instance.Position.To2D());
                if (distToNext <= dist)
                    return;
                var msDif = Player.Instance.MoveSpeed - target.MoveSpeed;
                if (msDif <= 0 && !Player.Instance.IsInAutoAttackRange(target))
                    E.Cast();

                var reachIn = dist / msDif;
                if (reachIn > 4)
                    E.Cast();
            }
            catch { }

        }

        static AIHeroClient MostWoundedAllyInRange(float range)
        {
            return ObjectManager.Get<AIHeroClient>().Where(x => x.IsAlly && !x.IsDead && !x.IsMe).OrderBy(x => x.Health).FirstOrDefault();
        }

        static void CCRemove()
        {
            //The best way to do it it's LINQ...
            //Realization taken from h3h3's Support AIO
            if (!Item.HasItem(ItemId.Mikaels_Crucible, Player.Instance) || !Item.CanUseItem((int)ItemId.Mikaels_Crucible) || CountEnemiesInRange(1000) < 1) return;
            foreach (var hero in ObjectManager.Get<AIHeroClient>().Where(h => h.IsAlly && !h.IsDead && Vector3.Distance(Player.Instance.Position, h.Position) <= 800).OrderByDescending(h => h.FlatPhysicalDamageMod))
                foreach (var buff in CcTypes)
                    if (hero.HasBuffOfType(buff))
                        Item.UseItem((int)ItemId.Mikaels_Crucible, hero);
        }

        static int AlliesInRange(float range)
        {
            int count = 0;
            foreach(AIHeroClient hero in ObjectManager.Get<AIHeroClient>())
                if (hero.IsAlly && !hero.IsDead && !hero.IsMe && Vector3.Distance(Player.Instance.Position, hero.Position) <= range) count++;
            return count;
        }

        static int CountEnemiesInRange(float range)
        {
            int count = 0;
            foreach (AIHeroClient hero in ObjectManager.Get<AIHeroClient>())
                if (hero.IsEnemy && !hero.IsDead && Vector3.Distance(Player.Instance.Position, hero.Position) <= range) count++;
            return count;
        }
                
        static void LoadMenu()
        {
            // Initialize the menu
            menu = MainMenu.AddMenu("Royal Song of Sona", "Royal_Song_of_Sona");

            // Combo
            Menu combo = menu.AddSubMenu("Combo Settings");
            combo.Add("_ComboQ", _ComboQ);
            combo.Add("_ComboQSlider", _ComboQSlider);
            combo.AddSeparator();
            combo.Add("_ComboW", _ComboW);
            combo.Add("_ComboE", _ComboE);
            combo.AddSeparator();
            combo.Add("_ComboR", _ComboR);
            combo.Add("_ComboRSlider", _ComboRSlider);

            Menu harass = menu.AddSubMenu("Harass Settings");
            harass.Add("_HarassQ", _HarassQ);
            harass.Add("_HarassQProof", _HarassQProof);
            harass.Add("_HarassW", _HarassW);

            Menu heal = menu.AddSubMenu("Healing Settings");
            heal.Add("_HealPercentage", _HealPercentage);
            heal.Add("_HealCount", _HealCount);
            heal.Add("_HealMyself", _HealMyself);
            heal.Add("_HealMode", _HealMode);

            Menu misc = menu.AddSubMenu("Miscellaneous Settings");
            misc.Add("_MiscGapclose", _MiscGapclose);
            misc.Add("_MiscInterrupt", _MiscInterrupt);
            misc.Add("_MiscAA", _MiscAA);
            misc.Add("_MiscMinionAA", _MiscMinionAA);
            misc.Add("_MiscExhaust", _MiscExhaust);
            misc.Add("_MiscCleanse", _MiscCleanse);
            misc.Add("_MiscUlti", _MiscUlti);

            Menu draw = menu.AddSubMenu("Drawing Settings");
            draw.Add("_DrawingQ", _DrawingQ);
            draw.Add("_DrawingW", _DrawingW);
            draw.Add("_DrawingE", _DrawingE);
            draw.Add("_DrawingR", _DrawingR);
        }
    }
}
