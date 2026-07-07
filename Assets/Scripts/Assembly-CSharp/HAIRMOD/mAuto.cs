using System;
using System.Collections.Generic;
using UnityEngine;

namespace HAIRMOD
{
    internal class mAuto : AvMain
    {
        private static mAuto _Instance;
        internal static mAuto Instance => _Instance ??= new mAuto();
        internal bool isTanSat { get; set; }
        internal bool isGomQuai { get; set; }
        private Dictionary<int, Vector2> originalMobPositions = new Dictionary<int, Vector2>();
        private long lastAttackTime = 0;
        private long attackCooldown = 100;

        private int lastMobId = -1;
        private int frozenMobX = 0;
        private int frozenMobY = 0;
        private int lastIndexFire = 0;

        internal void setToggleTanSat()
        {
            isTanSat = !isTanSat;
            GameCanvas.Start_Normal_Only_CmdClose_DiaLog("T\u00e0n S\u00e1t: " + (isTanSat ? T.on : T.off));

            if (isTanSat)
            {
                Player.AutoFireCur = 1;
                Player.setStart_EndAutoFire(true);
            }
            else
            {
                Player.AutoFireCur = 0;
                Player.setStart_EndAutoFire(false);
                GameScreen.objFocus = null;
                lastMobId = -1;
            }
        }

        internal void setToggleGomQuai()
        {
            isGomQuai = !isGomQuai;
            GameCanvas.Start_Normal_Only_CmdClose_DiaLog("Gom Qu\u00e1i: " + (isGomQuai ? T.on : T.off));

            if (!isGomQuai)
            {
                RestoreOriginalPositions();
            }
        } 
        internal new void update()
        {
            if (GameScreen.player == null || GameScreen.player.Action == 4 || GameScreen.player.Hp <= 0)
                return;
            checkAndCacheBossTeleportStone();
            doGomQuai();
            doTanSat();
        } 
      

