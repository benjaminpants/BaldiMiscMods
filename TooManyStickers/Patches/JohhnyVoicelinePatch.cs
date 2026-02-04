using HarmonyLib;
using MTM101BaldAPI.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(StoreRoomFunction))]
    [HarmonyPatch("OnPlayerExit")]
    class JohhnyVoicelinePatch
    {
        private static bool ShouldLockdown()
        {
            if (Singleton<StickerManager>.Instance == null) return false;
            for (int i = 0; i < Singleton<StickerManager>.Instance.stickerInventory.Count; i++)
            {
                StickerMetaData meta = Singleton<StickerManager>.Instance.stickerInventory[i].GetMeta();
                if (meta.tags.Contains("tms_daredevil") && !meta.tags.Contains("tms_daredevil_allowleave"))
                {
                    return true;
                }
            }
            return false;
        }

        static bool Prefix(StoreRoomFunction __instance, RoomController ___room, Transform ___johnnyBase, ref int ___totalCustomers, PlayerManager player)
        {
            if (ShouldLockdown())
            {
                ___totalCustomers--;
                player.Teleport(___room.RandomEntitySafeCellNoGarbage().CenterWorldPosition);
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(TooManyStickersPlugin.Instance.assetMan.Get<SoundObject>("TeleportSound"));
                JohnnyTeleportAnimator anim = null;
                if (!___johnnyBase.gameObject.TryGetComponent(out anim))
                {
                    anim = ___johnnyBase.gameObject.AddComponent<JohnnyTeleportAnimator>();
                }
                anim.PressButton();
                anim.TurnPlayer(player);
                return false;
            }
            return true;
        }
    }
}

namespace TooManyStickers
{
    public class JohnnyTeleportAnimator : MonoBehaviour
    {
        Sprite originalSprite;
        SpriteRenderer renderer;
        SpriteRenderer mouth;

        int pressFrame = 2;
        float pressNextTime = float.MaxValue;

        float[] buttonPressAnimTime = new float[]
        {
            0.5f,
            2f,
            float.MaxValue
        };

        // this would normally be absolutely horrible but this component is created when the animation is needed, not during load time. is this good? ehhhh...
        Sprite[] buttonPressAnimSprite = new Sprite[]
        {
            TooManyStickersPlugin.Instance.assetMan.Get<Sprite>("JohnnyTeleporter_Press"),
            TooManyStickersPlugin.Instance.assetMan.Get<Sprite>("JohnnyTeleporter"),
            null
        };

        Sprite[] johnnyMouths = new Sprite[]
        {
            TooManyStickersPlugin.Instance.assetMan.Get<Sprite>("JohnnyMouthSheet_0"),
            TooManyStickersPlugin.Instance.assetMan.Get<Sprite>("JohnnyMouthSheet_1"),
            TooManyStickersPlugin.Instance.assetMan.Get<Sprite>("JohnnyMouthSheet_2"),
            TooManyStickersPlugin.Instance.assetMan.Get<Sprite>("JohnnyMouthSheet_3"),
            TooManyStickersPlugin.Instance.assetMan.Get<Sprite>("JohnnyMouthSheet_4"),
        };

        // im so sorry
        (int, float)[] mouthAnim = new (int, float)[]
        {
            (2,0.05f),
            (0,0.02f),
            (4,0.08f),
            (0,0.1f),
            (3,0.1f),
            (0,0.05f),
            (4,0.15f),
            (3,0.15f),
            (0,0.2f),
            (3,0.05f),
            (1,0.1f),
            (0,0.05f),
            (2,0.1f),
            (1,0.02f),
            (3,0.1f),
            (1,0.11f),
            (2,0.05f),
            (0,0.2f),
            (2,0.1f),
            (4,0.1f),
            (3,0.05f),
            (1,0.05f),
            (3,0.05f),
            (0,0.25f),
            (1,0.13f),
            (3,1f),
        };



        void Awake()
        {
            renderer = GetComponent<SpriteRenderer>();
            originalSprite = renderer.sprite;
            renderer.sprite = TooManyStickersPlugin.Instance.assetMan.Get<Sprite>("JohnnyTeleporter");
            mouth = transform.Find("Mouth").GetComponent<SpriteRenderer>();
            StartCoroutine(Animate());
        }

        void Update()
        {
            pressNextTime -= Time.deltaTime;
            if (pressNextTime <= 0f)
            {
                pressFrame++;
                pressNextTime = buttonPressAnimTime[pressFrame];
                renderer.sprite = buttonPressAnimSprite[pressFrame];
                if (renderer.sprite == null)
                {
                    renderer.sprite = originalSprite;
                }
            }
        }

        IEnumerator currentTurn = null;

        public void PressButton()
        {
            pressFrame = 0;
            pressNextTime = buttonPressAnimTime[pressFrame];
            renderer.sprite = buttonPressAnimSprite[pressFrame];
        }

        public void TurnPlayer(PlayerManager pm)
        {
            if (currentTurn != null)
            {
                StopCoroutine(currentTurn);
            }
            currentTurn = TurnPlayer(pm, 2f);
            StartCoroutine(currentTurn);
        }

        IEnumerator TurnPlayer(PlayerManager player, float speed)
        {
            float time = 0.75f;
            Vector3 vector;
            while (time > 0f)
            {
                vector = Vector3.RotateTowards(player.transform.forward, (transform.position - player.transform.position).normalized, Time.deltaTime * 2f * Mathf.PI * speed, 0f);
                player.transform.rotation = Quaternion.LookRotation(vector, Vector3.up);
                time -= Time.deltaTime;
                yield return null;
            }
            currentTurn = null;
            yield break;
        }

        static FieldInfo _animator = AccessTools.Field(typeof(PropagatedAudioManagerAnimator), "animator");

        IEnumerator Animate()
        {
            yield return new WaitForSeconds(1f);
            PropagatedAudioManagerAnimator audMan = GetComponent<PropagatedAudioManagerAnimator>();
            audMan.FlushQueue(true);
            audMan.PlaySingle(TooManyStickersPlugin.Instance.assetMan.Get<SoundObject>("Jon_Daredevils"));
            Animator anim = (Animator)_animator.GetValue(audMan);
            anim.enabled = false;
            float timeLeft = 0f;
            for (int i = 0; i < mouthAnim.Length; i++)
            {
                mouth.sprite = johnnyMouths[mouthAnim[i].Item1];
                // snap to 30fps to emulate scratch (which i used for animation
                timeLeft = Mathf.Ceil(mouthAnim[i].Item2 * 30) / 30;
                while (timeLeft > 0f)
                {
                    timeLeft -= Time.deltaTime;
                    yield return null;
                }
            }
            while (renderer.sprite != originalSprite)
            {
                yield return null;
            }
            anim.enabled = true;
            Destroy(this);
            yield break;
        }
    }
}
