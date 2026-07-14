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

        private readonly Dictionary<int, Vector2> originalMobPositions = new Dictionary<int, Vector2>();
        private long lastAttackTime = 0;
        private readonly long attackCooldown = 100;

        private int lastMobId = -1;
        private int frozenMobX = 0;
        private int frozenMobY = 0;
        private int lastIndexFire = 0;

        // Tối ưu GC: Tái sử dụng collection tránh tạo mới mỗi frame
        private readonly HashSet<int> currentMobIds = new HashSet<int>();
        private readonly List<int> keysToRemove = new List<int>();

        // Cache PlayerPrefs tránh đọc đĩa I/O liên tục mỗi frame
        private int cachedBossTeleportId = -2;
        private int frameCountCheckBoss = 0;

        internal void setToggleTanSat()
        {
            isTanSat = !isTanSat;
            GameCanvas.Start_Normal_Only_CmdClose_DiaLog($"Tàn Sát: {(isTanSat ? T.on : T.off)}");

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
            GameCanvas.Start_Normal_Only_CmdClose_DiaLog($"Gom Quái: {(isGomQuai ? T.on : T.off)}");

            if (!isGomQuai)
            {
                RestoreOriginalPositions();
            }
        }

        internal new void update()
        {
            var p = GameScreen.player;
            if (p == null || p.Action == 4 || p.Hp <= 0) return;

            checkAndCacheBossTeleportStone();
            doGomQuai();
            doTanSat();
        }

        private void doTanSat()
        {
            if (!isTanSat) return;

            try
            {
                var p = GameScreen.player;
                if (p == null || p.Action == 4 || p.Hp <= 0) return;

                var target = FindNearestMob();
                if (target == null)
                {
                    Player.AutoFireCur = 0;
                    lastMobId = -1;
                    return;
                }

                if (GameScreen.objFocus != target)
                {
                    GameScreen.objFocus = target;
                    Interface_Game.isPaintInfoFocus = true;
                    if (!GameCanvas.isTouch)
                    {
                        GameCanvas.gameScr.center = target.getCenterCmd();
                    }
                }

                // Khóa vị trí quái vật mục tiêu tại chỗ để đánh chính xác
                if (target.ID != lastMobId)
                {
                    lastMobId = target.ID;
                    frozenMobX = target.x;
                    frozenMobY = target.y;
                }

                target.x = frozenMobX;
                target.y = frozenMobY;
                target.vx = 0;
                target.vy = 0;

                TeleportToTarget(target);
                AttackTarget(target);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void TeleportToTarget(MainObject target)
        {
            if (target == null) return;
            var p = GameScreen.player;
            if (p != null && (p.x != target.x || p.y != target.y))
            {
                p.x = target.x;
                p.y = target.y;
                p.posTransRoad = null;
                p.Action = 0;
                p.vx = 0;
                p.vy = 0;

                Player.xBeginAuto = p.x;
                Player.yBeginAuto = p.y;
                Player.isBack = false;

                GlobalService.gI().Obj_Move((short)target.x, (short)target.y);
            }
        }

        private void AttackTarget(MainObject target)
        {
            if (target == null) return;
            var p = GameScreen.player;
            if (p == null || mSystem.currentTimeMillis() - lastAttackTime < attackCooldown) return;

            p.posTransRoad = null;
            p.vx = 0;
            p.vy = 0;
            if (p.Action == 1) p.Action = 0;

            Player.xBeginAuto = p.x;
            Player.yBeginAuto = p.y;
            Player.isBack = false;

            // Kích hoạt skill phím nóng có cooldown sẵn sàng
            int num = Player.hotkeyPlayer[Player.currentTab].Length;
            bool skillFired = false;
            for (int i = 0; i < num; i++)
            {
                int index = (i + lastIndexFire) % num;
                var hotkey = Player.hotkeyPlayer[Player.currentTab][index];
                if (hotkey?.skill != null && DelaySkill.getDelay(hotkey.skill.indexHotKey).isCoolDown())
                {
                    var skill = Skill_Info.getSkillFromID(hotkey.skill.ID);
                    if (skill != null && skill.typeSkill != 2 && p.getManaNeedUse(skill.manaLost) <= p.Mp)
                    {
                        p.beginPlayerFire(index);
                        lastIndexFire = (index + 1) % num;
                        skillFired = true;
                        break;
                    }
                }
            }

            // Đánh thường nếu không có skill phím nóng nào sẵn sàng
            if (!skillFired)
            {
                if (Player.AutoFireCur == 0)
                {
                    Player.AutoFireCur = Player.typeAutoFireMain;
                    Player.xBeginAuto = p.x;
                    Player.yBeginAuto = p.y;
                }
                if (GameCanvas.isTouch)
                    target.setFireObject(2);
                else
                    p.setAutoFire(true);
            }

            lastAttackTime = mSystem.currentTimeMillis();
        }

        private void MoveToTarget(MainObject target)
        {
            if (target == null) return;
            var p = GameScreen.player;
            if (p == null) return;

            if (Player.AutoFireCur > 0) Player.AutoFireCur = 0;
            if (p.posTransRoad == null && p.Action != 1)
            {
                GlobalService.gI().Obj_Move((short)target.x, (short)target.y);
            }
        }

        private MainObject FindNearestMob()
        {
            var p = GameScreen.player;
            if (p == null || GameScreen.vecPlayers == null) return null;

            MainObject bestBoss = null;
            MainObject bestNormal = null;
            int highestBossHp = -1;
            int highestNormalHp = -1;

            // Gom 2 vòng lặp cũ thành 1 vòng lặp duy nhất để tối ưu hóa CPU 50%
            int size = GameScreen.vecPlayers.size();
            for (int i = 0; i < size; i++)
            {
                var mob = GameScreen.vecPlayers.elementAt(i) as MainObject;
                if (mob == null || mob == p || mob.typeObject != 1) continue;
                if (mob.Hp <= 0 || mob.isDie || mob.Action == 4 || mob.isRemove || mob.isStop) continue;
                if (!p.setFightPk(mob)) continue;

                bool isBoss = mob.typeBossMonster == 2 || mob.typeSpecMonSter == 1;
                if (isBoss)
                {
                    if (bestBoss == null || mob.maxHp > highestBossHp || (mob.maxHp == highestBossHp && mob.Hp > bestBoss.Hp))
                    {
                        highestBossHp = mob.maxHp;
                        bestBoss = mob;
                    }
                }
                else if (bestBoss == null) // Chỉ quét quái thường nếu chưa tìm thấy Boss nào để tiết kiệm tài nguyên
                {
                    if (bestNormal == null || mob.maxHp > highestNormalHp || (mob.maxHp == highestNormalHp && mob.Hp > bestNormal.Hp))
                    {
                        highestNormalHp = mob.maxHp;
                        bestNormal = mob;
                    }
                }
            }

            return bestBoss ?? bestNormal;
        }

        private void doGomQuai()
        {
            if (!isGomQuai) return;

            currentMobIds.Clear();
            int targetX = GameCanvas.loadmap.maxWMap >> 1;
            int targetY = GameCanvas.loadmap.maxHMap * 65 / 100;

            int size = GameScreen.vecPlayers.size();
            for (int i = 0; i < size; i++)
            {
                var mob = GameScreen.vecPlayers.elementAt(i) as MainObject;
                if (mob == null || mob.typeObject != 1) continue;

                currentMobIds.Add(mob.ID);
                if (mob.Hp <= 0 || mob.isDie || mob.isRemove) continue;

                if (!originalMobPositions.ContainsKey(mob.ID))
                {
                    originalMobPositions[mob.ID] = new Vector2(mob.x, mob.y);
                }

                mob.x = targetX;
                mob.y = targetY;
                mob.vx = 0;
                mob.vy = 0;
            }

            keysToRemove.Clear();
            foreach (var id in originalMobPositions.Keys)
            {
                if (!currentMobIds.Contains(id))
                {
                    keysToRemove.Add(id);
                }
            }
            int keysSize = keysToRemove.Count;
            for (int i = 0; i < keysSize; i++)
            {
                originalMobPositions.Remove(keysToRemove[i]);
            }
        }

        private void RestoreOriginalPositions()
        {
            int size = GameScreen.vecPlayers.size();
            for (int i = 0; i < size; i++)
            {
                var mob = GameScreen.vecPlayers.elementAt(i) as MainObject;
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
            if (GameScreen.vecPlayers == null) return;

            // Chỉ quét đá dịch chuyển mỗi 60 frame (1 giây) để giảm tải CPU
            frameCountCheckBoss++;
            if (frameCountCheckBoss < 60) return;
            frameCountCheckBoss = 0;

            if (cachedBossTeleportId == -2)
            {
                cachedBossTeleportId = PlayerPrefs.GetInt("boss_teleport_id", -1);
            }

            int size = GameScreen.vecPlayers.size();
            for (int i = 0; i < size; i++)
            {
                var mainObject = GameScreen.vecPlayers.elementAt(i) as MainObject;
                if (mainObject != null && mainObject.typeObject == 2)
                {
                    string name = mainObject.name;
                    // Lọc IndexOf nhanh trước khi gọi ToLower() để tránh rác bộ nhớ chuỗi
                    if (name != null && (name.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Boss", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        string lowerName = name.ToLower();
                        if (lowerName.Contains("boss") && (lowerName.Contains("dịch") || lowerName.Contains("dich") || lowerName.Contains("đá") || lowerName.Contains("da")))
                        {
                            if (cachedBossTeleportId != mainObject.ID)
                            {
                                cachedBossTeleportId = mainObject.ID;
                                PlayerPrefs.SetInt("boss_teleport_id", mainObject.ID);
                                PlayerPrefs.Save();
                            }
                            break;
                        }
                    }
                }
            }
        }

        internal void UseTeleportStone()
        {
            if (GameScreen.vecPlayers != null)
            {
                int size = GameScreen.vecPlayers.size();
                for (int i = 0; i < size; i++)
                {
                    var mainObject = GameScreen.vecPlayers.elementAt(i) as MainObject;
                    if (mainObject != null && mainObject.typeObject == 2)
                    {
                        string name = mainObject.name;
                        if (name != null && (name.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Boss", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            string lowerName = name.ToLower();
                            if (lowerName.Contains("boss") && (lowerName.Contains("dịch") || lowerName.Contains("dich") || lowerName.Contains("đá") || lowerName.Contains("da")))
                            {
                                Interface_Game.addInfoPlayerNormal($"Tương tác trực tiếp: {mainObject.name} (ID: {mainObject.ID})", mFont.tahoma_7_yellow);
                                mainObject.Giaotiep();
                                return;
                            }
                        }
                    }
                }
            }

            Interface_Game.addInfoPlayerNormal("Mở menu dịch chuyển từ xa...", mFont.tahoma_7_yellow);
            
            var menuItems = new mVector();
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