        private void doTanSat()
        {
            if (!isTanSat) return;

            try
            {
                var player = GameScreen.player;
                if (player.Action == 4 || player.Hp <= 0)
                    return;
                var nearestMob = FindNearestMob();

                if (nearestMob == null)
                {
                    Player.AutoFireCur = 0;
                    lastMobId = -1;
                    return;
                }
                if (GameScreen.objFocus != nearestMob)
                {
                    GameScreen.objFocus = nearestMob;
                    Interface_Game.isPaintInfoFocus = true;
                    if (!GameCanvas.isTouch)
                    {
                        GameCanvas.gameScr.center = GameScreen.objFocus.getCenterCmd();
                    }
                }

                // Freeze targeted monster at its original position
                if (nearestMob.ID != lastMobId)
                {
                    lastMobId = nearestMob.ID;
                    frozenMobX = nearestMob.x;
                    frozenMobY = nearestMob.y;
                }

                nearestMob.x = frozenMobX;
                nearestMob.y = frozenMobY;
                nearestMob.vx = 0;
                nearestMob.vy = 0;

                // Instant teleport player to the target monster
                TeleportToTarget(nearestMob);

                // Perform attack
                AttackTarget(nearestMob);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void TeleportToTarget(MainObject target)
        {
            if (target == null) return;
            var player = GameScreen.player;
            if (player.x != target.x || player.y != target.y)
            {
                player.x = target.x;
                player.y = target.y;
                player.posTransRoad = null;
                player.Action = 0;
                player.vx = 0;
                player.vy = 0;

                Player.xBeginAuto = player.x;
                Player.yBeginAuto = player.y;
                Player.isBack = false;

                GlobalService.gI().Obj_Move((short)target.x, (short)target.y);
            }
        }

        private void AttackTarget(MainObject target)
        {
            if (target == null) return;

            var player = GameScreen.player;
            if (mSystem.currentTimeMillis() - lastAttackTime < attackCooldown)
                return;

            player.posTransRoad = null;
            player.vx = 0;
            player.vy = 0;
            if (player.Action == 1)
            {
                player.Action = 0;
            }

            Player.xBeginAuto = player.x;
            Player.yBeginAuto = player.y;
            Player.isBack = false;

            // Cycle through all player's hotkeyed skills and activate the first one with cooldown ready
            int num = Player.hotkeyPlayer[Player.currentTab].Length;
            bool skillFired = false;
            for (int i = 0; i < num; i++)
            {
                int index = (i + lastIndexFire) % num;
                Hotkey hotkey = Player.hotkeyPlayer[Player.currentTab][index];
                if (hotkey != null && hotkey.skill != null)
                {
                    if (DelaySkill.getDelay(hotkey.skill.indexHotKey).isCoolDown())
                    {
                        Skill_Info skillFromID = Skill_Info.getSkillFromID(hotkey.skill.ID);
                        if (skillFromID != null && skillFromID.typeSkill != 2 && player.getManaNeedUse(skillFromID.manaLost) <= player.Mp)
                        {
                            player.beginPlayerFire(index);
                            lastIndexFire = (index + 1) % num;
                            skillFired = true;
                            break;
                        }
                    }
                }
            }

            // Fallback to auto-fire default skill if no hotkeyed skill is ready
            if (!skillFired)
            {
                if (Player.AutoFireCur == 0)
                {
                    Player.AutoFireCur = Player.typeAutoFireMain;
                    Player.xBeginAuto = player.x;
                    Player.yBeginAuto = player.y;
                }
                if (GameCanvas.isTouch)
                {
                    target.setFireObject(2);
                }
                else
                {
                    player.setAutoFire(true);
                }
            }

            lastAttackTime = mSystem.currentTimeMillis();
        }

        private void MoveToTarget(MainObject target)
        {
            if (target == null) return;

            var player = GameScreen.player;
            if (Player.AutoFireCur > 0)
            {
                Player.AutoFireCur = 0;
            }
            if (player.posTransRoad == null && player.Action != 1)
            {
                GlobalService.gI().Obj_Move((short)target.x, (short)target.y);
            }
        }

        private MainObject FindNearestMob()
        {
            MainObject nearest = null;
            int minDist = int.MaxValue;
            var player = GameScreen.player;

            for (int i = 0; i < GameScreen.vecPlayers.size(); i++)
            {
                MainObject mob = (MainObject)GameScreen.vecPlayers.elementAt(i);

                if (mob == null || mob == player) continue;
                if (mob.typeObject != 1) continue;
                if (mob.Hp <= 0 || mob.isDie || mob.Action == 4) continue;
                if (mob.isRemove || mob.isStop) continue;
                if (!player.setFightPk(mob)) continue;
                
                int dist = MainObject.getDistance(player.x, player.y, mob.x, mob.y);

                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = mob;
                }
            }
            if (nearest != null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[TanSat] Found target: ID={nearest.ID}, HP={nearest.Hp}/{nearest.maxHp}, Distance={minDist}");
#endif
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[TanSat] No valid target found");
#endif
            }

            return nearest;
        }



        private void doGomQuai()
        {
            if (!isGomQuai) return;

            var player = GameScreen.player;
            HashSet<int> currentMobIds = new HashSet<int>();

            int targetX = GameCanvas.loadmap.maxWMap >> 1;
            int targetY = GameCanvas.loadmap.maxHMap * 65 / 100;

            for (int i = 0; i < GameScreen.vecPlayers.size(); i++)
            {
                MainObject mob = (MainObject)GameScreen.vecPlayers.elementAt(i);

                if (mob == null || mob.typeObject != 1)
                    continue;

                currentMobIds.Add(mob.ID);

                if (mob.Hp <= 0 || mob.isDie || mob.isRemove)
                    continue;

                if (!originalMobPositions.ContainsKey(mob.ID))
                {
                    originalMobPositions[mob.ID] = new Vector2(mob.x, mob.y);
                }

                mob.x = targetX;
                mob.y = targetY;
                mob.vx = 0;
                mob.vy = 0;
            }

            // Clean up original positions for mobs that are no longer on the map (avoid memory leak)
            List<int> keysToRemove = new List<int>();
            foreach (var id in originalMobPositions.Keys)
            {
                if (!currentMobIds.Contains(id))
                {
                    keysToRemove.Add(id);
                }
            }
            foreach (var id in keysToRemove)
            {
                originalMobPositions.Remove(id);
            }
        }

        private void RestoreOriginalPositions()
        {
            for (int i = 0; i < GameScreen.vecPlayers.size(); i++)
            {
                MainObject mob = (MainObject)GameScreen.vecPlayers.elementAt(i);

                if (mob != null && mob.typeObject == 1 && originalMobPositions.TryGetValue(mob.ID, out var originalPos))
                {
                    mob.x = (int)originalPos.x;
                    mob.y = (int)originalPos.y;
                }
            }

            originalMobPositions.Clear();
        }

        private void checkAndCacheBossTeleportStone()
        {
            if (GameScreen.vecPlayers == null)
            {
                return;
            }
            for (int i = 0; i < GameScreen.vecPlayers.size(); i++)
            {
                MainObject mainObject = (MainObject)GameScreen.vecPlayers.elementAt(i);
                if (mainObject != null && mainObject.typeObject == 2)
                {
                    string text = mainObject.name.ToLower();
                    if (text.Contains("boss") && (text.Contains("dịch") || text.Contains("dich") || text.Contains("đá") || text.Contains("da")))
                    {
                        int cachedId = PlayerPrefs.GetInt("boss_teleport_id", -1);
                        if (cachedId != mainObject.ID)
                        {
                            PlayerPrefs.SetInt("boss_teleport_id", mainObject.ID);
                            PlayerPrefs.Save();
                        }
                        break;
                    }
                }
            }
        }

        internal void UseTeleportStone()
        {
            // 1. Try to find NPC in the current map first to let server handle it locally
            if (GameScreen.vecPlayers != null)
            {
                for (int i = 0; i < GameScreen.vecPlayers.size(); i++)
                {
                    MainObject mainObject = (MainObject)GameScreen.vecPlayers.elementAt(i);
                    if (mainObject != null && mainObject.typeObject == 2)
                    {
                        string text = mainObject.name.ToLower();
                        if (text.Contains("boss") && (text.Contains("dịch") || text.Contains("dich") || text.Contains("đá") || text.Contains("da")))
                        {
                            Interface_Game.addInfoPlayerNormal("Tương tác trực tiếp: " + mainObject.name + " (ID: " + mainObject.ID + ")", mFont.tahoma_7_yellow);
                            mainObject.Giaotiep();
                            return;
                        }
                    }
                }
            }

            // 2. If not in the current map, the server rejects direct NPC shop interactions.
            // We bypass this by opening the dynamic menu locally, which triggers the server's
            // process_menu callback (code -20251) which has no map proximity validation!
            Interface_Game.addInfoPlayerNormal("Mở menu dịch chuyển từ xa...", mFont.tahoma_7_yellow);
            
            mVector menuItems = new mVector();
            menuItems.addElement(new iCommand("Bãi biển hoang sơ (Boss Ruby)", null));
            menuItems.addElement(new iCommand("Bản đồ Train Extol", null));
            menuItems.addElement(new iCommand("Chopper Nổi Loạn (Boss)", null));
            menuItems.addElement(new iCommand("Chúa Tể Bóng Đêm (Boss)", null));
            menuItems.addElement(new iCommand("Thất Vũ Hải Jinbe (Boss)", null));
            menuItems.addElement(new iCommand("Siêu trùm (Boss)", null));
            menuItems.addElement(new iCommand("Thông tin boss", null));

            GameCanvas.menu.setinfoDynamic(menuItems, 2, 0, -20251, "Dịch Chuyển Boss");
        }

        public override void commandPointer(int index, int subIndex)
        {
            switch (index)
            {
                case 0:
                    setToggleTanSat();
                    break;
                case 1:
                    setToggleGomQuai();
                    break;
            }
            base.commandPointer(index, subIndex);
        }
    }
}