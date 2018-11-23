using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BlaBlaCarStatisticAnalizer.Windows
{
    public class ApiKeysViewModel : ReactiveObject
    {
        private readonly SynchronizationContext _uiContext = SynchronizationContext.Current;
        public ObservableCollection<string> Keys { get; set; } = new ObservableCollection<string>();

        [Reactive]
        public string SelectedKey { get; set; }

        [Reactive]
        public string NewKeyText { get; set; }

        #region Commands

        public ICommand RemoveKeyCommand { get; set; }
        public ICommand AddKeyCommand { get; set; }
        public ICommand SaveKeysCommand { get; set; }

        #endregion

        public ApiKeysViewModel()
        {
            RemoveKeyCommand = new CommandHandler(RemoveKey, true);
            AddKeyCommand = new CommandHandler(AddKey, true);
            SaveKeysCommand = new CommandHandler(SaveKeys, true);
        }

        public Task Update()
        {
            _uiContext.Send(async x =>
            {
                Keys.Clear();
                foreach (var key in await ApiKeyController.LoadKeysAsync())
                {
                    Keys.Add(key);
                }
            }, null);
            return Task.CompletedTask;
        }

        private void RemoveKey()
        {
            _uiContext.Send(x => { Keys.Remove(SelectedKey); }, null);
        }

        private void AddKey()
        {
            _uiContext.Send(x => {Keys.Add(NewKeyText);}, null);
        }

        private void SaveKeys()
        {
            ApiKeyController.SaveKeys(Keys.ToList());
            Update();
        }
    }
}
