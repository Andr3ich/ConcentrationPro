using System.Collections.ObjectModel;

namespace ConcentrationTracker.MVVM.Model
{
    public class SessionDetailsModel
    {
        public ObservableCollection<SessionDetailActivityModel> ActivityEvents { get; set; }
        public ObservableCollection<SessionDetailSwitchModel> SwitchEvents { get; set; }
        public ObservableCollection<SessionDetailFocusBlockModel> FocusBlocks { get; set; }

        public SessionDetailsModel()
        {
            ActivityEvents = new ObservableCollection<SessionDetailActivityModel>();
            SwitchEvents = new ObservableCollection<SessionDetailSwitchModel>();
            FocusBlocks = new ObservableCollection<SessionDetailFocusBlockModel>();
        }
    }
}
