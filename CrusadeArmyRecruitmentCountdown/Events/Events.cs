using Kingmaker.Kingdom;
using static CrusadeArmyRecruitmentCountdown.Main;

namespace CrusadeArmyRecruitmentCountdown.Events
{
    public class OnDayChanged : IKingdomDayHandler
    {
        public void OnNewDay()
        {
            LogDebug("Events.OnDayChanged: OnNewDay event handler triggered.");
            AddRecruitmentCounter();
        }
    }
}
