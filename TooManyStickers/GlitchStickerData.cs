using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TooManyStickers
{
    public class GlitchStickerData : ExtendedStickerData
    {
        public static Dictionary<Sprite, Sprite> actualSpriteToGlitchSprite = new Dictionary<Sprite, Sprite>();

        public static Sprite GetOrGenerateGlitchSprite(string stickerName, Sprite spr)
        {
            if (actualSpriteToGlitchSprite.ContainsKey(spr)) return actualSpriteToGlitchSprite[spr];

            int stupidRngSeed = 0;
            for (int i = 0; i < stickerName.Length; i++)
            {
                stupidRngSeed += stickerName[i];
            }
            System.Random rng = new System.Random(stupidRngSeed);

            Texture2D stickerTex = AssetLoader.MakeReadableCopy(spr.texture, false);

            Dictionary<Color, Color> colorMap = new Dictionary<Color, Color>();

            Color[] pixels = stickerTex.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a < 1f) continue; // ignore transparent pixels
                colorMap[pixels[i]] = pixels[i];
            }

            int colorsToCorrupt = colorMap.Count;

            List<Color> uncorruptedColors = colorMap.Keys.ToList();

            for (int i = 0; i < colorsToCorrupt; i++)
            {
                int index = rng.Next(0, uncorruptedColors.Count);
                Color selectedColor = uncorruptedColors[index];
                uncorruptedColors.RemoveAt(index);
                colorMap[selectedColor] = new Color(rng.Next(0,255) / 255f, rng.Next(0, 255) / 255f, rng.Next(0, 255) / 255f);
            }

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a < 1f) continue; // ignore transparent pixels
                pixels[i] = colorMap[pixels[i]];
            }

            int rowsToOffset = Mathf.CeilToInt(spr.texture.width / 2f);

            for (int i = 0; i < rowsToOffset; i++)
            {
                int chosenRow = rng.Next(0, pixels.Length / spr.texture.width);
                int startInd = chosenRow * spr.texture.width;
                Color[] rowColors = new Color[spr.texture.width];
                for (int j = 0; j < spr.texture.width; j++)
                {
                    rowColors[j] = pixels[startInd + j];
                }
                int offset = -1 + (rng.Next(0, 1) * 2);
                for (int j = (offset == -1 ? rowColors.Length - 1 : 1); (offset == -1 ? j >= 0 : j < rowColors.Length); j += offset)
                {
                    if (((j + offset) < 0) || ((j + offset) >= rowColors.Length))
                    {
                        rowColors[j] = Color.clear;
                        continue;
                    }
                    rowColors[j] = rowColors[j + offset];
                }
                for (int j = 0; j < rowColors.Length; j++)
                {
                    pixels[startInd + j] = rowColors[j];
                } 
            }

            stickerTex.SetPixels(pixels);
            stickerTex.Apply();
            stickerTex.name = spr.name + "_Glitch";

            Sprite generatedSprite = AssetLoader.SpriteFromTexture2D(stickerTex, spr.pixelsPerUnit);

            actualSpriteToGlitchSprite.Add(spr, generatedSprite);
            return generatedSprite;
        }

        public override Sprite GetInventorySprite(StickerStateData data)
        {
            GlitchStickerStateData glitchState = (GlitchStickerStateData)data;
            if (glitchState.stickerMimicing == sticker) return sprite;
            return GetOrGenerateGlitchSprite(glitchState.stickerMimicing.ToStringExtended(), glitchState.stickerMimicing.GetMeta().value.sprite);
        }

        public override Sprite GetAppliedSprite(StickerStateData data)
        {
            return GetInventorySprite(data);
        }

        public override string GetLocalizedInventoryStickerDescription(StickerStateData data)
        {
            GlitchStickerStateData glitchState = (GlitchStickerStateData)data;
            string desc = $"{Singleton<LocalizationManager>.Instance.GetLocalizedText($"StickerTitle_{EnumExtensions.GetExtendedName<Sticker>((int)glitchState.stickerMimicing)}")}<br><br>{Singleton<LocalizationManager>.Instance.GetLocalizedText($"StickerDescription_{EnumExtensions.GetExtendedName<Sticker>((int)glitchState.stickerMimicing)}")}";
            return CorruptText(desc);
        }

        // Source - https://stackoverflow.com/a/2641383
        // Posted by Matti Virkkunen, modified by community. See post 'Timeline' for change history
        // Retrieved 2026-09-10, License - CC BY-SA 3.0

        protected List<int> AllIndexesOf(string str, string value)
        {
            if (String.IsNullOrEmpty(value))
                return new List<int>();
            List<int> indexes = new List<int>();
            for (int index = 0; ; index += value.Length)
            {
                index = str.IndexOf(value, index);
                if (index == -1)
                    return indexes;
                indexes.Add(index);
            }
        }


        public string CorruptText(string str)
        {
            char[] chars = str.ToCharArray();
            List<int> forbiddenIndexes = AllIndexesOf(str, "<br>");
            List<int> additionalForbidden = new List<int>();
            foreach (int item in forbiddenIndexes)
            {
                additionalForbidden.Add(item + 1);
                additionalForbidden.Add(item + 2);
                additionalForbidden.Add(item + 3);
            }
            forbiddenIndexes.AddRange(additionalForbidden);
            int toCorrupt = Mathf.CeilToInt(str.Length / 6);
            for (int i = 0; i < toCorrupt; i++)
            {
                int ind = UnityEngine.Random.Range(1, chars.Length);
                if (forbiddenIndexes.Contains(ind))
                {
                    i--;
                    continue;
                }
                chars[ind] = (char)(Mathf.Max(chars[ind] + UnityEngine.Random.Range(-16, 16),0));
            }
            return new string(chars);
        }

        public override string GetLocalizedAppliedStickerDescription(StickerStateData data)
        {
            return GetLocalizedInventoryStickerDescription(data);
        }

        public override BooleanHandshake CanStackWith(StickerStateData thisSticker, StickerStateData otherSticker)
        {
            if (otherSticker is GlitchStickerStateData otherGlitch)
            {
                return ((GlitchStickerStateData)thisSticker).stickerMimicing == otherGlitch.stickerMimicing ? BooleanHandshake.TrueIfAgree : BooleanHandshake.FalseIfAgree;
            }
            return BooleanHandshake.FalseIfAgree;
        }

        public override StickerStateData CreateStateData(int activeLevel, bool opened, bool sticky)
        {
            GlitchStickerStateData state = new GlitchStickerStateData(sticker, activeLevel, opened, sticky);
            state.RandomlyChangeMimicSticker();
            return state;
        }

        public override StickerStateData CreateOrGetAppliedStateData(StickerStateData inventoryState)
        {
            inventoryState.activeLevel = Singleton<BaseGameManager>.Instance.CurrentLevel;
            inventoryState.opened = true;
            return inventoryState;
        }
    }

    public class GlitchStickerStateData : ExtendedStickerStateData
    {
        public List<Sticker> stickerHistory = new List<Sticker>();
        public Sticker stickerMimicing => stickerHistory.Count == 0 ? sticker : stickerHistory[stickerHistory.Count - 1];
        public GlitchStickerStateData(Sticker sticker, int activeLevel, bool opened, bool sticky) : base(sticker, activeLevel, opened, sticky)
        {

        }

        public bool RollMimicStickerChance()
        {
            if (UnityEngine.Random.Range(0f,1f) <= 0.15f)
            {
                RandomlyChangeMimicSticker();
                return true;
            }
            return false;
        }



        public virtual bool StickerIsValidTarget(Sticker sticker)
        {
            StickerMetaData data = sticker.GetMeta();
            if (data.tags.Contains("tms_glitch_forceallowmimic")) return true;
            if (data.flags.HasFlag(StickerFlags.AffectsLevelGeneration) || data.flags.HasFlag(StickerFlags.IsBonus)) return false;
            if ((data.value.GetType() != typeof(ExtendedStickerData)) && (data.value.GetType() != typeof(VanillaCompatibleExtendedStickerData))) return false; // no attempting to emulate stickers with abnormal behavior WE WILL FAIL!
            if (data.tags.Contains("tms_glitch_nomimic")) return false;
            return true;
        }

        public static bool StickerIsValidTargetForPregeneration(Sticker sticker)
        {
            StickerMetaData data = sticker.GetMeta();
            if (data.tags.Contains("tms_glitch_forceallowmimic")) return true;
            if (data.flags.HasFlag(StickerFlags.AffectsLevelGeneration) || data.flags.HasFlag(StickerFlags.IsBonus)) return false;
            if ((data.value.GetType() != typeof(ExtendedStickerData)) && (data.value.GetType() != typeof(VanillaCompatibleExtendedStickerData))) return false; // no attempting to emulate stickers with abnormal behavior WE WILL FAIL!
            if (data.tags.Contains("tms_glitch_nomimic")) return false;
            return true;
        }

        public void RandomlyChangeMimicSticker()
        {
            List<WeightedSticker> potentialStickers = new List<WeightedSticker>();
            // this shouldn't ever be possible... but just incase.
            if (Singleton<BaseGameManager>.Instance == null)
            {
                potentialStickers.AddRange(StickerMetaStorage.Instance.All().Select(x => new WeightedSticker(x.type, Mathf.Max(Mathf.CeilToInt(1000 / Mathf.Max(x.value.duplicateOddsMultiplier, 0.2f)), 1))));
            }
            else
            {
                if (Singleton<BaseGameManager>.Instance.InPitstop())
                {
                    potentialStickers.AddRange(Singleton<CoreGameManager>.Instance.nextLevel.potentialStickers);
                }
                else
                {
                    potentialStickers.AddRange(Singleton<CoreGameManager>.Instance.sceneObject.potentialStickers);
                }
            }
            potentialStickers.RemoveAll(x => !StickerIsValidTarget(x.selection));
            if (potentialStickers.Where(x => !stickerHistory.Contains(x.selection)).Count() == 0)
            {
                stickerHistory.Clear();
            }
            potentialStickers.RemoveAll(x => stickerHistory.Contains(x.selection));
            stickerHistory.Add(potentialStickers.RandomSelection());
        }

        public const byte version = 0;

        public override void VirtualWrite(BinaryWriter writer)
        {
            base.VirtualWrite(writer);
            writer.Write(version);
            writer.Write(stickerHistory.Count);
            for (int i = 0; i < stickerHistory.Count; i++)
            {
                writer.Write(stickerHistory[i].ToStringExtended());
            }
        }

        public override void VirtualReadInto(BinaryReader reader)
        {
            base.VirtualReadInto(reader);
            stickerHistory.Clear();
            byte version = reader.ReadByte();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                EnumExtensions.GetFromExtendedNameSafe(reader.ReadString(), out Sticker? stickerEnum);
                if (!stickerEnum.HasValue) continue;
                stickerHistory.Add(stickerEnum.Value);
            }
        }
    }
}
