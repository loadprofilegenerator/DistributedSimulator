using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using CommonDistSimFrame;
using CommonDistSimFrame.Common;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace DistSimClientWpf {
    public class ApplicationPresenter : INotifyPropertyChanged {
        [NotNull] private readonly Settings _settings;

        public ApplicationPresenter() => _settings = LoadSettings();

        [NotNull]
        public Settings Settings => _settings;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] [CanBeNull] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        [NotNull]
        private static Settings LoadSettings()
        {
            FileInfo dst = new FileInfo("clientSettings.json");
            if (dst.Exists) {
                string str = File.ReadAllText(dst.FullName);
                return JsonConvert.DeserializeObject<Settings>(str);
            }
#pragma warning disable S1075 // URIs should not be hardcoded
            dst = new FileInfo("c:\\work\\clientSettings.json");
#pragma warning restore S1075 // URIs should not be hardcoded
            if (dst.Exists) {
                string str = File.ReadAllText(dst.FullName);
                return JsonConvert.DeserializeObject<Settings>(str);
            }

            var settings = new Settings(new ClientSettings("workingdir", 1),
                new ServerSettings("lpgdir", new List<string>(), "archivedir"),
                "serverip");
            File.WriteAllText(dst.FullName, JsonConvert.SerializeObject(settings, Formatting.Indented));
            throw new DistSimException("Could not find config, new one written to " + dst.FullName);
        }
    }
}