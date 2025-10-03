using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusLevelStudio.Editor.Tools;
using PlusStudioLevelLoader;
using System;
using System.Collections.Generic;
using System.Text;

namespace RunnerShoes
{
    public static class EditorAndLoaderSupport
    {
        public static void AddLoaderSupport()
        {
            LevelLoaderPlugin.Instance.itemObjects.Add("runner_shoes", RunnerShoesPlugin.runnerShoes);
        }

        public static void AddStudioSupport()
        {
            EditorInterfaceModes.AddModeCallback((EditorMode mode, bool vanillaCompat) =>
            {
                EditorInterfaceModes.AddToolToCategory(mode, "items", new ItemTool("runner_shoes"));
            });
        }
    }
}
