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
            doGomQuai();
            doTanSat();
        } 
      

        private void doTanSat()
        {
            if (!isTanSat) return;

            try
            {
                var player = GameScreen.player;
                if (player.Action == 4 || player.Hp <= 0 || player.skillCurrent != null)
                    return;
                var nearestMob = FindNearestMob();

                if (nearestMob == null)
                {
                    Player.AutoFireCur = 0;
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
                if (isGomQuai)
                {
                    AttackTarget(nearestMob);
                }
                else
                {
                    int distance = MainObject.getDistance(player.x, player.y, nearestMob.x, nearestMob.y);

                    if (distance > Player.wFocus - 40)
                    {
                        MoveToTarget(nearestMob);
                    }
                    else
                    {
                        AttackTarget(nearestMob);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void AttackTarget(MainObject target)
        {
            if (target == null) return;

            var player = GameScreen.player;
            if (mSystem.currentTimeMillis() - lastAttackTime < attackCooldown / 10)
                return;
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
                if (dist > Player.wFocus * 3) continue;

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

                mob.x = GameCanvas.loadmap.maxWMap >> 1;
                mob.y = GameCanvas.loadmap.maxHMap >> 1;
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