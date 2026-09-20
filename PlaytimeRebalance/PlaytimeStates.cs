using HarmonyLib;
using MTM101BaldAPI.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PlaytimeRebalance
{
    // data that needs to be carried across all playtime npc chase states
    // normally i'd use a component for this type of extra data
    // but considering none of my patches need to know about it, a class passed into the state makes the most sense
    public class PlaytimeChaseData
    {
        public PlaytimeChaseData(float time)
        {
            remainingTime = time;
        }

        public float remainingTime;
        public HashSet<NPC> homedNPCs = new HashSet<NPC>();
        public Dictionary<NPC, float> npcCooldowns = new Dictionary<NPC, float>();

        public void AddCooldown(NPC target, float time)
        {
            if (npcCooldowns.ContainsKey(target))
            {
                npcCooldowns[target] = time;
                return;
            }
            npcCooldowns.Add(target, time);
            toUpdate.Add(target,0f);
        }


        private List<NPC> toRemove = new List<NPC>();
        private Dictionary<NPC, float> toUpdate = new Dictionary<NPC, float>();
        public void UpdateCooldowns(float value)
        {
            toRemove.Clear();
            foreach (var kvp in npcCooldowns)
            {
                if (npcCooldowns[kvp.Key] < 0f)
                {
                    toRemove.Add(kvp.Key);
                }
                else
                {
                    toUpdate[kvp.Key] = npcCooldowns[kvp.Key] - value;
                }
            }
            foreach (var remove in toRemove)
            {
                npcCooldowns.Remove(remove);
                toUpdate.Remove(remove);
            }
            foreach (var kvp in toUpdate)
            {
                npcCooldowns[kvp.Key] = kvp.Value;
            }
        }
    }

    public class Playtime_NPCChaseBase : Playtime_StateBase
    {
        private static MovementModifier moveMod = new MovementModifier(Vector3.zero, 1.35f);
        protected float remainingChaseTime
        {
            get
            {
                return data.remainingTime;
            }
            set
            {
                data.remainingTime = value;
            }
        }

        public PlaytimeChaseData data;
        public Playtime_NPCChaseBase(NPC npc, Playtime playtime, PlaytimeChaseData data) : base(npc, playtime)
        {
            this.data = data;
        }

        public override void Enter()
        {
            base.Enter();
            playtime.Entity.ExternalActivity.moveMods.Add(moveMod);
        }

        public override void Exit()
        {
            playtime.Entity.ExternalActivity.moveMods.Remove(moveMod);
        }

        public override void Update()
        {
            base.Update();
            data.UpdateCooldowns(Time.deltaTime * npc.TimeScale);
        }

        public virtual bool NPCIsValidTarget(NPC potTarget)
        {
            if (potTarget == npc) return false; // cant target self
            if (potTarget == null) return false;
            if (potTarget.Character == Character.Null) return false;
            if (potTarget.Entity.Hidden) return false;
            // CanBeOverriden is implemented wrong
            //if (!potTarget.Entity.CanBeOverridden) return false;
            NPCMetadata meta = potTarget.GetMeta();
            if (meta == null) return false;
            if ((!meta.flags.HasFlag(NPCFlags.CanMove)) || !meta.flags.HasFlag(NPCFlags.HasTrigger) || !meta.flags.HasFlag(NPCFlags.HasSprite) || meta.tags.Contains("pltr_notarget")) return false;
            return true;
        }

        public void BecomeSadAndWander()
        {
            playtime.BecomeSad();
            playtime.behaviorStateMachine.ChangeState(new Playtime_ChaseNPCSadState(npc, playtime, data, playtime.InitialCooldown));
        }
    }

    public class Playtime_ChaseNPCSadState : Playtime_NPCChaseBase
    {
        static FieldInfo _animator = AccessTools.Field(typeof(Playtime), "animator");
        float time = 15f;
        public Playtime_ChaseNPCSadState(NPC npc, Playtime playtime, PlaytimeChaseData data, float time) : base(npc, playtime, data)
        {
            this.time = time;
        }

        public override void Enter()
        {
            base.Enter();
            ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
        }

        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
        }

        public override void Exit()
        {
            base.Exit();
            ((Animator)_animator.GetValue(playtime)).Play("PLAY_Jump");
        }

        public override void Update()
        {
            base.Update();
            remainingChaseTime -= Time.deltaTime * npc.TimeScale;
            time -= Time.deltaTime * npc.TimeScale;
            if (time <= 0f)
            {
                npc.behaviorStateMachine.ChangeState(new Playtime_ChaseNPCWanderState(npc, playtime, data));
                return;
            }
            if (remainingChaseTime <= 0f)
            {
                playtime.EndCooldown();
            }
        }
    }

    public class Playtime_ChaseNPCWanderState : Playtime_NPCChaseBase
    {
        private float homingTime = 10f;
        public NPC homingNpc;

        public Playtime_ChaseNPCWanderState(NPC npc, Playtime playtime, PlaytimeChaseData data, IntVector2? forbiddenCell = null) : base(npc, playtime, data)
        {
        }

        public void ResetHomingNPC(IntVector2? forbiddenCell = null)
        {
            homingNpc = null;
            int homingNPCTileCount = int.MaxValue;
            foreach (NPC potTarget in npc.ec.Npcs)
            {
                if (!NPCIsValidTarget(potTarget)) continue;
                if (data.homedNPCs.Contains(potTarget)) continue;
                if (data.npcCooldowns.ContainsKey(potTarget)) continue;
                npc.ec.FindPath(npc.ec.CellFromPosition(npc.transform.position), npc.ec.CellFromPosition(potTarget.transform.position), PathType.Nav, out List<Cell> path, out bool success);
                if (!success) continue;
                if (forbiddenCell != null)
                {
                    if (path.Find(x => x.position == forbiddenCell) != null) continue; // dont allow paths that cross over the forbidden cell
                }
                if (homingNpc == null)
                {
                    homingNpc = potTarget;
                    homingNPCTileCount = path.Count;
                }
                else if (path.Count < homingNPCTileCount)
                {
                    homingNPCTileCount = path.Count;
                    homingNpc = potTarget;
                }
            }
            if (homingNpc == null)
            {
                if (data.homedNPCs.Count > 0)
                {
                    data.homedNPCs.Clear();
                    ResetHomingNPC(); // dont pass in forbidden cell as this pretty much should always succeed.
                    return;
                }
                homingTime = 0f;
                return;
            }
            ChangeNavigationState(new NavigationState_TargetPosition(npc, 0, homingNpc.transform.position));
        }

        public override void Enter()
        {
            base.Enter();
            ResetHomingNPC();
        }

        public override void Update()
        {
            base.Update();
            if (!npc.Navigator.Entity.Blinded)
            {
                foreach (NPC potTarget in npc.ec.Npcs)
                {
                    if (!NPCIsValidTarget(potTarget)) continue;
                    if (data.npcCooldowns.ContainsKey(potTarget)) continue;
                    npc.looker.Raycast(potTarget.transform, Mathf.Min((potTarget.transform.position - npc.transform.position).magnitude + npc.Navigator.Velocity.magnitude, npc.looker.distance, npc.ec.MaxRaycast), 2326529, out bool targetSighted);
                    if (targetSighted)
                    {
                        data.homedNPCs.Add(potTarget);
                        npc.behaviorStateMachine.ChangeState(new Playtime_ChaseNPCState(npc, playtime, data, potTarget));
                        return;
                    }
                }
            }
            remainingChaseTime -= Time.deltaTime * npc.TimeScale;
            if (homingTime > 0)
            {
                if (homingNpc == null)
                {
                    ResetHomingNPC();
                    homingTime = 10f;
                }
                else
                {
                    playtime.behaviorStateMachine.CurrentNavigationState.UpdatePosition(homingNpc.transform.position);
                }
                homingTime -= Time.deltaTime * npc.TimeScale;
                if (homingTime <= 0f)
                {
                    ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
                    data.homedNPCs.Add(homingNpc);
                    homingNpc = null;
                }
            }
            if (remainingChaseTime <= 0f)
            {
                playtime.EndCooldown();
            }
        }

        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            homingTime = 0f;
            homingNpc = null;
            ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
        }
    }

    public class Playtime_ChaseNPCState : Playtime_NPCChaseBase
    {
        static FieldInfo _normSpeed = AccessTools.Field(typeof(Playtime), "normSpeed");
        static FieldInfo _runSpeed = AccessTools.Field(typeof(Playtime), "runSpeed");
        static FieldInfo _audMan = AccessTools.Field(typeof(Playtime), "audMan");
        static FieldInfo _audLetsPlay = AccessTools.Field(typeof(Playtime), "audLetsPlay");
        static FieldInfo _audGo = AccessTools.Field(typeof(Playtime), "audGo");

        bool alreadyHandled = false;

        NPC target;
        public Playtime_ChaseNPCState(NPC npc, Playtime playtime, PlaytimeChaseData data, NPC target) : base(npc, playtime, data)
        {
            this.target = target;
        }

        public override void Enter()
        {
            base.Enter();
            ChangeNavigationState(new NavigationState_TargetPosition(npc, 63, target.transform.position));
            float runSpeed = (float)_runSpeed.GetValue(playtime);
            npc.Navigator.maxSpeed = runSpeed;
            npc.Navigator.SetSpeed(runSpeed);

            AudioManager audMan = (AudioManager)_audMan.GetValue(playtime);
            if (audMan.audioSourceManager.isPlaying)
            {
                audMan.FlushQueue(true);
                audMan.PlaySingle((SoundObject)_audLetsPlay.GetValue(playtime));
            }
        }

        public override void Exit()
        {
            base.Exit();
            float walkSpeed = (float)_normSpeed.GetValue(playtime);
            npc.Navigator.maxSpeed = walkSpeed;
            npc.Navigator.SetSpeed(walkSpeed);
        }

        public override void OnStateTriggerEnter(Entity otherEntity, Collider other, bool validCollision)
        {
            base.OnStateTriggerEnter(otherEntity, other, validCollision);
            if (!otherEntity.CompareTag("NPC")) return; // dont care
            if (!validCollision)
            {
                // we only become sad if the person we fail to collide with is who we wanted to play with
                if (otherEntity == target.Entity)
                {
                    BecomeSadAndWander();
                }
                return;
            }
            if (!otherEntity.TryGetComponent(out NPC collidedNPC)) return; // wtf
            if (data.npcCooldowns.ContainsKey(collidedNPC)) return;
            if ((!NPCIsValidTarget(collidedNPC)) || !CanSeeNPC(collidedNPC))
            {
                data.AddCooldown(collidedNPC, 5f);
                npc.behaviorStateMachine.ChangeState(new Playtime_ChaseNPCWanderState(npc, playtime, data));
                return;
            }

            alreadyHandled = true;
            // play logic
            npc.Navigator.maxSpeed = 0f;
            npc.Entity.AddForce(new Force(npc.transform.position - otherEntity.transform.position, 20f, -60f));
            ((AudioManager)_audMan.GetValue(playtime)).PlaySingle((SoundObject)_audGo.GetValue(playtime));
            npc.behaviorStateMachine.ChangeState(new Playtime_NPCPlayState(npc, playtime, data, collidedNPC));
        }

        public override void OnStateTriggerStay(Entity otherEntity, Collider other, bool validCollision)
        {
            if (!alreadyHandled) return; // i think this is what is causing that weird force doubling
            OnStateTriggerEnter(otherEntity, other, validCollision);
        }


        public bool CanSeeNPC(NPC targ)
        {
            npc.looker.Raycast(targ.transform, Mathf.Min((targ.transform.position - npc.transform.position).magnitude + npc.Navigator.Velocity.magnitude, npc.looker.distance, npc.ec.MaxRaycast), 2326529, out bool targetSighted);
            return targetSighted;
        }

        public override void Update()
        {
            base.Update();
            remainingChaseTime = Mathf.Max(remainingChaseTime - (Time.deltaTime * npc.TimeScale), 5f);
            if (target == null)
            {
                npc.behaviorStateMachine.ChangeState(new Playtime_ChaseNPCWanderState(npc, playtime, data));
                return;
            }
            if (CanSeeNPC(target))
            {
                npc.behaviorStateMachine.CurrentNavigationState.UpdatePosition(target.transform.position);
            }
            else
            {
                npc.behaviorStateMachine.ChangeState(new Playtime_ChaseNPCWanderState(npc, playtime, data, IntVector2.GetGridPosition(target.transform.position)));
            }
        }

        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            npc.behaviorStateMachine.ChangeState(new Playtime_ChaseNPCWanderState(npc, playtime, data));
        }
    }

    // i port many things from JumpRope directly, even if unnecessary, incase mystman12 changes the code in the future.
    public class Playtime_NPCPlayState : Playtime_NPCChaseBase
    {
        int jumps = 0;
        int maxJumps;
        float ropeTime;
        float ropeDelay;
        float initVelocity;
        float accel;
        float height = 0f;
        float maxHeight = 0f;
        float jumpBuffer = 0.2f; // i could make this accurate, however I do not want to deal with NPCs "missing" jumps.

        MovementModifier moveMod = new MovementModifier(Vector3.zero, 0f);
        EntityOverrider overrider = new EntityOverrider();

        static FieldInfo _jumpropePre = AccessTools.Field(typeof(Playtime), "jumpropePre");
        static FieldInfo _maxJumps = AccessTools.Field(typeof(Jumprope), "maxJumps");
        static FieldInfo _ropeDelay = AccessTools.Field(typeof(Jumprope), "ropeDelay");
        static FieldInfo _ropeTime = AccessTools.Field(typeof(Jumprope), "ropeTime");
        static FieldInfo _initVelocity = AccessTools.Field(typeof(Jumprope), "initVelocity");
        static FieldInfo _accel = AccessTools.Field(typeof(Jumprope), "accel");

        static FieldInfo _audMan = AccessTools.Field(typeof(Playtime), "audMan");
        static FieldInfo _audCongrats = AccessTools.Field(typeof(Playtime), "audCongrats");

        NPC target;

        List<IEnumerator> numerators = new List<IEnumerator>(); // we dont want to use real enumerators because if a statechange cleanup is minorly annoying

        public Playtime_NPCPlayState(NPC npc, Playtime playtime, PlaytimeChaseData data, NPC target) : base(npc, playtime, data)
        {
            this.target = target;
            Jumprope jumpropePre = (Jumprope)_jumpropePre.GetValue(playtime);
            maxJumps = (int)_maxJumps.GetValue(jumpropePre);
            ropeTime = (float)_ropeTime.GetValue(jumpropePre);
            ropeDelay = (float)_ropeDelay.GetValue(jumpropePre);
            initVelocity = (float)_initVelocity.GetValue(jumpropePre);
            accel = (float)_accel.GetValue(jumpropePre);
        }

        public void ClearCooldownsIfWillBecomeFull()
        {
            int cooldowned = 0;
            int total = 0;
            foreach (NPC npc in npc.ec.Npcs)
            {
                if (!NPCIsValidTarget(npc)) continue;
                total++;
                if (data.npcCooldowns.ContainsKey(npc))
                {
                    cooldowned++;
                }
            }
            if ((cooldowned + 1) >= total)
            {
                data.npcCooldowns.Clear();
            }
        }

        public void StartCoroutine(IEnumerator numerator)
        {
            numerators.Add(numerator);
        }

        public override void Enter()
        {
            base.Enter();
            npc.Navigator.maxSpeed = 0f;
            target.Entity.ExternalActivity.moveMods.Add(moveMod);
            if (!target.Entity.Override(overrider))
            {
                overrider = null;
                ClearCooldownsIfWillBecomeFull();
                data.AddCooldown(target, playtime.InitialCooldown);
                BecomeSadAndWander();
                return;
            }
            StartCoroutine(RopeTimer());
        }

        static FieldInfo _normSpeed = AccessTools.Field(typeof(Playtime), "normSpeed");
        public override void Exit()
        {
            base.Exit();
            target.Entity.ExternalActivity.moveMods.Remove(moveMod);
            float walkSpeed = (float)_normSpeed.GetValue(playtime);
            npc.Navigator.maxSpeed = walkSpeed;
            npc.Navigator.SetSpeed(walkSpeed);
            if (overrider != null)
            {
                overrider.SetHeight(5f);
                overrider.ReleaseHeight();
                target.Entity.Release();
            }
        }

        public override void Update()
        {
            base.Update();
            if (target == null)
            {
                End(false);
            }
            remainingChaseTime = Mathf.Max(remainingChaseTime - (Time.deltaTime * npc.TimeScale), 5f);
            if (!CanSeeNPC(target))
            {
                End(false);
                return;
            }
            for (int i = (numerators.Count - 1); i >= 0; i--)
            {
                if (!numerators[i].MoveNext())
                {
                    numerators.RemoveAt(i);
                }
            }
            overrider.SetHeight(5f + height);
        }

        private IEnumerator Jump()
        {
            float velocity = initVelocity;
            moveMod.movementMultiplier = 0.25f;
            while (height >= 0f)
            {
                height += velocity * Time.deltaTime + 0.5f * accel * Time.deltaTime * Time.deltaTime;
                velocity += accel * Time.deltaTime;
                if (height > maxHeight)
                {
                    maxHeight = height;
                }
                if (height <= 0f)
                {
                    break;
                }
                yield return null;
            }
            height = 0f;
            moveMod.movementMultiplier = 0f;
            yield break;
        }

        private IEnumerator RopeTimer()
        {
            while (jumps < maxJumps)
            {
                float delay = ropeDelay;
                while (delay > 0f)
                {
                    delay -= Time.deltaTime;
                    yield return null;
                }
                float hitTime = ropeTime;
                float jumpTime = UnityEngine.Random.Range(0.5f, 1.25f);
                while (hitTime > 0f)
                {
                    if ((hitTime < jumpTime) && (height <= 0f))
                    {
                        StartCoroutine(Jump());
                    }
                    hitTime -= Time.deltaTime;
                    yield return null;
                }
                RopeDown();
            }
            while (height > 0f)
            {
                yield return null;
            }
            End(true);
            yield break;
        }

        public bool CanSeeNPC(NPC targ)
        {
            npc.looker.Raycast(targ.transform, Mathf.Min((targ.transform.position - npc.transform.position).magnitude + npc.Navigator.Velocity.magnitude, npc.looker.distance, npc.ec.MaxRaycast), 2326529, out bool targetSighted);
            return targetSighted;
        }

        void End(bool success)
        {
            if (success)
            {
                ClearCooldownsIfWillBecomeFull();
                data.AddCooldown(target, playtime.InitialCooldown);
                npc.behaviorStateMachine.ChangeState(new Playtime_ChaseNPCWanderState(npc, playtime, data));
                ((AudioManager)_audMan.GetValue(playtime)).PlaySingle((SoundObject)_audCongrats.GetValue(playtime));
                return;
            }

            BecomeSadAndWander();
        }

        private void RopeDown()
        {
            if (height > jumpBuffer)
            {
                jumps++;
                playtime.Count(jumps);
            }
            else
            {
                jumps = 0;
                playtime.JumpropeHit();
            }
        }
    }
}
