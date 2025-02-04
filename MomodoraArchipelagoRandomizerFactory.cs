using LiveSplit.Model;
using System;

namespace LiveSplit.UI.Components
{
    class MomodoraArchipelagoRandomizerFactory : IComponentFactory
    {
        public string ComponentName => "Momodora RUtM Archipelago Randomizer";

        public string Description => "A fun and exciting archipelago randomizer for Momodora: Reverie Under the Moonlight";

        public ComponentCategory Category => ComponentCategory.Other;

        public string UpdateName => ComponentName;

        public string XMLURL => UpdateURL + "update.MomodoraArchipelagoRandomizer.xml";

        public string UpdateURL => "https://raw.githubusercontent.com/ordinary-magic/MomodoraArchipelagoRandomizer/main/";

        public Version Version => Version.Parse("1.0.0");

        public IComponent Create(LiveSplitState state)
        {
            return new MomodoraArchipelagoRandomizer(state);
        }
    }
}
