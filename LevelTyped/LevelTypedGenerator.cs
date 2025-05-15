using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Text;

namespace LevelTyped
{
    public abstract class LevelTypedGenerator
    {
        /// <summary>
        /// Determines if a level type should be generated for this specific floor.
        /// Use this to avoid accidently creating duplicates
        /// </summary>
        /// <param name="levelName"></param>
        /// <param name="levelId"></param>
        /// <param name="sceneObject"></param>
        /// <returns></returns>
        public abstract bool ShouldGenerate(string levelName, int levelId, SceneObject sceneObject);
        /// <summary>
        /// The level type that the LevelObject passed into ShouldGenerate will be
        /// This has to be a vanilla level type.
        /// </summary>
        public abstract LevelType levelTypeToBaseOff { get; }

        /// <summary>
        /// The level type that this LevelTypedGenerator generates.
        /// </summary>
        public abstract LevelType myLevelType { get; }

        /// <summary>
        /// The method that will be called for our LevelObject to properly change it
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="levelName"></param>
        /// <param name="levelId"></param>
        public abstract void ApplyChanges(string levelName, int levelId, CustomLevelObject obj);

        /// <summary>
        /// The name of this level type that should be used to name the LevelObject (For example, "Maintenance" or "Factory")
        /// </summary>
        public abstract string levelObjectName { get; }
        
        /// <summary>
        /// Returns the weight for this LevelType.
        /// </summary>
        /// <param name="defaultWeight"></param>
        /// <returns></returns>
        public virtual int GetWeight(int defaultWeight)
        {
            return defaultWeight;
        }
    }
}
